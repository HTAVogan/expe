using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{

    public class TangentSimpleSolver
    {
        private Vector3 positionTarget;
        private Quaternion rotationTarget;
        public AnimationSet ObjectAnimation;
        private int currentFrame;
        private int size;

        public List<int> RequiredKeyframeIndices;

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
            public Vector3 euler_orientation;
            public int time;
        }

        Constraints constraints;

        public TangentSimpleSolver(Vector3 targetPosition, Quaternion targetRotation, AnimationSet animation, int frame, int zoneSize)
        {
            positionTarget = targetPosition;
            rotationTarget = targetRotation;
            ObjectAnimation = animation;
            currentFrame = frame;
            size = zoneSize;
            constraints = new Constraints()
            { endFrames = new List<int>(), gameObjectIndices = new List<int>(), properties = new List<AnimatableProperty>(), startFrames = new List<int>(), values = new List<float>() };

        }

        public bool TrySolver()
        {
            ObjectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(currentFrame - size, out int firstIndex);
            int firstFrame = ObjectAnimation.curves[AnimatableProperty.PositionX].keys[firstIndex].frame;
            ObjectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(currentFrame + size, out int lastIndex);
            int lastFrame = ObjectAnimation.curves[AnimatableProperty.PositionX].keys[lastIndex].frame;

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

            RequiredKeyframeIndices = FindRequiredTangents(firstFrame, lastFrame, ObjectAnimation.GetCurve(AnimatableProperty.PositionX));
            int K = RequiredKeyframeIndices.Count;
            int totalKeyframes = ObjectAnimation.GetCurve(AnimatableProperty.PositionX).keys.Count;
            int n = 6;
            int p = 24 * K;
            int pinsNB = constraints.gameObjectIndices.Count;

            double[] theta = GetAllTangents(p, K, RequiredKeyframeIndices);
            double[,] Theta = ColumnArrayToArray(theta);

            State currentState = GetCurrentState(currentFrame);
            State desiredState = new State()
            {
                position = positionTarget,
                euler_orientation = rotationTarget.eulerAngles,
                time = currentFrame
            };
            double[,] Js = ds_dtheta(currentFrame, n, p, K, RequiredKeyframeIndices);
            double[,] DT_D = new double[p, p];
            for (int i = 0; i < p; i++)
            {
                DT_D[i, i] = 0d * 0d;
            }

            double[,] Delta_s_prime = new double[6, 1];
            for (int i = 0; i <= 2; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            for (int i = 3; i <= 5; i++)
            {
                Delta_s_prime[i, 0] = -Mathf.DeltaAngle(desiredState.euler_orientation[i - 3], currentState.euler_orientation[i - 3]);
            }

            double[,] TT_T = new double[p, p];
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

            double wm = 100d;
            double wb = 1d;
            double wd = 1d;

            double[,] Q_opt = Add(Add(Multiply(2d * wm, Multiply(Transpose(Js), Js)), Add(Multiply(2d * wd, DT_D), Multiply(2d * wb, TT_T))), Multiply((double)Mathf.Pow(10, -6), Identity(p)));

            double[,] B_opt = Add(Multiply(-2d * wm, Multiply(Transpose(Js), Delta_s_prime)), Multiply(2d * wb, Multiply(TT_T, Theta)));
            double[] b_opt = ArrayToColumnArray(B_opt);

            double[] u = InitializeUBound(n);
            double[] v = InitializeVBound(n);

            double[] lowerBound = LowerBoundConstraints(theta, u, v, p, K, RequiredKeyframeIndices, totalKeyframes);
            double[] upperBound = UpperBoundConstraints(theta, u, v, p, K, RequiredKeyframeIndices, totalKeyframes);

            double[] delta_theta_0 = new double[p];
            double[] delta_theta;
            double[] s = new double[p];
            for (int i = 0; i < p; i++)
            {
                s[i] = 1d;
                delta_theta_0[i] = 0d;
            }

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
                double[,] Jrho = drho_dtheta(indexTable, pinsNB, p, K, RequiredKeyframeIndices);

                (double[,], int[]) linearConstraints = FindLinearEqualityConstraints(Jrho, delta_rho_prime);
                double[,] C = linearConstraints.Item1;
                int[] CT = linearConstraints.Item2;
                int K_size = CT.Length;

                alglib.minqpsetlc(state_opt, C, CT, K_size);
            }

            alglib.minqpsetscale(state_opt, s);

            //alglib.minqpsetalgoquickqp(state_opt, 0.0, 0.0, 0.0, 0, true);
            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            double[] new_theta = new double[p];
            for (int i = 0; i < p; i++)
            {
                new_theta[i] = delta_theta[i] + theta[i];
            }

            for (int i = 0; i < p; i++)
            {
                if (System.Double.IsNaN(delta_theta[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < 6; i++)
            {

                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.curves[property];

                for (int k = 0; k < K; k++)
                {
                    Vector2 inTangent = new Vector2((float)new_theta[4 * (i * K + k) + 0], (float)new_theta[4 * (i * K + k) + 1]);
                    Vector2 outTangent = new Vector2((float)new_theta[4 * (i * K + k) + 2], (float)new_theta[4 * (i * K + k) + 3]);
                    ModifyTangents(curve, RequiredKeyframeIndices[k], inTangent, outTangent);
                    //if (property == AnimatableProperty.PositionX) Debug.Log("k" + k + " - " + property + " / " + inTangent + " / " + outTangent);
                }
            }

            //State newState = GetCurrentState(currentFrame);

            return true;
        }

        private State GetCurrentState(int currentFrame)
        {
            float[] data = new float[6];
            for (int i = 0; i < data.Length; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                ObjectAnimation.GetCurve(property).Evaluate(currentFrame, out data[i]);
            }
            return new State()
            {
                position = new Vector3(data[0], data[1], data[2]),
                euler_orientation = new Vector3(data[3], data[4], data[5]),
                time = currentFrame
            };

        }

        private double[] GetAllTangents(int p, int K, List<int> requieredKeys)
        {
            double[] theta = new double[p];
            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.GetCurve(property);
                for (int k = 0; k < K; k++)
                {
                    AnimationKey key = curve.keys[requieredKeys[k]];
                    theta[4 * (i * K + k) + 0] = key.inTangent.x;
                    theta[4 * (i * K + k) + 1] = key.inTangent.y;
                    theta[4 * (i * K + k) + 2] = key.outTangent.x;
                    theta[4 * (i * K + k) + 3] = key.outTangent.y;
                }
            }

            return theta;
        }

        public void ModifyTangents(Curve curve, int index, Vector2 inTangent, Vector2 outTangent)
        {
            curve.keys[index].inTangent = inTangent;
            curve.keys[index].outTangent = outTangent;
            curve.ComputeCacheValuesAt(index);
        }

        private List<int> FindRequiredTangents(int firstFrame, int lastFrame, Curve curve)
        {
            curve.GetKeyIndex(firstFrame, out int firstKeyIndex);
            curve.GetKeyIndex(lastFrame, out int lastKeyIndex);
            List<int> keys = new List<int>() { firstKeyIndex, lastKeyIndex };
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

        double[,] ds_dc(int frame, int n)
        {
            double[,] Js1 = new double[6, n];

            ObjectAnimation.curves[AnimatableProperty.PositionX].Evaluate(frame, out float x);
            ObjectAnimation.curves[AnimatableProperty.PositionY].Evaluate(frame, out float y);
            ObjectAnimation.curves[AnimatableProperty.PositionZ].Evaluate(frame, out float z);
            Vector3 sp = new Vector3(x, y, z);

            for (int j = 0; j < 6; j++)
            {
                if (j <= 2)
                {
                    Js1[j, j] = 1d;
                }
                else
                {
                    Vector3 v = new Vector3(0, 0, 0);
                    v[j - 3] = 1f;
                    Vector3 r = new Vector3(x, y, z);
                    Vector3 derive_position = Vector3.Cross(v, sp - r);
                    Js1[0, j] = derive_position[0];
                    Js1[1, j] = derive_position[1];
                    Js1[2, j] = derive_position[2];
                    Js1[3, j] = v[0];
                    Js1[4, j] = v[1];
                    Js1[5, j] = v[2];
                }
            }
            return Js1;
        }

        double[,] ds_dtheta(int frame, int n, int p, int K, List<int> requiredKeyframes)
        {
            double[,] Js1 = ds_dc(frame, n);
            double[,] Js2 = dc_dtheta(frame, n, p, K, requiredKeyframes);
            return Multiply(Js1, Js2);
        }

        double[,] dc_dtheta(int frame, int n, int p, int K, List<int> requiredKeyframeIndices)
        {

            double[,] Js2 = new double[n, p];
            float dtheta = Mathf.Pow(10, -4);

            for (int i = 0; i < 6; i++)
            {

                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.curves[property];

                for (int k = 0; k < K; k++)
                {

                    Vector2 inTangent = curve.keys[requiredKeyframeIndices[k]].inTangent;
                    Vector2 outTangent = curve.keys[requiredKeyframeIndices[k]].outTangent;
                    float c_plus, c_minus;

                    int j1 = 4 * (i * K + k);
                    inTangent.x += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    inTangent.x -= 2f * dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j1] = (double)((c_plus - c_minus) / (2f * dtheta));
                    inTangent.x += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);

                    int j2 = 4 * (i * K + k) + 1;
                    inTangent.y += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    inTangent.y -= 2f * dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j2] = (double)((c_plus - c_minus) / (2f * dtheta));
                    inTangent.y += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);

                    int j3 = 4 * (i * K + k) + 2;
                    outTangent.x += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    outTangent.x -= 2f * dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j3] = (double)((c_plus - c_minus) / (2f * dtheta));
                    outTangent.x += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);

                    int j4 = 4 * (i * K + k) + 3;
                    outTangent.y += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    outTangent.y -= 2f * dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j4] = (double)((c_plus - c_minus) / (2f * dtheta));
                    outTangent.y += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                }
            }

            return Js2;
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
                u[i] = -1000d;
            }

            return u;
        }
        double[] InitializeVBound(int n)
        {

            double[] v = new double[n];
            for (int i = 0; i < n; i++)
            {
                v[i] = 1000d;
            }

            return v;
        }

        double[] LowerBoundConstraints(double[] theta, double[] u, double[] v, int p, int K, List<int> requiredKeyframeIndices, int globalKeyframes)
        {
            double[] lowerBound = new double[p];

            for (int i = 0; i < 6; i++)
            {

                AnimatableProperty property = (AnimatableProperty)i;

                for (int k = 0; k < K; k++)
                {
                    lowerBound[4 * (i * K + k) + 0] = -theta[4 * (i * K + k) + 0];
                    lowerBound[4 * (i * K + k) + 1] = -psi(v[i], property, requiredKeyframeIndices[k], false, globalKeyframes) - theta[4 * (i * K + k) + 1];
                    lowerBound[4 * (i * K + k) + 2] = -theta[4 * (i * K + k) + 2];
                    lowerBound[4 * (i * K + k) + 3] = phi(u[i], property, requiredKeyframeIndices[k], true, globalKeyframes) - theta[4 * (i * K + k) + 3];
                }
            }

            return lowerBound;
        }

        double[] UpperBoundConstraints(double[] theta, double[] u, double[] v, int p, int K, List<int> requiredKeyframeIndices, int globalKeyframes)
        {

            double[] upperBound = new double[p];

            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.curves[property];

                for (int k = 0; k < K; k++)
                {

                    if (requiredKeyframeIndices[k] > 0 && requiredKeyframeIndices[k] < globalKeyframes - 1)
                    {
                        int tkp1 = curve.keys[requiredKeyframeIndices[k] + 1].frame;
                        int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                        int tkm1 = curve.keys[requiredKeyframeIndices[k] - 1].frame;

                        upperBound[4 * (i * K + k) + 0] = tk - tkm1 - theta[4 * (i * K + k) + 0];
                        upperBound[4 * (i * K + k) + 1] = -phi(u[i], property, requiredKeyframeIndices[k], false, globalKeyframes) - theta[4 * (i * K + k) + 1];
                        upperBound[4 * (i * K + k) + 2] = tkp1 - tk - theta[4 * (i * K + k) + 2];
                        upperBound[4 * (i * K + k) + 3] = psi(v[i], property, requiredKeyframeIndices[k], true, globalKeyframes) - theta[4 * (i * K + k) + 3];
                    }
                    else if (requiredKeyframeIndices[k] == 0)
                    {
                        int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                        int tkp1 = curve.keys[requiredKeyframeIndices[k] + 1].frame;

                        upperBound[4 * (i * K + k) + 0] = tkp1 - tk - theta[4 * (i * K + k) + 0];
                        upperBound[4 * (i * K + k) + 1] = -phi(u[i], property, requiredKeyframeIndices[k], false, globalKeyframes) - theta[4 * (i * K + k) + 1];
                        upperBound[4 * (i * K + k) + 2] = tkp1 - tk - theta[4 * (i * K + k) + 2];
                        upperBound[4 * (i * K + k) + 3] = psi(v[i], property, requiredKeyframeIndices[k], true, globalKeyframes) - theta[4 * (i * K + k) + 3];
                    }
                    else
                    {
                        int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                        int tkm1 = curve.keys[requiredKeyframeIndices[k] - 1].frame;

                        upperBound[4 * (i * K + k) + 0] = tk - tkm1 - theta[4 * (i * K + k) + 0];
                        upperBound[4 * (i * K + k) + 1] = -phi(u[i], property, requiredKeyframeIndices[k], false, globalKeyframes) - theta[4 * (i * K + k) + 1];
                        upperBound[4 * (i * K + k) + 2] = tk - tkm1 - theta[4 * (i * K + k) + 2];
                        upperBound[4 * (i * K + k) + 3] = psi(v[i], property, requiredKeyframeIndices[k], true, globalKeyframes) - theta[4 * (i * K + k) + 3];
                    }
                }
            }
            return upperBound;
        }

        double psi(double v, AnimatableProperty property, int k, bool plus, int GlobalKeyframes)
        {
            Curve curve = ObjectAnimation.curves[property];

            if (plus)
            {

                if (k < GlobalKeyframes - 1)
                {
                    float cik = curve.keys[k].value;
                    float cikp1 = curve.keys[k + 1].value;
                    return (double)((3f / 4f) * ((float)v - Mathf.Max(cik, cikp1)));
                }

                else
                {
                    float cik = curve.keys[k].value;
                    return (double)((3f / 4f) * ((float)v - cik));
                }

            }

            else
            {

                if (k > 0)
                {
                    float cik = curve.keys[k].value;
                    float cikm1 = curve.keys[k - 1].value;
                    return (double)((3f / 4f) * ((float)v - Mathf.Max(cik, cikm1)));
                }

                else
                {
                    float cik = curve.keys[k].value;
                    return (double)((3f / 4f) * ((float)v - cik));
                }

            }
        }

        double phi(double u, AnimatableProperty property, int k, bool plus, int globalKeyframes)
        {

            Curve curve = ObjectAnimation.curves[property];

            if (plus)
            {

                if (k < globalKeyframes - 1)
                {
                    float cik = curve.keys[k].value;
                    float cikp1 = curve.keys[k + 1].value;
                    return (double)((3f / 4f) * ((float)u - Mathf.Min(cik, cikp1)));
                }

                else
                {
                    float cik = curve.keys[k].value;
                    return (double)((3f / 4f) * ((float)u - cik));
                }

            }

            else
            {

                if (k > 0)
                {
                    float cik = curve.keys[k].value;
                    float cikm1 = curve.keys[k - 1].value;
                    return (double)((3f / 4f) * ((float)u - Mathf.Min(cik, cikm1)));
                }

                else
                {
                    float cik = curve.keys[k].value;
                    return (double)((3f / 4f) * ((float)u - cik));
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
                Curve Property = ObjectAnimation.curves[constraints.properties[i]];

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

        double[,] drho_dtheta(Dictionary<int, int[]> indexTable, int nb_pins, int p, int K, List<int> requiredKeyframes)
        {
            int size = indexTable[nb_pins - 1][1] + 1;
            double[,] Jrho = new double[size, p];

            for (int pin = 0; pin < nb_pins; pin++)
            {
                int child_index = constraints.gameObjectIndices[pin];
                int pointer = 0;
                for (int frame = constraints.startFrames[pin]; frame <= constraints.endFrames[pin]; frame++)
                {
                    double[,] Js = ds_dtheta(child_index, frame, p, K, requiredKeyframes);
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

    }
}
