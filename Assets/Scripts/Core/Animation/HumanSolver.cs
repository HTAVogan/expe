using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VRtist
{
    public class HumanSolver
    {

        private Vector3 positionTarget;
        private Quaternion rotationTarget;

        private List<AnimationSet> animationList;
        private List<HumanGoalController> controllers;
        private AnimationSet objectAnimation;
        private int animationCount;
        private IEnumerator coroutine;

        private int currentFrame;
        private int firstFrame;
        private int lastFrame;

        private List<Curve> curves;

        struct Constraints
        {
            public List<int> gameObjectIndices;
            public List<int> startFrames;
            public List<int> endFrames;
            public List<AnimatableProperty> properties;
            public List<float> values;
        }

        struct State
        {
            public Vector3 position;
            public Quaternion rotation;
            public int time;
        }

        struct JobData
        {
            public List<NativeArray<double>> jsValues;
            public DsThetaJob Job;
            public JobHandle Handle;
            public NativeArray<KeyFrame> prevFrames;
            public NativeArray<KeyFrame> postFrames;
        }

        private JobData jData;
        private bool activeJob = false;

        private State currentState;
        private State desiredState;

        public List<int> requiredKeyframe;

        Vector2[,] CurveMinMax;

        double tanContinuity;

        int p,
            pinsNB,
            K;

        double[,] Q_opt,
            Stiffnes_D,
            Continuity_T,
            Delta_s_prime,
            Theta;
        double[] b_opt,
            delta_theta_0,
            lowerBound,
            upperBound,
            s,
            delta_theta,
            theta;

        public HumanSolver(Vector3 targetPosition, Quaternion targetRotation, AnimationSet objectAnim, List<AnimationSet> animation, int frame, int startFrame, int endFrame, double continuity)
        {
            positionTarget = targetPosition;
            rotationTarget = targetRotation;
            animationList = new List<AnimationSet>();
            controllers = new List<HumanGoalController>();
            curves = new List<Curve>();
            GetCurves(objectAnim, animation);
            objectAnimation = objectAnim;
            animationCount = animationList.Count;
            tanContinuity = continuity;
            currentFrame = frame;
            firstFrame = startFrame;
            lastFrame = endFrame;
        }

        private void GetCurves(AnimationSet objectAnim, List<AnimationSet> animation)
        {
            animation.ForEach(anim =>
            {
                if (null != anim)
                {
                    animationList.Add(anim);
                    controllers.Add(anim.transform.GetComponent<HumanGoalController>());

                    curves.Add(anim.GetCurve(AnimatableProperty.RotationX));
                    curves.Add(anim.GetCurve(AnimatableProperty.RotationY));
                    curves.Add(anim.GetCurve(AnimatableProperty.RotationZ));
                }
            });
            animationList.Add(objectAnim);
            controllers.Add(objectAnim.transform.GetComponent<HumanGoalController>());
            curves.Add(objectAnim.GetCurve(AnimatableProperty.RotationX));
            curves.Add(objectAnim.GetCurve(AnimatableProperty.RotationY));
            curves.Add(objectAnim.GetCurve(AnimatableProperty.RotationZ));
            curves.Add(animationList[0].GetCurve(AnimatableProperty.PositionX));
            curves.Add(animationList[0].GetCurve(AnimatableProperty.PositionY));
            curves.Add(animationList[0].GetCurve(AnimatableProperty.PositionZ));
        }

        public bool TrySolver()
        {
            if (coroutine == null)
            {
                coroutine = Solve();
                return true;
            }
            return coroutine.MoveNext();
        }

        public IEnumerator Solve()
        {
            yield return Setup();
            yield return Compute();
            yield return Apply();
            yield return false;
        }

        public bool Setup()
        {
            Curve rotXCurve = objectAnimation.GetCurve(AnimatableProperty.RotationX);
            rotXCurve.GetKeyIndex(firstFrame, out int firstIndex);
            //firstFrame = rotXCurve.keys[firstIndex].frame;
            rotXCurve.GetKeyIndex(lastFrame, out int lastIndex);
            //lastFrame = rotXCurve.keys[lastIndex].frame;

            if (currentFrame < firstFrame) return false;
            if (currentFrame > lastFrame) return false;

            requiredKeyframe = new List<int>() { firstFrame, lastFrame };
            K = requiredKeyframe.Count;
            int curveCount = curves.Count;
            p = curveCount * 4 * 2;

            ds_thetaJob(p, K);

            theta = new double[p];
            Stiffnes_D = new double[p, p];
            Continuity_T = new double[p, p];
            CurveMinMax = new Vector2[curveCount, 2];
            lowerBound = new double[p];
            upperBound = new double[p];
            s = new double[p];
            delta_theta_0 = new double[p];

            for (int a = 0; a < animationCount; a++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int curveIndex = a * 3 + c;
                    AnimationKey prevKey = curves[curveIndex].keys[firstIndex];
                    AnimationKey ppKey = firstIndex > 0 ? curves[curveIndex].keys[firstIndex - 1] : new AnimationKey(prevKey.frame, prevKey.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                    AnimationKey nextKey = curves[curveIndex].keys[lastIndex];
                    AnimationKey nnKey = lastIndex < curves[curveIndex].keys.Count - 1 ? curves[curveIndex].keys[lastIndex + 1] : new AnimationKey(nextKey.frame, nextKey.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                    GetTangents(curveIndex * 8, prevKey, nextKey);
                    CurveMinMax[curveIndex, 0] = GetMinMax(ppKey, prevKey);
                    CurveMinMax[curveIndex, 1] = GetMinMax(nextKey, nnKey);

                    float Min = controllers[a].LowerAngleBound[c];
                    float Max = controllers[a].UpperAngleBound[c];
                    FillLowerBounds(curveIndex, prevKey, nextKey, Min, Max);
                    FillUpperBounds(curveIndex, prevKey, nextKey, ppKey, nnKey, Min, Max);
                    GetContinuity(curveIndex * 8, Min, Max);
                    for (int t = 0; t < 8; t++)
                    {
                        int tanIndice = curveIndex * 8 + t;
                        Stiffnes_D[tanIndice, tanIndice] = controllers[a].stiffness;
                        s[tanIndice] = 1d;
                        delta_theta_0[tanIndice] = 0;
                    }
                }
            }
            int aIndex = animationCount;
            for (int c = 0; c < 3; c++)
            {
                int curveIndex = aIndex * 3 + c;
                AnimationKey prevKey = curves[curveIndex].keys[firstIndex];
                AnimationKey ppKey = firstIndex > 0 ? curves[curveIndex].keys[firstIndex - 1] : new AnimationKey(prevKey.frame, prevKey.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                AnimationKey nextKey = curves[curveIndex].keys[lastIndex];
                AnimationKey nnKey = lastIndex < curves[curveIndex].keys.Count - 1 ? curves[curveIndex].keys[lastIndex + 1] : new AnimationKey(nextKey.frame, nextKey.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                GetTangents(curveIndex * 8, prevKey, nextKey);
                CurveMinMax[curveIndex, 0] = GetMinMax(ppKey, prevKey);
                CurveMinMax[curveIndex, 1] = GetMinMax(nextKey, nnKey);
                GetContinuity(curveIndex * 8);
                for (int t = 0; t < 8; t++)
                {
                    int tanIndice = curveIndex * 8 + t;
                    Stiffnes_D[tanIndice, tanIndice] = controllers[0].stiffness;
                    lowerBound[tanIndice] = -10;
                    upperBound[tanIndice] = 10;
                    s[tanIndice] = 1d;
                    delta_theta_0[tanIndice] = 0;
                }

            }
            currentState = GetCurrentState(currentFrame);
            desiredState = new State()
            {
                position = positionTarget,
                rotation = rotationTarget,
                time = currentFrame

            };

            Delta_s_prime = new double[7, 1];
            for (int i = 0; i < 3; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            for (int i = 3; i < 7; i++)
            {
                Delta_s_prime[i, 0] = desiredState.rotation[i - 3] - currentState.rotation[i - 3];
            }

            Theta = ColumnArrayToArray(theta);


            return true;
        }


        public bool Compute()
        {
            //wm
            double targetW = 100d;
            //wb
            double continuityW = 1d;
            //wd
            double stiffnessW = 1d;

            double[,] Js = ThetaFromJob(p);

            Q_opt = Add(Add(Multiply(2d * targetW, Multiply(Transpose(Js), Js)), Add(Multiply(2d * stiffnessW, Stiffnes_D),
                Multiply(2d * continuityW, Continuity_T))), Multiply((double)Mathf.Pow(10, -6), Identity(p)));

            double[,] B_opt = Add(Multiply(-2d * targetW, Multiply(Transpose(Js), Delta_s_prime)), Multiply(2d * continuityW, Multiply(Continuity_T, Theta)));
            b_opt = ArrayToColumnArray(B_opt);

            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(p, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);

            alglib.minqpsetscale(state_opt, s);

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            return true;
        }
        public bool Apply()
        {
            double[] new_theta = new double[p];
            for (int i = 0; i < p; i++)
            {
                new_theta[i] = delta_theta[i] + theta[i];
            }
            for (int l = 0; l < animationCount; l++)
            {
                AnimationSet currentAnim = animationList[l];
                for (int i = 0; i < 3; i++)
                {

                    AnimatableProperty property = (AnimatableProperty)i + 3;
                    Curve curve = currentAnim.GetCurve(property);

                    for (int k = 0; k < K; k++)
                    {
                        curve.GetKeyIndex(requiredKeyframe[k], out int index);
                        Vector2 inTangent = new Vector2((float)new_theta[12 * K * l + 4 * (i * K + k) + 0], (float)new_theta[12 * K * l + 4 * (i * K + k) + 1]);
                        Vector2 outTangent = new Vector2((float)new_theta[12 * K * l + 4 * (i * K + k) + 2], (float)new_theta[12 * K * l + 4 * (i * K + k) + 3]);
                        ModifyTangents(curve, index, inTangent, outTangent);
                    }
                }
            }
            for (int i = 3; i < 6; i++)
            {
                Curve curve = animationList[0].GetCurve((AnimatableProperty)i - 3);

                for (int k = 0; k < K; k++)
                {
                    curve.GetKeyIndex(requiredKeyframe[k], out int index);
                    Vector2 inTangent = new Vector2((float)new_theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0], (float)new_theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1]);
                    Vector2 outTangent = new Vector2((float)new_theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2], (float)new_theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3]);
                    ModifyTangents(curve, index, inTangent, outTangent);
                }
            }

            return true;
        }

        public void ClearJob()
        {
            Debug.Log("clear job");
            if (activeJob)
            {
                jData.Handle.Complete();
                for (int v = 0; v < 7; v++)
                {
                    jData.jsValues[v].Dispose();
                }
                jData.postFrames.Dispose();
                jData.prevFrames.Dispose();
                activeJob = false;
            }
        }
        private void FillLowerBounds(int curveIndex, AnimationKey prevKey, AnimationKey nextKey, float Min, float Max)
        {
            //k- in.x
            lowerBound[curveIndex * 8 + 0] = 0;
            //k- in.y
            lowerBound[curveIndex * 8 + 1] = -Mathf.Max(0, Max - Mathf.Max(CurveMinMax[curveIndex, 0].x, CurveMinMax[curveIndex, 0].y));
            //k- out.x
            lowerBound[curveIndex * 8 + 2] = 0;
            //k- out.y
            lowerBound[curveIndex * 8 + 3] = Mathf.Min(0, (4 / 3f) * (Min - (prevKey.value + (3 / 4f) * prevKey.outTangent.y)));
            //k+ in.x
            lowerBound[curveIndex * 8 + 4] = 0;
            //k+ in.y
            lowerBound[curveIndex * 8 + 5] = Mathf.Min(0, -(4 / 3f) * (Max - (nextKey.value - (3 / 4f) * nextKey.inTangent.y)));
            //k+ out.x
            lowerBound[curveIndex * 8 + 6] = 0;
            //k+ out.y
            lowerBound[curveIndex * 8 + 7] = Mathf.Min(0, Min - Mathf.Min(CurveMinMax[curveIndex, 1].x, CurveMinMax[curveIndex, 1].y));
        }

        private void FillUpperBounds(int curveIndex, AnimationKey prevKey, AnimationKey nextKey, AnimationKey ppKey, AnimationKey nnKey, float Min, float Max)
        {
            //k- in.x
            upperBound[curveIndex * 8 + 0] = prevKey.frame - ppKey.frame;
            //k- in.y
            upperBound[curveIndex * 8 + 1] = -Mathf.Min(0, Min - Mathf.Min(CurveMinMax[curveIndex, 0].x, CurveMinMax[curveIndex, 0].y));
            //k- out.x
            upperBound[curveIndex * 8 + 2] = currentFrame - prevKey.frame;
            //k- out.y
            upperBound[curveIndex * 8 + 3] = Mathf.Max(0, (4 / 3f) * (Max - (prevKey.value + (3 / 4f) * prevKey.outTangent.y)));
            //k+ in.x
            upperBound[curveIndex * 8 + 4] = nextKey.frame - currentFrame;
            //k+ in.y
            upperBound[curveIndex * 8 + 5] = Mathf.Max(0, -(4 / 3f) * (Min - (nextKey.value - (3 / 4f) * nextKey.inTangent.y)));
            //k+ out.x
            upperBound[curveIndex * 8 + 6] = nnKey.frame - nextKey.frame;
            //k+ out.y
            upperBound[curveIndex * 8 + 7] = Mathf.Max(0, Max - Mathf.Max(CurveMinMax[curveIndex, 1].x, CurveMinMax[curveIndex, 1].y));
        }

        private void GetContinuity(int ac, float Min, float Max)
        {
            double continuity;

            if (theta[ac + 3] <= 0) continuity = Max == 0 ? 0 : Mathf.Clamp((float)-lowerBound[ac + 1] / (float)Max, 0.001f, 1);
            else continuity = Min == 0 ? 0 : Mathf.Clamp((float)-upperBound[ac + 1] / (float)Min, 0.001f, 1);

            for (int j = 0; j < 4; j++)
            {
                int indice = ac + j;
                Continuity_T[indice, indice] = continuity;
                if (indice % 4 == 0 || j % 4 == 1) Continuity_T[indice + 2, indice] = -continuity;
                else Continuity_T[indice - 2, indice] = -continuity;
            }

            if (theta[ac + 5] >= 0) continuity = Max == 0 ? 0 : Mathf.Clamp((float)upperBound[ac + 7] / (float)Max, 0.001f, 1);
            else continuity = Min == 0 ? 0 : Mathf.Clamp((float)lowerBound[ac + 7] / (float)Min, 0.001f, 1);

            for (int j = 4; j < 8; j++)
            {
                int indice = ac + j;
                Continuity_T[indice, indice] = continuity;
                if (indice % 4 == 0 || indice % 4 == 1) Continuity_T[indice + 2, indice] = -continuity;
                else Continuity_T[indice - 2, indice] = -continuity;
            }
        }
        private void GetContinuity(int ac)
        {

            for (int i = 0; i < 8; i++)
            {
                int j = (ac) + i;
                Continuity_T[j, j] = 1;
                if (j % 4 == 0 || j % 4 == 1)
                {
                    Continuity_T[j + 2, j] = -1d;
                }
                else
                {
                    Continuity_T[j - 2, j] = -1d;
                }
            }
        }


        void ds_thetaJob(int p, int K)
        {
            float dtheta = Mathf.Pow(10, -2);
            jData = new JobData();
            jData.jsValues = new List<NativeArray<double>>();
            for (int i = 0; i < 7; i++)
            {
                jData.jsValues.Add(new NativeArray<double>(12 * 2 * animationCount + 24, Allocator.TempJob));
            }
            Matrix4x4 parentMatrix = animationList[0].transform.parent.localToWorldMatrix;

            jData.prevFrames = new NativeArray<KeyFrame>(animationCount, Allocator.TempJob);
            jData.postFrames = new NativeArray<KeyFrame>(animationCount, Allocator.TempJob);

            for (int l = 0; l < animationCount; l++)
            {
                jData.prevFrames[l] = KeyFrame.GetKey(animationList[l], requiredKeyframe[0]);
                jData.postFrames[l] = KeyFrame.GetKey(animationList[l], requiredKeyframe[1]);
            }

            jData.Job = new DsThetaJob()
            {
                Js0 = jData.jsValues[0],
                Js1 = jData.jsValues[1],
                Js2 = jData.jsValues[2],
                Js3 = jData.jsValues[3],
                Js4 = jData.jsValues[4],
                Js5 = jData.jsValues[5],
                Js6 = jData.jsValues[6],
                dtheta = dtheta,
                frame = currentFrame,
                ParentMatrix = parentMatrix,
                postFrames = jData.postFrames,
                prevFrames = jData.prevFrames
            };

            jData.Handle = jData.Job.Schedule(24 * animationCount, 48);
            activeJob = true;
        }

        double[,] ThetaFromJob(int p)
        {
            //Profiler.BeginSample("theta from job");
            double[,] Js = new double[7, p];

            jData.Handle.Complete();
            for (int i = 0; i < p; i++)
            {
                for (int v = 0; v < 7; v++)
                {
                    Js[v, i] = jData.jsValues[v][i];
                }
            }
            jData.jsValues.ForEach(x => x.Dispose());
            jData.jsValues.Clear();
            jData.postFrames.Dispose();
            jData.prevFrames.Dispose();
            activeJob = false;
            //Profiler.EndSample();

            return Js;
        }

        private void GetTangents(int ac, AnimationKey prevKey, AnimationKey nextKey)
        {
            theta[ac + 0] = prevKey.inTangent.x;
            theta[ac + 1] = prevKey.inTangent.y;
            theta[ac + 2] = prevKey.outTangent.x;
            theta[ac + 3] = prevKey.outTangent.y;
            theta[ac + 4] = nextKey.inTangent.x;
            theta[ac + 5] = nextKey.inTangent.y;
            theta[ac + 6] = nextKey.outTangent.x;
            theta[ac + 7] = nextKey.outTangent.y;
        }

        private State GetCurrentState(int currentFrame)
        {
            Matrix4x4 currentMatrix = FrameMatrix(currentFrame, animationList);
            Maths.DecomposeMatrix(currentMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            return new State()
            {
                position = pos,
                rotation = rot,
                time = currentFrame
            };
        }

        #region Tools

        private Vector2 GetMinMax(AnimationKey prevK, AnimationKey nextK)
        {
            float A = prevK.value;
            float B = A + prevK.outTangent.y;
            float D = nextK.value;
            float C = D - nextK.inTangent.y;

            float a = -A + (3 * B) - (3 * C) + D;
            float b = (3 * A) - (6 * B) + (3 * C);
            float c = (-3 * A) + (3 * B);

            float tMin = 0;
            float tMax = 1;

            if (a != 0 && ((b * b) - 3 * a * c) > 0)
            {
                tMin = (-b - Mathf.Sqrt((b * b) - 3 * a * c)) / (3 * a);
                tMax = (-b + Mathf.Sqrt((b * b) - 3 * a * c)) / (3 * a);
            }
            float MinValue = CubicBezier(A, B, C, D, Mathf.Clamp01(tMin));
            float MaxValue = CubicBezier(A, B, C, D, Mathf.Clamp01(tMax));
            return new Vector2(MinValue, MaxValue);
        }

        double[,] ColumnArrayToArray(double[] m)
        {
            int row = m.Length;
            double[,] response = new double[row, 1];
            for (int i = 0; i < row; i++)
            {
                response[i, 0] = m[i];
            }
            return response;
        }

        private float CubicBezier(float A, float B, float C, float D, float t)
        {
            float invT1 = 1 - t;
            float invT2 = invT1 * invT1;
            float invT3 = invT2 * invT1;

            float t2 = t * t;
            float t3 = t2 * t;

            return (A * invT3) + (B * 3 * t * invT2) + (C * 3 * invT1 * t2) + (D * t3);
        }

        public Matrix4x4 FrameMatrix(int frame, List<AnimationSet> animations)
        {
            Matrix4x4 trsMatrix = animations[0].transform.parent.localToWorldMatrix;

            for (int i = 0; i < animationList.Count; i++)
            {
                trsMatrix = trsMatrix * GetBoneMatrix(animations[i], frame);
            }
            return trsMatrix;
        }

        private Matrix4x4 GetBoneMatrix(AnimationSet anim, int frame)
        {
            if (null == anim) return Matrix4x4.identity;
            Vector3 position = Vector3.zero;
            Curve posx = anim.GetCurve(AnimatableProperty.PositionX);
            Curve posy = anim.GetCurve(AnimatableProperty.PositionY);
            Curve posz = anim.GetCurve(AnimatableProperty.PositionZ);
            if (null != posx && null != posy && null != posz)
            {
                if (posx.Evaluate(frame, out float px) && posy.Evaluate(frame, out float py) && posz.Evaluate(frame, out float pz))
                {
                    position = new Vector3(px, py, pz);
                }
            }
            Quaternion rotation = Quaternion.identity;
            Curve rotx = anim.GetCurve(AnimatableProperty.RotationX);
            Curve roty = anim.GetCurve(AnimatableProperty.RotationY);
            Curve rotz = anim.GetCurve(AnimatableProperty.RotationZ);
            if (null != posx && null != roty && null != rotz)
            {
                if (rotx.Evaluate(frame, out float rx) && roty.Evaluate(frame, out float ry) && rotz.Evaluate(frame, out float rz))
                {
                    rotation = Quaternion.Euler(rx, ry, rz);
                }
            }
            Vector3 scale = Vector3.one;
            Curve scalex = anim.GetCurve(AnimatableProperty.ScaleX);
            Curve scaley = anim.GetCurve(AnimatableProperty.ScaleY);
            Curve scalez = anim.GetCurve(AnimatableProperty.ScaleZ);
            if (null != scalex && null != scaley && null != scalez)
            {
                if (scalex.Evaluate(frame, out float sx) && scaley.Evaluate(frame, out float sy) && scalez.Evaluate(frame, out float sz))
                {
                    scale = new Vector3(sx, sy, sz);
                }
            }
            return Matrix4x4.TRS(position, rotation, scale);
        }


        double[,] Add(double[,] m1, double[,] m2)
        {
            int row1 = m1.GetUpperBound(0) + 1;
            int col1 = m1.GetUpperBound(1) + 1;
            int row2 = m2.GetUpperBound(0) + 1;
            int col2 = m2.GetUpperBound(1) + 1;

            if (row1 != row2 || col1 != col2)
            {
                Debug.Log("Matrix addition with uncompatible matrix sizes");
                return new double[1, 1];
            }

            else
            {
                double[,] response = new double[row1, col1];
                for (int i = 0; i < row1; i++)
                {
                    for (int j = 0; j < col1; j++)
                    {
                        response[i, j] = m1[i, j] + m2[i, j];
                    }
                }
                return response;
            }
        }

        double[,] Multiply(double alpha, double[,] m)
        {
            int row = m.GetUpperBound(0) + 1;
            int col = m.GetUpperBound(1) + 1;

            double[,] response = new double[row, col];
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    response[i, j] = alpha * m[i, j];
                }
            }
            return response;
        }
        double[,] Transpose(double[,] m)
        {
            int row = m.GetUpperBound(0) + 1;
            int col = m.GetUpperBound(1) + 1;
            double[,] response = new double[col, row];
            for (int i = 0; i < col; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    response[i, j] = m[j, i];
                }
            }
            return response;
        }

        double[,] Multiply(double[,] m1, double[,] m2)
        {
            int row1 = m1.GetUpperBound(0) + 1;
            int col1 = m1.GetUpperBound(1) + 1;
            int row2 = m2.GetUpperBound(0) + 1;
            int col2 = m2.GetUpperBound(1) + 1;

            if (col1 != row2)
            {
                Debug.Log("Matrix multiplication with uncompatible matrix sizes");
                return new double[1, 1];
            }
            else
            {
                double[,] response = new double[row1, col2];
                for (int i = 0; i < row1; i++)
                {
                    for (int j = 0; j < col2; j++)
                    {
                        double sum = 0d;
                        for (int k = 0; k < col1; k++)
                        {
                            sum += m1[i, k] * m2[k, j];
                        }
                        response[i, j] = sum;
                    }
                }
                return response;
            }
        }

        double[,] Identity(int p)
        {
            double[,] response = new double[p, p];
            for (int i = 0; i < p; i++)
            {
                response[i, i] = 1d;
            }
            return response;
        }

        double[] ArrayToColumnArray(double[,] m)
        {
            int row = m.GetUpperBound(0) + 1;
            int col = m.GetUpperBound(1) + 1;
            if (col != 1)
            {
                Debug.Log("Impossible to make a column array.");
                return new double[1];
            }
            else
            {
                double[] response = new double[row];
                for (int i = 0; i < row; i++)
                {
                    response[i] = m[i, 0];
                }
                return response;
            }
        }

        public void ModifyTangents(Curve curve, int index, Vector2 inTangent, Vector2 outTangent)
        {
            curve.keys[index].inTangent = inTangent;
            curve.keys[index].outTangent = outTangent;
            curve.ComputeCacheValuesAt(index);
        }

        #endregion
    }
}
