using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VRtist
{
    public class TangentsSolver
    {

        public struct Constraint
        {
            public List<int> gameObjectIndices;
            public List<int> startFrames;
            public List<int> endFrames;
            public List<int> properties;
            public List<float> values;
        }

        public struct State
        {
            public Vector3 position;
            public Quaternion rotation;
            public int frame;
        }

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private List<AnimationSet> objectHierarchy;
        private int frame;
        private int zoneSize;
        private Constraint objectConstraints;

        private int p;
        private int keyCount;
        List<int> requiredKeyFrames;
        State desiredState;
        State currentState;
        double[,] Js;
        double[] theta;
        double[,] Theta;
        private int n;
        alglib.minqpstate state_opt;
        alglib.minqpreport rep;
        double[,] Q_opt;
        double[] b_opt;
        double[] delta_theta_0;
        double[] delta_theta;
        double[] lowerBound;
        double[] upperBound;
        double[] s;


        public TangentsSolver(Vector3 targetPosition, Quaternion targetRotation, List<AnimationSet> hierarchy, int frame, int zoneSize, Constraint constraints)
        {

            Debug.Log(targetPosition + " / " + targetRotation);

            this.targetPosition = targetPosition;
            this.targetRotation = targetRotation;
            objectHierarchy = hierarchy;
            this.frame = frame;
            this.zoneSize = zoneSize;
            objectConstraints = constraints;
        }

        public bool StepOne()
        {
            objectHierarchy[0].curves[AnimatableProperty.PositionX].GetKeyIndex(frame - zoneSize, out int firstIndex);
            int firstFrame = objectHierarchy[0].curves[AnimatableProperty.PositionX].keys[firstIndex].frame;
            objectHierarchy[0].curves[AnimatableProperty.PositionX].GetKeyIndex(frame + zoneSize, out int lastIndex);
            int lastFrame = objectHierarchy[0].curves[AnimatableProperty.PositionX].keys[lastIndex + 1].frame;

            if (frame < firstFrame) return false;
            if (frame > lastFrame) return false;
            foreach (int sf in objectConstraints.startFrames)
            {
                if (sf < firstFrame) return false;
            }
            foreach (int lf in objectConstraints.endFrames)
            {
                if (lf > lastFrame) return false;
            }

            requiredKeyFrames = FindRequieredTangents(firstFrame, lastFrame, objectHierarchy[0].curves[AnimatableProperty.PositionX]);
            keyCount = requiredKeyFrames.Count - 1;
            int totalKeyframeNB = objectHierarchy[0].curves[AnimatableProperty.PositionX].keys.Count;

            n = 3 * objectHierarchy.Count + 3;
            p = 12 * objectHierarchy.Count * keyCount + 12 * keyCount;
            int nb_pins = objectConstraints.gameObjectIndices.Count;

            theta = GetAllTangents(p, keyCount, objectHierarchy, requiredKeyFrames);
            Theta = ColumnArrayToArray(theta);

            currentState = GetCurrentState(objectHierarchy[objectHierarchy.Count - 1], frame);
            desiredState = new State() { position = targetPosition, rotation = targetRotation, frame = frame };
            Js = ds_dtheta(objectHierarchy, frame, p, objectHierarchy.Count, keyCount, requiredKeyFrames);

            return true;
        }

        public bool StepThree()
        {
            double[,] DT_D = new double[p, p];
            //Root rotation tangents
            for (int i = 0; i < 12 * keyCount; i++)
            {
                DT_D[i, i] = 0d * 0d;
            }
            //Non-root rotation tangents
            for (int i = 12 * keyCount; i < p - 12 * keyCount; i++)
            {
                DT_D[i, i] = 0d * 0d;
            }
            //Root position tangents
            for (int i = p - 12 * keyCount; i < p; i++)
            {
                DT_D[i, i] = 0d * 0d;
            }

            // s' (t) - s( θ ,t)
            double[,] Delta_s_prime = new double[7, 1];
            for (int i = 0; i <= 2; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            for (int i = 3; i <= 6; i++)
            {
                Delta_s_prime[i, 0] = desiredState.rotation[i - 3] - currentState.rotation[i - 3];
            }

            //T is the matrix to insure tangent continuity, here we calculate directly T^T x T
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

            Q_opt = Add(Add(Multiply(2d * wm, Multiply(Transpose(Js), Js)), Add(Multiply(2d * wd, DT_D), Multiply(2d * wb, TT_T))), Multiply((double)Mathf.Pow(10, -6), Identity(p)));

            double[,] B_opt = Add(Multiply(-2d * wm, Multiply(Transpose(Js), Delta_s_prime)), Multiply(2d * wb, Multiply(TT_T, Theta)));
            b_opt = ArrayToColumnArray(B_opt);

            double[] u = InitializeUBound(n);
            double[] v = InitializeVBound(n);

            lowerBound = LowerBoundConstraints(theta, u, v, objectHierarchy.Count, p, keyCount, requiredKeyFrames);
            upperBound = UpperBoundConstraints(theta, u, v, objectHierarchy.Count, p, keyCount, requiredKeyFrames);

            delta_theta_0 = new double[p];
            s = new double[p];
            for (int i = 0; i < p; i++)
            {
                s[i] = 1d;
                delta_theta_0[i] = 0d;
            }


            alglib.minqpcreate(p, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);

            return true;
        }

        public bool StepFive()
        {
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);

            if (objectConstraints.gameObjectIndices.Count != 0)
            {

                Dictionary<int, int[]> indexTable = FindConstraintsInRho(objectConstraints.gameObjectIndices.Count, objectConstraints);

                (double[], double[]) rhos = RhoAndRhoPrime(indexTable, objectConstraints.gameObjectIndices.Count);
                double[] rho = rhos.Item1;
                double[] rho_prime = rhos.Item2;
                double[,] Delta_rho_prime = Add(ColumnArrayToArray(rho_prime), Multiply(-1d, ColumnArrayToArray(rho)));
                double[] delta_rho_prime = ArrayToColumnArray(Delta_rho_prime);
                double[,] Jrho = drho_dtheta(indexTable, objectConstraints.gameObjectIndices.Count, p, objectHierarchy[objectHierarchy.Count - 1].GetCurve(AnimatableProperty.PositionX).keys.Count, keyCount, requiredKeyFrames);

                (double[,], int[]) linearConstraints = FindLinearEqualityConstraints(Jrho, delta_rho_prime);
                double[,] C = linearConstraints.Item1;
                int[] CT = linearConstraints.Item2;
                int K_size = CT.Length;

                alglib.minqpsetlc(state_opt, C, CT, K_size);
            }

            alglib.minqpsetscale(state_opt, s);

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            //θ'
            double[] new_theta = new double[p];
            for (int i = 0; i < p; i++)
            {
                new_theta[i] = delta_theta[i] + theta[i];
            }

            //Check if the optimization found real solutions
            for (int i = 0; i < p; i++)
            {
                if (System.Double.IsNaN(delta_theta[i]))
                {
                    return false;
                }
            }

            //Rotation curves
            for (int l = 0; l < objectHierarchy.Count; l++)
            {
                for (int i = 0; i < 3; i++)
                {
                    AnimationSet anim = objectHierarchy[l];
                    AnimatableProperty property = (AnimatableProperty)i + 3;
                    Curve curve = anim.GetCurve(property);

                    for (int k = 0; k < keyCount; k++)
                    {
                        Vector2 inTangent = new Vector2((float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 0], (float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 1]);
                        Vector2 outTangent = new Vector2((float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 2], (float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 3]);
                        curve.SetTangents(requiredKeyFrames[k], inTangent, outTangent);
                    }
                }
            }

            AnimationSet rootAnim = objectHierarchy[0];
            //Position curves of the root
            for (int i = 3; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i - 3;
                Curve curve = rootAnim.GetCurve(property);

                for (int k = 0; k < keyCount; k++)
                {
                    Vector2 inTangent = new Vector2((float)new_theta[12 * keyCount * objectHierarchy.Count + 4 * ((i - 3) * keyCount + k) + 0], (float)new_theta[12 * keyCount * objectHierarchy.Count + 4 * ((i - 3) * keyCount + k) + 1]);
                    Vector2 outTangent = new Vector2((float)new_theta[12 * keyCount * objectHierarchy.Count + 4 * ((i - 3) * keyCount + k) + 2], (float)new_theta[12 * keyCount * objectHierarchy.Count + 4 * ((i - 3) * keyCount + k) + 3]);
                    curve.SetTangents(requiredKeyFrames[k], inTangent, outTangent);
                }
            }
            return true;
        }

        public double[] GetAllTangents(int p, int K, List<AnimationSet> animations, List<int> RequieredKeys)
        {
            double[] theta = new double[p];
            int nbPoints = animations.Count;

            //Root position curves
            for (int i = 3; i < 6; i++)
            {
                AnimationSet anim = animations[0];
                AnimatableProperty property = (AnimatableProperty)i-3;
                Curve curve = anim.GetCurve(property);

                for (int k = 0; k < K; k++)
                {
                    double[] tangents = GetTangents(curve, RequieredKeys[k]);
                    theta[12 * K * nbPoints + 4 * ((i - 3) * K + k) + 0] = tangents[0];
                    theta[12 * K * nbPoints + 4 * ((i - 3) * K + k) + 1] = tangents[1];
                    theta[12 * K * nbPoints + 4 * ((i - 3) * K + k) + 2] = tangents[2];
                    theta[12 * K * nbPoints + 4 * ((i - 3) * K + k) + 3] = tangents[3];
                }
            }
            //Rotation curves
            for (int l = 0; l < nbPoints; l++)
            {
                for (int i = 0; i < 3; i++)
                {
                    AnimationSet anim = animations[l];
                    AnimatableProperty property = (AnimatableProperty)i+3;
                    Curve curve = anim.GetCurve(property);
                    for (int k = 0; k < K; k++)
                    {
                        double[] tangents = GetTangents(curve, RequieredKeys[k]);

                        theta[12 * K * l + 4 * (i * K + k) + 0] = tangents[0];
                        theta[12 * K * l + 4 * (i * K + k) + 1] = tangents[1];
                        theta[12 * K * l + 4 * (i * K + k) + 2] = tangents[2];
                        theta[12 * K * l + 4 * (i * K + k) + 3] = tangents[3];
                    }
                }
            }

            return theta;
        }

        public double[] GetTangents(Curve curve, int keyIndex)
        {
            double[] tangents = new double[4];
            Vector2 inTan = curve.keys[keyIndex].inTangent;
            Vector2 outTan = curve.keys[keyIndex].outTangent;

            tangents[0] = inTan.x;
            tangents[1] = inTan.y;
            tangents[2] = outTan.x;
            tangents[3] = outTan.y;
            return tangents;
        }

        public double[,] ColumnArrayToArray(double[] m)
        {
            int row = m.Length;
            double[,] theta = new double[row, 1];
            for (int i = 0; i < row; i++)
            {
                theta[i, 0] = m[i];
            }
            return theta;
        }

        public List<int> FindRequieredTangents(int firstFrame, int lastFrame, Curve curve)
        {
            List<int> keys = new List<int>();
            curve.GetKeyIndex(firstFrame, out int firstKeyIndex);
            curve.GetKeyIndex(lastFrame, out int lastKeyIndex);
            lastKeyIndex++;

            for (int i = firstKeyIndex; i <= lastKeyIndex; i++)
            {
                keys.Add(i);
            }

            return keys;
        }

        public double[,] ds_dtheta(List<AnimationSet> hierarchy, int frame, int p, int nbKeys, int K, List<int> requiredKeys)
        {
            double[,] Js = new double[7, p];
            float dtheta = Mathf.Pow(10, -2);

            GetAnimationData(hierarchy[hierarchy.Count - 1], frame, out Vector3 position, out Vector3 rotation);

            for (int l = 0; l < nbKeys; l++)
            {
                AnimationSet anim = hierarchy[l];
                // Rotation Curves
                for (int i = 3; i < 6; i++)
                {
                    AnimatableProperty property = (AnimatableProperty)i;
                    Curve curve = anim.GetCurve(property);

                    for (int k = 0; k < K; k++)
                    {
                        Vector2 inTangent = curve.keys[requiredKeys[k]].inTangent;
                        Vector2 outTangent = curve.keys[requiredKeys[k]].outTangent;

                        for (int m = 0; m < 4; m++)
                        {
                            int col = 12 * K * l + 4 * (i * K + k) + m;
                            //GetAnimationData(anim, frame, out Vector3 childPos, out Vector3 childRot);
                            Vector2 modInTangent = inTangent;
                            Vector2 modOutTangent = outTangent;
                            switch (m)
                            {
                                case 0: modInTangent.x += dtheta; break;
                                case 1: modInTangent.y += dtheta; break;
                                case 2: modOutTangent.x += dtheta; break;
                                case 3: modOutTangent.y += dtheta; break;
                            }
                            curve.SetTangents(requiredKeys[k], modInTangent, modOutTangent);
                            Maths.DecomposeMatrix(GetFrameMatrix(hierarchy.GetRange(0, l + 1), frame), out Vector3 plusPosition, out Quaternion plusRotation, out Vector3 plusScale);

                            modInTangent = inTangent;
                            modOutTangent = outTangent;
                            switch (m)
                            {
                                case 0: modInTangent.x -= dtheta; break;
                                case 1: modInTangent.y -= dtheta; break;
                                case 2: modOutTangent.x -= dtheta; break;
                                case 3: modOutTangent.y -= dtheta; break;
                            }
                            curve.SetTangents(requiredKeys[k], modInTangent, modOutTangent);
                            Maths.DecomposeMatrix(GetFrameMatrix(hierarchy.GetRange(0, l + 1), frame), out Vector3 minusPosition, out Quaternion minusRotation, out Vector3 minusScale);
                            Js[0, col] = (double)(plusPosition.x - minusPosition.x) / (dtheta);
                            Js[1, col] = (double)(plusPosition.y - minusPosition.y) / (dtheta);
                            Js[2, col] = (double)(plusPosition.z - minusPosition.z) / (dtheta);
                            Js[3, col] = (double)(plusRotation.x - minusRotation.x) / (dtheta);
                            Js[4, col] = (double)(plusRotation.y - minusRotation.y) / (dtheta);
                            Js[5, col] = (double)(plusRotation.z - minusRotation.z) / (dtheta);
                            Js[6, col] = (double)(plusRotation.w - minusRotation.w) / (dtheta);
                        }
                    }
                }

            }

            //Root position curves
            AnimationSet rootAnim = hierarchy[0];
            for (int i = 0; i < 3; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = rootAnim.GetCurve(property);
                for (int k = 0; k < K; k++)
                {
                    Vector2 inTangent = curve.keys[requiredKeys[k]].inTangent;
                    Vector2 outTangent = curve.keys[requiredKeys[k]].outTangent;
                    for (int m = 0; m < 4; m++)
                    {
                        int col = 12 * K * nbKeys + 4 * ((i - 3) * K + k) + m;
                        Vector2 modInTangent = inTangent;
                        Vector2 modOutTangent = outTangent;
                        switch (m)
                        {
                            case 0: modInTangent.x += dtheta; break;
                            case 1: modInTangent.y += dtheta; break;
                            case 2: modOutTangent.x += dtheta; break;
                            case 3: modOutTangent.y += dtheta; break;
                        }
                        curve.SetTangents(requiredKeys[k], modInTangent, modOutTangent);
                        Maths.DecomposeMatrix(GetFrameMatrix(hierarchy.GetRange(0, 1), frame), out Vector3 plusPosition, out Quaternion plusRotation, out Vector3 plusScale);
                        modInTangent = inTangent;
                        modOutTangent = outTangent;
                        switch (m)
                        {
                            case 0: modInTangent.x -= dtheta; break;
                            case 1: modInTangent.y -= dtheta; break;
                            case 2: modOutTangent.x -= dtheta; break;
                            case 3: modOutTangent.y -= dtheta; break;
                        }
                        curve.SetTangents(requiredKeys[k], modInTangent, modOutTangent);
                        Maths.DecomposeMatrix(GetFrameMatrix(hierarchy.GetRange(0, 1), frame), out Vector3 minusPosition, out Quaternion minusRotation, out Vector3 minusScale);

                        Js[0, col] = (double)(plusPosition.x - minusPosition.x) / (dtheta);
                        Js[1, col] = (double)(plusPosition.y - minusPosition.y) / (dtheta);
                        Js[2, col] = (double)(plusPosition.z - minusPosition.z) / (dtheta);
                        Js[3, col] = (double)(plusRotation.x - minusRotation.x) / (dtheta);
                        Js[4, col] = (double)(plusRotation.y - minusRotation.y) / (dtheta);
                        Js[5, col] = (double)(plusRotation.z - minusRotation.z) / (dtheta);
                        Js[6, col] = (double)(plusRotation.w - minusRotation.w) / (dtheta);

                    }
                }
            }

            return Js;
        }

        public State GetCurrentState(AnimationSet anim, int frame)
        {
            GetAnimationData(anim, frame, out Vector3 position, out Vector3 rotation);

            return new State() { position = position, rotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z), frame = frame };
        }

        private static void GetAnimationData(AnimationSet anim, int frame, out Vector3 position, out Vector3 rotation)
        {
            anim.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx);
            anim.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy);
            anim.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz);

            anim.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx);
            anim.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty);
            anim.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz);

            position = new Vector3(posx, posy, posz);
            rotation = new Vector3(rotx, roty, rotz);
        }

        double[] UpperBoundConstraints(double[] theta, double[] u, double[] v, int nbControlPoints, int p, int K, List<int> requiredKeyframeIndices)
        {
            double[] upperBound = new double[p];
            for (int l = 0; l < nbControlPoints; l++)
            {
                AnimationSet anim = objectHierarchy[l];
                for (int i = 3; i < 6; i++)
                {
                    AnimatableProperty property = (AnimatableProperty)i;
                    Curve curve = anim.GetCurve(property);
                    for (int k = 0; k < K; k++)
                    {

                        if (requiredKeyframeIndices[k] > 0 && requiredKeyframeIndices[k] < curve.keys.Count - 1)
                        {
                            int tkp1 = curve.keys[requiredKeyframeIndices[k] + 1].frame;
                            int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                            int tkm1 = curve.keys[requiredKeyframeIndices[k] - 1].frame;

                            upperBound[12 * K * l + 4 * (i * K + k) + 0] = tk - tkm1 - theta[12 * K * l + 4 * (i * K + k) + 0];
                            upperBound[12 * K * l + 4 * (i * K + k) + 1] = -phi(u[i], curve, requiredKeyframeIndices[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                            upperBound[12 * K * l + 4 * (i * K + k) + 2] = tkp1 - tk - theta[12 * K * l + 4 * (i * K + k) + 2];
                            upperBound[12 * K * l + 4 * (i * K + k) + 3] = psi(v[i], curve, requiredKeyframeIndices[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                        }

                        else if (requiredKeyframeIndices[k] == 0)
                        {
                            int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                            int tkp1 = curve.keys[requiredKeyframeIndices[k] + 1].frame;

                            upperBound[12 * K * l + 4 * (i * K + k) + 0] = tkp1 - tk - theta[12 * K * l + 4 * (i * K + k) + 0];
                            upperBound[12 * K * l + 4 * (i * K + k) + 1] = -phi(u[i], curve, requiredKeyframeIndices[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                            upperBound[12 * K * l + 4 * (i * K + k) + 2] = tkp1 - tk - theta[12 * K * l + 4 * (i * K + k) + 2];
                            upperBound[12 * K * l + 4 * (i * K + k) + 3] = psi(v[i], curve, requiredKeyframeIndices[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                        }

                        else
                        {
                            int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                            int tkm1 = curve.keys[requiredKeyframeIndices[k] - 1].frame;

                            upperBound[12 * K * l + 4 * (i * K + k) + 0] = tk - tkm1 - theta[12 * K * l + 4 * (i * K + k) + 0];
                            upperBound[12 * K * l + 4 * (i * K + k) + 1] = -phi(u[i], curve, requiredKeyframeIndices[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                            upperBound[12 * K * l + 4 * (i * K + k) + 2] = tk - tkm1 - theta[12 * K * l + 4 * (i * K + k) + 2];
                            upperBound[12 * K * l + 4 * (i * K + k) + 3] = psi(v[i], curve, requiredKeyframeIndices[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                        }

                    }
                }
            }

            AnimationSet rootAnim = objectHierarchy[0];
            for (int i = 0; i < 3; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = rootAnim.GetCurve(property);
                for (int k = 0; k < K; k++)
                {

                    if (requiredKeyframeIndices[k] > 0 && requiredKeyframeIndices[k] < curve.keys.Count - 1)
                    {
                        int tkp1 = curve.keys[requiredKeyframeIndices[k] + 1].frame;
                        int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                        int tkm1 = curve.keys[requiredKeyframeIndices[k] - 1].frame;

                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0] = tk - tkm1 - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1] = -phi(u[i], curve, requiredKeyframeIndices[k], false) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2] = tkp1 - tk - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3] = psi(v[i], curve, requiredKeyframeIndices[k], true) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3];
                    }

                    else if (requiredKeyframeIndices[k] == 0)
                    {
                        int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                        int tkp1 = curve.keys[requiredKeyframeIndices[k] + 1].frame;

                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0] = tkp1 - tk - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1] = -phi(u[i], curve, requiredKeyframeIndices[k], false) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2] = tkp1 - tk - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3] = psi(v[i], curve, requiredKeyframeIndices[k], true) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3];
                    }

                    else
                    {
                        int tk = curve.keys[requiredKeyframeIndices[k]].frame;
                        int tkm1 = curve.keys[requiredKeyframeIndices[k] - 1].frame;

                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0] = tk - tkm1 - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1] = -phi(u[i], curve, requiredKeyframeIndices[k], false) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2] = tk - tkm1 - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2];
                        upperBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3] = psi(v[i], curve, requiredKeyframeIndices[k], true) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3];
                    }
                }
            }
            return upperBound;
        }

        double[] LowerBoundConstraints(double[] theta, double[] u, double[] v, int nbControlPoints, int p, int K, List<int> requiredKeyframeIndices)
        {
            double[] lowerBound = new double[p];
            for (int l = 0; l < nbControlPoints; l++)
            {
                AnimationSet anim = objectHierarchy[l];
                for (int i = 3; i < 6; i++)
                {
                    AnimatableProperty property = (AnimatableProperty)i;
                    for (int k = 0; k < K; k++)
                    {
                        lowerBound[12 * K * l + 4 * (i * K + k) + 0] = -theta[12 * K * l + 4 * (i * K + k) + 0];
                        lowerBound[12 * K * l + 4 * (i * K + k) + 1] = -psi(v[i], anim.GetCurve(property), requiredKeyframeIndices[k], false) - theta[12 * K * l + 4 * (i * K + k) + 1];
                        lowerBound[12 * K * l + 4 * (i * K + k) + 2] = -theta[12 * K * l + 4 * (i * K + k) + 2];
                        lowerBound[12 * K * l + 4 * (i * K + k) + 3] = phi(u[i], anim.GetCurve(property), requiredKeyframeIndices[k], true) - theta[12 * K * l + 4 * (i * K + k) + 3];
                    }
                }
            }
            AnimationSet rootAnim = objectHierarchy[0];
            for (int i = 0; i < 3; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                for (int k = 0; k < K; k++)
                {
                    lowerBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0] = -theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 0];
                    lowerBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1] = -psi(v[i], rootAnim.GetCurve(property), requiredKeyframeIndices[k], false) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 1];
                    lowerBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2] = -theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 2];
                    lowerBound[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3] = phi(u[i], rootAnim.GetCurve(property), requiredKeyframeIndices[k], true) - theta[12 * K * nbControlPoints + 4 * ((i - 3) * K + k) + 3];
                }
            }
            return lowerBound;
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

        double[,] drho_dtheta(Dictionary<int, int[]> indexTable, int nbConstraints, int p, int nbKeys, int K, List<int> requiredKeys)
        {
            int size = indexTable[nbConstraints - 1][1] + 1;
            double[,] Jrho = new double[size, p];

            for (int pin = 0; pin < nbConstraints; pin++)
            {
                int child_index = objectConstraints.gameObjectIndices[pin];
                int pointer = 0;
                for (int frame = objectConstraints.startFrames[pin]; frame <= objectConstraints.endFrames[pin]; frame++)
                {
                    double[,] Js = ds_dtheta(objectHierarchy, frame, p, nbKeys, K, requiredKeys);
                    int i = objectConstraints.properties[pin];
                    for (int j = 0; j < Js.GetUpperBound(1) + 1; j++)
                    {
                        Jrho[indexTable[pin][0] + pointer, j] = Js[i, j];
                    }
                    pointer += 1;
                }
            }

            return Jrho;

        }

        (double[], double[]) RhoAndRhoPrime(Dictionary<int, int[]> indexTable, int nbConstraints)
        {
            int size = indexTable[nbConstraints - 1][1] + 1;
            double[] rho = new double[size];
            double[] rho_prime = new double[size];

            int pointer = 0;
            for (int i = 0; i < nbConstraints; i++)
            {
                int start = objectConstraints.startFrames[i];
                int end = objectConstraints.endFrames[i];
                AnimationSet child = objectHierarchy[objectConstraints.gameObjectIndices[i]];

                for (int frame = start; frame <= end; frame++)
                {
                    Matrix4x4 matrix = GetFrameMatrix(objectHierarchy.GetRange(0, objectConstraints.gameObjectIndices[i] - 1), frame);
                    Maths.DecomposeMatrix(matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);

                    if (objectConstraints.properties[i] == 0)
                    {
                        rho[pointer] = (double)position.x;
                    }
                    if (objectConstraints.properties[i] == 1)
                    {
                        rho[pointer] = (double)position.y;
                    }
                    if (objectConstraints.properties[i] == 2)
                    {
                        rho[pointer] = (double)position.z;
                    }
                    if (objectConstraints.properties[i] == 3)
                    {
                        rho[pointer] = (double)rotation.x;
                    }
                    if (objectConstraints.properties[i] == 4)
                    {
                        rho[pointer] = (double)rotation.y;
                    }
                    if (objectConstraints.properties[i] == 5)
                    {
                        rho[pointer] = (double)rotation.z;
                    }
                    if (objectConstraints.properties[i] == 6)
                    {
                        rho[pointer] = (double)rotation.w;
                    }

                    rho_prime[pointer] = (double)objectConstraints.values[i];

                    pointer += 1;
                }

            }

            return (rho, rho_prime);

        }

        Dictionary<int, int[]> FindConstraintsInRho(int nbConstraints, Constraint constraints)
        {
            Dictionary<int, int[]> Table = new Dictionary<int, int[]>();
            int pointer = 0;
            for (int i = 0; i < nbConstraints; i++)
            {
                int start = constraints.startFrames[i];
                int end = constraints.endFrames[i];
                Table.Add(i, new int[2] { pointer, pointer + end - start });
                pointer += end - start + 1;
            }
            return Table;
        }

        double phi(double u, Curve curve, int k, bool plus)
        {
            if (plus)
            {

                if (k < curve.keys.Count - 1)
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

        double psi(double v, Curve curve, int k, bool plus)
        {
            if (plus)
            {

                if (k < curve.keys.Count - 1)
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

        private Matrix4x4 GetFrameMatrix(List<AnimationSet> animations, int frame)
        {
            Matrix4x4 trsMatrix = animations[0].transform.parent.localToWorldMatrix;
            foreach (AnimationSet anim in animations) trsMatrix = trsMatrix * GetBoneMatrix(anim, frame);
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
                    position = new Vector3(sx, sy, sz);
                }
            }
            return Matrix4x4.TRS(position, rotation, scale);
        }

        #region MatrixFunction
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

        void Multiply(double alpha, ref double[,] m)
        {
            for (int i = 0; i < m.GetUpperBound(0) + 1; i++)
            {
                for (int j = 0; j < m.GetUpperBound(1) + 1; j++)
                {
                    m[i, j] = alpha * m[i, j];
                }
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

        #endregion
    }

}