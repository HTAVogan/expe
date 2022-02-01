using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace VRtist
{

    public class TangentHumanSolver
    {
        private Vector3 positionTarget;
        private Quaternion rotationTarget;
        private List<AnimationSet> animationList;
        private List<HumanGoalController> controllers;
        private int currentFrame;
        private int size;
        private AnimationSet objectAnimation;
        private int animationCount;
        private IEnumerator coroutine;


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

        Constraints constraints;

        private int firstFrame;
        private int lastFrame;

        double[,] Q_opt;
        double[] b_opt;
        int p;
        double[] delta_theta_0;
        double[] lowerBound;
        double[] upperBound;
        int pinsNB;
        int K;
        public List<int> requiredKeyframe;
        double[] s;
        double[] delta_theta;
        double[] theta;
        double[,] DT_D;
        double[,] Delta_s_prime;
        double[,] TT_T;
        double[,] Theta;

        private string[,] tanNames;

        public TangentHumanSolver(Vector3 targetPosition, Quaternion targetRotation, AnimationSet objectAnim, List<AnimationSet> animation, int frame, int zoneSize)
        {
            positionTarget = targetPosition;
            rotationTarget = targetRotation;
            animationList = new List<AnimationSet>();
            controllers = new List<HumanGoalController>();
            animation.ForEach(x =>
            {
                if (x != null)
                {
                    animationList.Add(x);
                    controllers.Add(x.transform.GetComponent<HumanGoalController>());
                }
            });
            animationList.Add(objectAnim);
            controllers.Add(objectAnim.transform.GetComponent<HumanGoalController>());
            objectAnimation = animationList[animationList.Count - 1];
            animationCount = animationList.Count;
            currentFrame = frame;
            size = zoneSize;
            constraints = new Constraints()
            { endFrames = new List<int>(), gameObjectIndices = new List<int>(), properties = new List<AnimatableProperty>(), startFrames = new List<int>(), values = new List<float>() };


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
            //Profiler.BeginSample("setup");
            yield return Setup();
            //Profiler.EndSample();
            //Profiler.BeginSample("compute");
            yield return Compute();
            //Profiler.EndSample();
            //Profiler.BeginSample("Apply");
            yield return Apply();
            //Profiler.EndSample();
            yield return false;
        }

        public bool Setup()
        {
            Debug.Log("setup");
            objectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(currentFrame - size, out int firstIndex);
            firstFrame = objectAnimation.curves[AnimatableProperty.PositionX].keys[firstIndex].frame;
            objectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(currentFrame + size, out int lastIndex);
            lastFrame = objectAnimation.curves[AnimatableProperty.PositionX].keys[lastIndex].frame;

            if (currentFrame < firstFrame) return false;
            if (currentFrame > lastFrame) return false;
            if (constraints.startFrames.Count != 0)
            {
                for (int i = 0; i < constraints.gameObjectIndices.Count; i++)
                {
                    if (constraints.startFrames[i] < firstFrame)
                    {
                        return false;
                    }
                    if (constraints.endFrames[i] > lastFrame)
                    {
                        return false;
                    }
                }
            }
            Debug.Log("setup true");

            requiredKeyframe = FindRequiredTangents(firstFrame, lastFrame, objectAnimation.GetCurve(AnimatableProperty.PositionX));
            K = requiredKeyframe.Count;
            int totalKeyframes = objectAnimation.GetCurve(AnimatableProperty.PositionX).keys.Count;
            int n = 3 * animationCount + 3;
            p = 12 * animationCount * K + 12 * K;
            pinsNB = constraints.gameObjectIndices.Count;

            //Profiler.BeginSample("get all tan");
            tanNames = new string[2, p];
            theta = GetAllTangents(p, K);
            //Profiler.EndSample();
            Theta = ColumnArrayToArray(theta);

            currentState = GetCurrentState(currentFrame);
            desiredState = new State()
            {
                position = positionTarget,
                rotation = rotationTarget,
                time = currentFrame
            };

            //Profiler.BeginSample("ds dtheta");
            //double[,] Js = ds_dtheta(p, K);
            ds_thetaJob(p, K);
            //Profiler.EndSample();
            //Stiffness
            DT_D = new double[p, p];
            //Root rotation tangents
            for (int i = 0; i < 12 * K; i++)
            {
                DT_D[i, i] = 1d;
            }
            int val = 0;
            for (int l = 1; l < animationCount; l++)
            {
                for (int k = 0; k < K; k++)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        val = (12 * K) * l + i + (k * 12);
                        DT_D[val, val] = controllers[l].stiffness;
                    }
                }
            }

            //Non-root rotation tangents
            //for (int i = 12 * K; i < p - 12 * K; i++)
            //{
            //    DT_D[i, i] = 0d;
            //}
            //Root position tangents
            for (int i = p - 12 * K; i < p; i++)
            {
                DT_D[i, i] = 1d;
            }

            Delta_s_prime = new double[7, 1];
            for (int i = 0; i < 3; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            for (int i = 3; i < 7; i++)
            {
                Delta_s_prime[i, 0] = desiredState.rotation[i - 3] - currentState.rotation[i - 3];
            }

            TT_T = new double[p, p];
            for (int j = 0; j < p; j++)
            {
                TT_T[j, j] = 1d;
                if (j % 4 == 0 || j % 4 == 1)
                {
                    TT_T[j + 2, j] = -1d;
                }
                else
                {
                    TT_T[j - 2, j] = -1d;
                }
            }

            //lowerBound = InitializeUBound(p);
            //upperBound = InitializeVBound(p);

            ////Profiler.BeginSample("constraints");
            //lowerBound = LowerBoundConstraints(theta, u, v, p, K, totalKeyframes);
            //upperBound = UpperBoundConstraints(theta, u, v, p, K, totalKeyframes);
            //Profiler.EndSample();

            lowerBound = LowerBound(p, K);
            upperBound = UpperBound(p, K);

            delta_theta_0 = new double[p];

            s = new double[p];
            for (int i = 0; i < p; i++)
            {
                s[i] = 1d;
                delta_theta_0[i] = 0d;
            }
            return true;
        }

        public bool Compute()
        {
            Debug.Log("compute");

            //Profiler.BeginSample("compute");
            double wm = 100d;
            double wb = 1d;
            double wd = 1d;

            double[,] Js = ThetaFromJob(p);

            Q_opt = Add(Add(Multiply(2d * wm, Multiply(Transpose(Js), Js)), Add(Multiply(2d * wd, DT_D), Multiply(2d * wb, TT_T))), Multiply((double)Mathf.Pow(10, -6), Identity(p)));

            double[,] B_opt = Add(Multiply(-2d * wm, Multiply(Transpose(Js), Delta_s_prime)), Multiply(2d * wb, Multiply(TT_T, Theta)));
            b_opt = ArrayToColumnArray(B_opt);

            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(p, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);

            if (constraints.startFrames.Count != 0)
            {
                Dictionary<int, int[]> indexTable = FindConstraintsInRho(pinsNB);

                (double[], double[]) rhos = RhoAndRhoPrime(indexTable, pinsNB);
                double[] rho = rhos.Item1;
                double[] rho_prime = rhos.Item2;
                double[,] Delta_rho_prime = Add(ColumnArrayToArray(rho_prime), Multiply(-1d, ColumnArrayToArray(rho)));
                double[] delta_rho_prime = ArrayToColumnArray(Delta_rho_prime);
                double[,] Jrho = drho_dtheta(indexTable, pinsNB, p, K);

                (double[,], int[]) linearConstraints = FindLinearEqualityConstraints(Jrho, delta_rho_prime);
                double[,] C = linearConstraints.Item1;
                int[] CT = linearConstraints.Item2;
                int K_size = CT.Length;

                alglib.minqpsetlc(state_opt, C, CT, K_size);
            }

            //Profiler.BeginSample("alglib");
            alglib.minqpsetscale(state_opt, s);

            //alglib.minqpsetalgoquickqp(state_opt, 0.0, 0.0, 0.0, 0, true);
            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            //Profiler.EndSample();
            //Profiler.EndSample();

            return true;
        }
        public bool Apply()
        {
            Debug.Log("apply");
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

            //State finalState = GetCurrentState(currentFrame);
            ////Debug.Log("result " + finalState.position.x + " , " + finalState.position.y + " , " + finalState.position.z + " - " + finalState.rotation);

            //if (Vector3.Distance(finalState.position, desiredState.position) > Vector3.Distance(currentState.position, desiredState.position))
            //{
            //    for (int l = 0; l < animationCount; l++)
            //    {
            //        AnimationSet currentAnim = animationList[l];
            //        for (int i = 0; i < 3; i++)
            //        {

            //            AnimatableProperty property = (AnimatableProperty)i + 3;
            //            Curve curve = currentAnim.GetCurve(property);

            //            for (int k = 0; k < K; k++)
            //            {
            //                curve.GetKeyIndex(requiredKeyframe[k], out int index);
            //                Vector2 inTangent = new Vector2((float)theta[12 * K * l + 4 * (i * K + k) + 0], (float)theta[12 * K * l + 4 * (i * K + k) + 1]);
            //                Vector2 outTangent = new Vector2((float)theta[12 * K * l + 4 * (i * K + k) + 2], (float)theta[12 * K * l + 4 * (i * K + k) + 3]);
            //                ModifyTangents(curve, index, inTangent, outTangent);
            //            }
            //        }
            //    }
            //    for (int i = 3; i < 6; i++)
            //    {
            //        Curve curve = animationList[0].GetCurve((AnimatableProperty)i - 3);

            //        for (int k = 0; k < K; k++)
            //        {
            //            curve.GetKeyIndex(requiredKeyframe[k], out int index);
            //            Vector2 inTangent = new Vector2((float)theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0], (float)theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1]);
            //            Vector2 outTangent = new Vector2((float)theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2], (float)theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3]);
            //            ModifyTangents(curve, index, inTangent, outTangent);
            //        }
            //    }
            //}

            return true;
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

        private double[] GetAllTangents(int p, int K)
        {
            double[] theta = new double[p];


            //Rotation curves
            for (int l = 0; l < animationCount; l++)
            {
                AnimationSet currentAnim = animationList[l];
                for (int i = 0; i < 3; i++)
                {

                    AnimatableProperty property = (AnimatableProperty)i + 3;
                    Curve curve = currentAnim.GetCurve(property);
                    for (int k = 0; k < K; k++)
                    {
                        double[] tangents = GetTangents(curve, requiredKeyframe[k]);

                        theta[12 * K * l + 4 * (i * K + k) + 0] = tangents[0];
                        tanNames[0, 12 * K * l + 4 * (i * K + k) + 0] = currentAnim.transform.name + " " + property + k + " in.x";
                        theta[12 * K * l + 4 * (i * K + k) + 1] = tangents[1];
                        tanNames[0, 12 * K * l + 4 * (i * K + k) + 1] = currentAnim.transform.name + " " + property + k + " in.y";
                        theta[12 * K * l + 4 * (i * K + k) + 2] = tangents[2];
                        tanNames[0, 12 * K * l + 4 * (i * K + k) + 2] = currentAnim.transform.name + " " + property + k + " out.x";
                        theta[12 * K * l + 4 * (i * K + k) + 3] = tangents[3];
                        tanNames[0, 12 * K * l + 4 * (i * K + k) + 3] = currentAnim.transform.name + " " + property + k + " out.y";
                    }
                }
            }

            //Position curves of the root
            for (int i = 3; i < 6; i++)
            {
                AnimationSet currentAnim = animationList[0];
                AnimatableProperty property = (AnimatableProperty)i - 3;
                Curve curve = currentAnim.GetCurve(property);

                for (int k = 0; k < K; k++)
                {
                    double[] tangents = GetTangents(curve, requiredKeyframe[k]);

                    theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0] = tangents[0];
                    theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1] = tangents[1];
                    theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2] = tangents[2];
                    theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3] = tangents[3];
                }

            }

            return theta;
        }

        private double[] GetTangents(Curve curve, int frame)
        {
            double[] tangents = new double[4];

            curve.GetKeyIndex(frame, out int index);
            AnimationKey AnimKey = curve.keys[index];
            Vector2 inTangent = AnimKey.inTangent;
            Vector2 outTangent = AnimKey.outTangent;

            tangents[0] = inTangent.x;
            tangents[1] = inTangent.y;
            tangents[2] = outTangent.x;
            tangents[3] = outTangent.y;

            return tangents;
        }

        public void ModifyTangents(Curve curve, int index, Vector2 inTangent, Vector2 outTangent)
        {
            Profiler.BeginSample("modify tan");
            curve.keys[index].inTangent = inTangent;
            curve.keys[index].outTangent = outTangent;
            curve.ComputeCacheValuesAt(index);
            Profiler.EndSample();
        }

        private List<int> FindRequiredTangents(int firstFrame, int lastFrame, Curve curve)
        {
            List<int> keys = new List<int>() { firstFrame, lastFrame };
            //curve.GetKeyIndex(firstFrame, out int firstKeyIndex);
            //curve.GetKeyIndex(lastFrame, out int lastKeyIndex);
            //for (int i = firstKeyIndex; i <= lastKeyIndex; i++)
            //{
            //    keys.Add(i);
            //}
            return keys;
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

        double[,] ds_dtheta(int p, int K)
        {

            double[,] Js = new double[7, p];
            float dtheta = Mathf.Pow(10, -2);

            for (int l = 0; l < animationCount; l++)
            {
                AnimationSet currentAnim = animationList[l];
                //rotation
                for (int i = 0; i < 3; i++)
                {
                    AnimatableProperty property = (AnimatableProperty)i + 3;
                    Curve curve = currentAnim.GetCurve(property);

                    for (int k = 0; k < K; k++)
                    {
                        //Debug.Log("k is : " + k);
                        curve.GetKeyIndex(requiredKeyframe[k], out int index);
                        Vector2 inTangent = curve.keys[index].inTangent;
                        Vector2 outTangent = curve.keys[index].outTangent;

                        for (int m = 0; m < 4; m++)
                        {
                            int col = 12 * K * l + 4 * (i * K + k) + m;

                            if (m == 0)
                            {
                                inTangent.x += dtheta;
                            }
                            if (m == 1)
                            {
                                inTangent.y += dtheta;
                            }
                            if (m == 2)
                            {
                                outTangent.x += dtheta;
                            }
                            if (m == 3)
                            {
                                outTangent.y += dtheta;
                            }
                            ModifyTangents(curve, index, inTangent, outTangent);

                            Matrix4x4 mat = FrameMatrix(currentFrame, animationList);
                            Maths.DecomposeMatrix(mat, out Vector3 plusPos, out Quaternion plusRot, out Vector3 plusScale);
                            Vector3 position_plus = plusPos;
                            Quaternion rotation_plus = plusRot;

                            if (m == 0)
                            {
                                inTangent.x -= dtheta;
                            }
                            if (m == 1)
                            {
                                inTangent.y -= dtheta;
                            }
                            if (m == 2)
                            {
                                outTangent.x -= dtheta;
                            }
                            if (m == 3)
                            {
                                outTangent.y -= dtheta;
                            }
                            ModifyTangents(curve, index, inTangent, outTangent);

                            Matrix4x4 minusMatrix = FrameMatrix(currentFrame, animationList);
                            Maths.DecomposeMatrix(minusMatrix, out Vector3 minusPos, out Quaternion minusRot, out Vector3 minusScale);
                            Vector3 position_minus = minusPos;
                            Quaternion rotation_minus = minusRot;

                            Js[0, col] = (double)(position_plus.x - position_minus.x) / (dtheta);
                            Js[1, col] = (double)(position_plus.y - position_minus.y) / (dtheta);
                            Js[2, col] = (double)(position_plus.z - position_minus.z) / (dtheta);
                            Js[3, col] = (double)(rotation_plus.x - rotation_minus.x) / (dtheta);
                            Js[4, col] = (double)(rotation_plus.y - rotation_minus.y) / (dtheta);
                            Js[5, col] = (double)(rotation_plus.z - rotation_minus.z) / (dtheta);
                            Js[6, col] = (double)(rotation_plus.w - rotation_minus.w) / (dtheta);

                            //double sum =
                            //    Js[0, col] +
                            //    Js[1, col] +
                            //    Js[2, col] +
                            //    Js[3, col] +
                            //    Js[4, col] +
                            //    Js[5, col] +
                            //    Js[6, col];
                            //if (sum == 0) Debug.Log(m);

                        }
                    }
                }
            }
            //positions
            for (int i = 3; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i - 3;
                Curve curve = animationList[0].GetCurve(property);

                for (int k = 0; k < K; k++)
                {
                    curve.GetKeyIndex(requiredKeyframe[k], out int index);
                    Vector2 inTangent = curve.keys[index].inTangent;
                    Vector2 outTangent = curve.keys[index].outTangent;

                    for (int m = 0; m < 4; m++)
                    {
                        int col = 12 * K * animationCount + 4 * ((i - 3) * K + k) + m;

                        if (m == 0)
                        {
                            inTangent.x += dtheta;
                        }
                        if (m == 1)
                        {
                            inTangent.y += dtheta;
                        }
                        if (m == 2)
                        {
                            outTangent.x += dtheta;
                        }
                        if (m == 3)
                        {
                            outTangent.y += dtheta;
                        }
                        ModifyTangents(curve, index, inTangent, outTangent);

                        Matrix4x4 mat = FrameMatrix(currentFrame, animationList);
                        Maths.DecomposeMatrix(mat, out Vector3 plusPos, out Quaternion plusRot, out Vector3 plusScale);


                        if (m == 0)
                        {
                            inTangent.x -= dtheta;
                        }
                        if (m == 1)
                        {
                            inTangent.y -= dtheta;
                        }
                        if (m == 2)
                        {
                            outTangent.x -= dtheta;
                        }
                        if (m == 3)
                        {
                            outTangent.y -= dtheta;
                        }
                        ModifyTangents(curve, index, inTangent, outTangent);

                        Matrix4x4 minusMatrix = FrameMatrix(currentFrame, animationList);
                        Maths.DecomposeMatrix(minusMatrix, out Vector3 minusPos, out Quaternion minusRot, out Vector3 minusScale);

                        Js[0, col] = (double)(plusPos.x - minusPos.x) / (dtheta);
                        Js[1, col] = (double)(plusPos.y - minusPos.y) / (dtheta);
                        Js[2, col] = (double)(plusPos.z - minusPos.z) / (dtheta);
                        Js[3, col] = (double)(plusRot.x - minusRot.x) / (dtheta);
                        Js[4, col] = (double)(plusRot.y - minusRot.y) / (dtheta);
                        Js[5, col] = (double)(plusRot.z - minusRot.z) / (dtheta);
                        Js[6, col] = (double)(plusRot.w - minusRot.w) / (dtheta);
                    }
                }

            }

            return Js;
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
        double[] InitializeUBound(int n)
        {
            double[] u = new double[n];
            for (int i = 0; i < n; i++)
            {
                u[i] = -100d;
            }
            return u;
        }

        double[] InitializeVBound(int n)
        {

            double[] v = new double[n];
            for (int i = 0; i < n; i++)
            {
                v[i] = 100d;
            }
            return v;
        }

        double[] LowerBound(int p, int K)
        {
            double[] lowerBound = new double[p];

            for (int aIndex = 0; aIndex < animationCount; aIndex++)
            {
                AnimationSet animation = animationList[aIndex];
                Vector3 u = controllers[aIndex].LowerAngleBound;
                Vector3 v = controllers[aIndex].UpperAngleBound;

                Matrix4x4 rtsMatrixM = animation.GetTranformMatrix(firstFrame);
                Maths.DecomposeMatrix(rtsMatrixM, out Vector3 posM, out Quaternion rotationM, out Vector3 scaleM);
                Vector3 axesM = rotationBounds(rotationM);

                Matrix4x4 rtsMatrixC = animation.GetTranformMatrix(currentFrame);
                Maths.DecomposeMatrix(rtsMatrixC, out Vector3 posC, out Quaternion rotationC, out Vector3 scaleC);
                Vector3 axesC = rotationBounds(rotationC);

                Matrix4x4 rtsMatrixP = animation.GetTranformMatrix(lastFrame);
                Maths.DecomposeMatrix(rtsMatrixP, out Vector3 posN, out Quaternion rotationP, out Vector3 scaleN);
                Vector3 axesP = rotationBounds(rotationP);

                for (int pIndex = 0; pIndex < 3; pIndex++)
                {
                    AnimatableProperty property = (AnimatableProperty)pIndex + 3;

                    //animation.GetCurve(property).Evaluate(firstFrame, out float valueM);
                    //animation.GetCurve(property).Evaluate(currentFrame, out float valueC);
                    //animation.GetCurve(property).Evaluate(lastFrame, out float valueP);
                    float valueM = axesM[pIndex];
                    float valueC = axesC[pIndex];
                    float valueP = axesP[pIndex];

                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K) + 0] = animation.transform.name + " " + property + "k- in.x";
                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K) + 1] = animation.transform.name + " " + property + "k- in.y";
                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K) + 2] = animation.transform.name + " " + property + "k- out.x";
                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K) + 3] = animation.transform.name + " " + property + "k- out.y";
                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K + 1) + 0] = animation.transform.name + " " + property + "k+ in.x";
                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K + 1) + 1] = animation.transform.name + " " + property + "k+ in.y";
                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K + 1) + 2] = animation.transform.name + " " + property + "k+ out.x";
                    tanNames[1, 12 * K * aIndex + 4 * (pIndex * K + 1) + 3] = animation.transform.name + " " + property + "k+ out.y";

                    //k- in.x -> not used
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K) + 0] = -100d;
                    //k- in.y -> not used
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K) + 1] = -100d;

                    //k- out.x -> 0
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K) + 2] = 0;
                    //k- out.y -> -psi(vi, k-)
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K) + 3] = (4d / 3d) * (u[pIndex] - valueC);

                    //k+ in.x -> 0
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 0] = 0;
                    //k+ in.y -> phi(ui, k+)
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 1] = -(4d / 3d) * (v[pIndex] - valueC);

                    //k+ out.x -> not used
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 2] = -100d;
                    //k+ out.y -> not used
                    lowerBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 3] = -100d;
                }
            }

            AnimationSet rootAnim = animationList[0];
            for (int pIndex = 3; pIndex < 6; pIndex++)
            {
                AnimatableProperty property = (AnimatableProperty)pIndex - 3;
                for (int kIndex = 0; kIndex < K; kIndex++)
                {
                    lowerBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 0] = 0;
                    lowerBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 1] = 0;
                    lowerBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 2] = 0;
                    lowerBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 3] = 0;
                }
            }

            return lowerBound;
        }

        double[] UpperBound(int p, int K)
        {
            double[] upperBound = new double[p];

            for (int aIndex = 0; aIndex < animationCount; aIndex++)
            {
                AnimationSet animation = animationList[aIndex];
                Vector3 u = controllers[aIndex].LowerAngleBound;
                Vector3 v = controllers[aIndex].UpperAngleBound;

                Matrix4x4 rtsMatrixM = animation.GetTranformMatrix(firstFrame);
                Maths.DecomposeMatrix(rtsMatrixM, out Vector3 posM, out Quaternion rotationM, out Vector3 scaleM);
                Vector3 axesM = rotationBounds(rotationM);

                Matrix4x4 rtsMatrixC = animation.GetTranformMatrix(currentFrame);
                Maths.DecomposeMatrix(rtsMatrixC, out Vector3 posC, out Quaternion rotationC, out Vector3 scaleC);
                Vector3 axesC = rotationBounds(rotationC);

                Matrix4x4 rtsMatrixP = animation.GetTranformMatrix(lastFrame);
                Maths.DecomposeMatrix(rtsMatrixP, out Vector3 posN, out Quaternion rotationP, out Vector3 scaleN);
                Vector3 axesP = rotationBounds(rotationP);

                for (int pIndex = 0; pIndex < 3; pIndex++)
                {
                    AnimatableProperty property = (AnimatableProperty)pIndex + 3;

                    //animation.GetCurve(property).Evaluate(firstFrame, out float valueM);
                    //animation.GetCurve(property).Evaluate(currentFrame, out float valueC);
                    //animation.GetCurve(property).Evaluate(lastFrame, out float valueP);
                    float valueM = axesM[pIndex];
                    float valueC = axesC[pIndex];
                    float valueP = axesP[pIndex];

                    //k- in.x -> not used
                    upperBound[12 * K * aIndex + 4 * (pIndex * K) + 0] = 100d;
                    //k- in.y -> not used
                    upperBound[12 * K * aIndex + 4 * (pIndex * K) + 1] = 100d;

                    //k- out.x -> tk - tk-1
                    upperBound[12 * K * aIndex + 4 * (pIndex * K) + 2] = currentFrame - firstFrame;
                    //k- out.y -> -phi(ui, k-)
                    upperBound[12 * K * aIndex + 4 * (pIndex * K) + 3] = (4d / 3d) * (v[pIndex] - valueC);

                    //k+ in.x -> tk+1 - tk
                    upperBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 0] = lastFrame - currentFrame;
                    //k+ in.y -> psi(vi, k+)
                    upperBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 1] = -(4d / 3d) * (u[pIndex] - valueC);

                    //k+ out.x -> not used
                    upperBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 2] = 100d;
                    //k+ out.y -> not used
                    upperBound[12 * K * aIndex + 4 * (pIndex * K + 1) + 3] = 100d;
                }

            }
            AnimationSet rootAnim = animationList[0];
            for (int pIndex = 3; pIndex < 6; pIndex++)
            {
                AnimatableProperty property = (AnimatableProperty)pIndex - 3;
                for (int kIndex = 0; kIndex < K; kIndex++)
                {
                    upperBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 0] = 0;
                    upperBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 1] = 0;
                    upperBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 2] = 0;
                    upperBound[12 * K * animationCount + 4 * ((pIndex - 3) * K + kIndex) + 3] = 0;
                }
            }
            return upperBound;
        }

        private float Phi(float u, float citk, float citkp)
        {
            return (4f / 3f) * (u - Mathf.Min(citk, citkp));
        }
        private float Psi(float v, float citk, float citkp)
        {
            return (4f / 3f) * (v - Mathf.Max(citk, citkp));
        }


        private Vector3 rotationBounds(Quaternion rotation)
        {
            Vector3 res = new Vector3();
            rotation.x /= rotation.w;
            rotation.y /= rotation.w;
            rotation.z /= rotation.w;
            rotation.w = 1;

            res.x = 2f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);

            res.y = 2f * Mathf.Rad2Deg * Mathf.Atan(rotation.y);

            res.z = 2f * Mathf.Rad2Deg * Mathf.Atan(rotation.z);

            return res;
        }

        double[] LowerBoundConstraints(double[] theta, double[] u, double[] v, int p, int K, int globalKeyframes)
        {
            double[] lowerBound = new double[p];

            //Rotation Curves
            for (int l = 0; l < animationCount; l++)
            {

                AnimationSet animation = animationList[l];

                for (int i = 0; i < 3; i++)
                {

                    AnimatableProperty property = (AnimatableProperty)i + 3;

                    for (int k = 0; k < K; k++)
                    {
                        //in .x
                        lowerBound[12 * K * l + 4 * (i * K + k) + 0] = 0;//-theta[12 * K * l + 4 * (i * K + k) + 0];
                                                                         //in.y
                        lowerBound[12 * K * l + 4 * (i * K + k) + 1] = psi(v[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                        //out.x
                        lowerBound[12 * K * l + 4 * (i * K + k) + 2] = 0;// -theta[12 * K * l + 4 * (i * K + k) + 2];
                                                                         //out.y
                        lowerBound[12 * K * l + 4 * (i * K + k) + 3] = -phi(u[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                    }
                }
            }

            //Position curves for the root    
            for (int i = 3; i < 6; i++)
            {
                AnimationSet animation = animationList[0];
                AnimatableProperty property = (AnimatableProperty)i - 3;

                for (int k = 0; k < K; k++)
                {
                    lowerBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0] = -theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0];
                    lowerBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1] = psi(v[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1];
                    lowerBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2] = -theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2];
                    lowerBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3] = -phi(u[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3];
                }
            }

            return lowerBound;
        }

        double[] UpperBoundConstraints(double[] theta, double[] u, double[] v, int p, int K, int globalKeyframes)
        {
            double[] upperBound = new double[p];

            //Rotation curves
            for (int l = 0; l < animationCount; l++)
            {
                AnimationSet animation = animationList[l];

                for (int i = 0; i < 3; i++)
                {

                    AnimatableProperty property = (AnimatableProperty)i + 3;
                    Curve curve = animation.GetCurve(property);

                    for (int k = 0; k < K; k++)
                    {
                        curve.GetKeyIndex(requiredKeyframe[k], out int index);
                        if (index > 0 && index < curve.keys.Count - 1)
                        {
                            int tkp1 = curve.keys[index + 1].frame;
                            int tk = curve.keys[index].frame;
                            int tkm1 = curve.keys[index - 1].frame;

                            upperBound[12 * K * l + 4 * (i * K + k) + 0] = tk - tkm1 - theta[12 * K * l + 4 * (i * K + k) + 0];
                            upperBound[12 * K * l + 4 * (i * K + k) + 1] = phi(u[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                            upperBound[12 * K * l + 4 * (i * K + k) + 2] = tkp1 - tk - theta[12 * K * l + 4 * (i * K + k) + 2];
                            upperBound[12 * K * l + 4 * (i * K + k) + 3] = -psi(v[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                        }

                        else if (index == 0)
                        {
                            int tk = curve.keys[index].frame;
                            int tkp1 = curve.keys[index + 1].frame;

                            upperBound[12 * K * l + 4 * (i * K + k) + 0] = tkp1 - tk - theta[12 * K * l + 4 * (i * K + k) + 0];
                            upperBound[12 * K * l + 4 * (i * K + k) + 1] = phi(u[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                            upperBound[12 * K * l + 4 * (i * K + k) + 2] = tkp1 - tk - theta[12 * K * l + 4 * (i * K + k) + 2];
                            upperBound[12 * K * l + 4 * (i * K + k) + 3] = -psi(v[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                        }

                        else
                        {
                            int tk = curve.keys[index].frame;
                            int tkm1 = curve.keys[index - 1].frame;

                            upperBound[12 * K * l + 4 * (i * K + k) + 0] = tk - tkm1 - theta[12 * K * l + 4 * (i * K + k) + 0];
                            upperBound[12 * K * l + 4 * (i * K + k) + 1] = phi(u[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                            upperBound[12 * K * l + 4 * (i * K + k) + 2] = tk - tkm1 - theta[12 * K * l + 4 * (i * K + k) + 2];
                            upperBound[12 * K * l + 4 * (i * K + k) + 3] = -psi(v[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                        }

                    }
                }
            }

            //Position curves for the root
            for (int i = 3; i < 6; i++)
            {
                AnimationSet animation = animationList[0];

                AnimatableProperty property = (AnimatableProperty)i - 3;
                Curve curve = animation.GetCurve(property);

                for (int k = 0; k < K; k++)
                {
                    curve.GetKeyIndex(requiredKeyframe[k], out int index);
                    if (index > 0 && index < curve.keys.Count - 1)
                    {
                        int tkp1 = curve.keys[index + 1].frame;
                        int tk = curve.keys[index].frame;
                        int tkm1 = curve.keys[index - 1].frame;

                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0] = tk - tkm1 - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1] = -phi(u[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2] = tkp1 - tk - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3] = psi(v[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3];
                    }

                    else if (index == 0)
                    {
                        int tk = curve.keys[index].frame;
                        int tkp1 = curve.keys[index + 1].frame;

                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0] = tkp1 - tk - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1] = -phi(u[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2] = tkp1 - tk - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3] = psi(v[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3];
                    }

                    else
                    {
                        int tk = curve.keys[index].frame;
                        int tkm1 = curve.keys[index - 1].frame;

                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0] = tk - tkm1 - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 0];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1] = -phi(u[i], animation, property, requiredKeyframe[k], false) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 1];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2] = tk - tkm1 - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 2];
                        upperBound[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3] = psi(v[i], animation, property, requiredKeyframe[k], true) - theta[12 * K * animationCount + 4 * ((i - 3) * K + k) + 3];
                    }

                }
            }

            return upperBound;
        }

        double psi(double v, AnimationSet currentAnim, AnimatableProperty property, int frame, bool plus)
        {
            Curve curve = currentAnim.GetCurve(property);
            curve.GetKeyIndex(frame, out int index);
            if (plus)
            {

                if (curve.keys.Count > index + 1)
                {
                    float cik = curve.keys[index].value;
                    float cikp1 = curve.keys[index + 1].value;
                    return (double)((4f / 3f) * ((float)v - Mathf.Max(cik, cikp1)));
                }

                else
                {
                    float cik = curve.keys[index].value;
                    return (double)((4f / 3f) * ((float)v - cik));
                }

            }

            else
            {

                if (index > 0)
                {
                    float cik = curve.keys[index].value;
                    float cikm1 = curve.keys[index - 1].value;
                    return (double)((4f / 3f) * ((float)v - Mathf.Max(cik, cikm1)));
                }

                else
                {
                    float cik = curve.keys[index].value;
                    return (double)((4f / 3f) * ((float)v - cik));
                }

            }
        }

        double phi(double u, AnimationSet currentAnim, AnimatableProperty property, int frame, bool plus)
        {

            Curve curve = currentAnim.GetCurve(property);
            curve.GetKeyIndex(frame, out int index);
            if (plus)
            {

                if (curve.keys.Count > index + 1)
                {
                    float cik = curve.keys[index].value;
                    float cikp1 = curve.keys[index + 1].value;
                    return (double)((4f / 3f) * ((float)u - Mathf.Min(cik, cikp1)));
                }

                else
                {
                    float cik = curve.keys[index].value;
                    return (double)((4f / 3f) * ((float)u - cik));
                }

            }

            else
            {

                if (index > 0)
                {
                    float cik = curve.keys[index].value;
                    float cikm1 = curve.keys[index - 1].value;
                    return (double)((4f / 3f) * ((float)u - Mathf.Min(cik, cikm1)));
                }

                else
                {
                    float cik = curve.keys[index].value;
                    return (double)((4f / 3f) * ((float)u - cik));
                }

            }
        }
        Dictionary<int, int[]> FindConstraintsInRho(int nb_pins)
        {
            Dictionary<int, int[]> Table = new Dictionary<int, int[]>();
            int pointer = 0;
            for (int i = 0; i < nb_pins; i++)
            {
                int start = constraints.startFrames[i];
                int end = constraints.endFrames[i];
                Table.Add(i, new int[2] { pointer, pointer + end - start });
                pointer += end - start + 1;
            }
            return Table;
        }

        (double[], double[]) RhoAndRhoPrime(Dictionary<int, int[]> indexTable, int nb_pins)
        {
            int size = indexTable[nb_pins - 1][1] + 1;
            double[] rho = new double[size];
            double[] rho_prime = new double[size];

            int pointer = 0;
            for (int i = 0; i < nb_pins; i++)
            {
                int start = constraints.startFrames[i];
                int end = constraints.endFrames[i];
                Curve Property = animationList[constraints.gameObjectIndices[i]].curves[constraints.properties[i]];

                for (int frame = start; frame <= end; frame++)
                {
                    Property.Evaluate(frame, out float val);

                    rho[pointer] = (double)val;
                    rho_prime[pointer] = (double)constraints.values[i];

                    pointer += 1;
                }
            }

            return (rho, rho_prime);

        }

        double[,] drho_dtheta(Dictionary<int, int[]> indexTable, int nb_pins, int p, int K)
        {
            int size = indexTable[nb_pins - 1][1] + 1;
            double[,] Jrho = new double[size, p];

            for (int pin = 0; pin < nb_pins; pin++)
            {
                int child_index = constraints.gameObjectIndices[pin];
                int pointer = 0;
                for (int frame = constraints.startFrames[pin]; frame <= constraints.endFrames[pin]; frame++)
                {
                    double[,] Js = ds_dtheta(p, K);
                    int i = (int)constraints.properties[pin];
                    for (int j = 0; j < Js.GetUpperBound(1) + 1; j++)
                    {
                        Jrho[indexTable[pin][0] + pointer, j] = Js[i, j];
                    }
                    pointer += 1;
                }
            }

            return Jrho;

        }

        (double[,], int[]) FindLinearEqualityConstraints(double[,] A, double[] b)
        {
            double epsilon = 0.01d;
            int K_size = A.GetUpperBound(0) + 1;
            int N_size = A.GetUpperBound(1) + 1;
            double[,] C = new double[2 * K_size, N_size + 1];
            int[] CT = new int[2 * K_size];

            for (int i = 0; i < K_size; i++)
            {
                CT[i] = -1;
                CT[K_size + i] = 1;
                C[i, N_size] = b[i] + epsilon;
                C[K_size + i, N_size] = b[i] - epsilon;
                for (int j = 0; j < N_size; j++)
                {
                    C[i, j] = A[i, j];
                    C[K_size + i, j] = A[i, j];
                }
            }

            return (C, CT);
        }


        public Matrix4x4 FrameMatrix(int frame, List<AnimationSet> animations)
        {
            Profiler.BeginSample("frame matrix");
            Matrix4x4 trsMatrix = animations[0].transform.parent.localToWorldMatrix;

            for (int i = 0; i < animationList.Count; i++)
            {
                trsMatrix = trsMatrix * GetBoneMatrix(animations[i], frame);
            }
            Profiler.EndSample();
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
    }

}