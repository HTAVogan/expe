using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{

    public class PoseManipulation
    {

        private Transform oTransform;
        private List<Transform> fullHierarchy;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private int hierarchySize;
        private Matrix4x4 initialMouthMatrix;
        public Matrix4x4 InitialParentMatrix;
        public Matrix4x4 InitialParentMatrixWorldToLocal;
        private Matrix4x4 initialTransformMatrix;
        private Matrix4x4 InitialTRS;
        private Vector3 fromRotation;
        private Quaternion initialRotation;

        private AnimationTool.PoseEditMode poseMode;

        private struct State
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        private State currentState;
        private State desiredState;
        private Vector3 initialRootPosition;
        List<Quaternion> initialRotations;


        private int p;
        private double[] theta;
        private double[,] Q_opt;
        private double[] b_opt;
        private double[] lowerBound, upperBound;
        private double[] delta_theta_0;
        private double[] s;
        private double[] delta_theta;

        private GameObject debugCurrent;
        private GameObject debugTarget;

        public PoseManipulation(Transform objectTransform, List<Transform> objectHierarchy, Transform mouthpiece, AnimationTool.PoseEditMode mode)
        {
            poseMode = mode;
            oTransform = objectTransform;
            fullHierarchy = new List<Transform>(objectHierarchy);
            fullHierarchy.Add(objectTransform);
            hierarchySize = fullHierarchy.Count;
            InitialTRS = Matrix4x4.TRS(oTransform.localPosition, oTransform.localRotation, oTransform.localScale);

            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            InitialParentMatrix = oTransform.parent.localToWorldMatrix;
            InitialParentMatrixWorldToLocal = oTransform.parent.worldToLocalMatrix;
            InitialTRS = Matrix4x4.TRS(oTransform.localPosition, oTransform.localRotation, oTransform.localScale);
            initialTransformMatrix = oTransform.localToWorldMatrix;


            fromRotation = Quaternion.FromToRotation(Vector3.forward, oTransform.localPosition) * Vector3.forward;
            initialRotation = fullHierarchy[hierarchySize - 2].localRotation;



            //debugCurrent = GameObject.CreatePrimitive(PrimitiveType.Plane);
            //debugCurrent.transform.localScale = Vector3.one * 0.05f;
            //debugCurrent.GetComponent<MeshRenderer>().material.color = Color.red;

            //debugTarget = GameObject.CreatePrimitive(PrimitiveType.Plane);
            //debugTarget.transform.localScale = Vector3.one * 0.05f;

            //debugCurrent.transform.position = oTransform.position;
            //debugCurrent.transform.rotation = oTransform.rotation;
            //Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            //Matrix4x4 target = transformation * initialTransformMatrix;
            //Maths.DecomposeMatrix(target, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            //debugTarget.transform.position = position;
            //debugTarget.transform.rotation = rotation * Quaternion.Euler(180, 0, 0);

        }

        public void SetDestination(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            if (poseMode == AnimationTool.PoseEditMode.FK)
            {

                Matrix4x4 transformed = InitialParentMatrixWorldToLocal *
                    transformation * InitialParentMatrix *
                    InitialTRS;
                Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion rotation, out Vector3 scale);
                targetPosition = position;
                targetRotation = rotation;
            }
            else
            {
                Matrix4x4 target = transformation * initialTransformMatrix;
                Maths.DecomposeMatrix(target, out Vector3 position, out Quaternion rotation, out Vector3 scale);
                targetPosition = position;
                targetRotation = rotation * Quaternion.Euler(-180, 0, 0);
            }
        }

        public bool TrySolver()
        {
            if (poseMode == AnimationTool.PoseEditMode.FK)
            {
                if (hierarchySize > 2)
                {
                    Vector3 to = Quaternion.FromToRotation(Vector3.forward, targetPosition) * Vector3.forward;
                    fullHierarchy[hierarchySize - 2].localRotation = initialRotation * Quaternion.FromToRotation(fromRotation, to);
                }
                else
                {
                    oTransform.localPosition = targetPosition;
                }

                oTransform.localRotation = targetRotation;
                return true;
            }
            else
            {
                Setup();
                if (Compute())
                    Apply();

                return true;
            }
        }

        public bool Setup()
        {
            // hierarchy * rotation + root position
            p = 3 * hierarchySize + 3;
            theta = GetAllValues(p);


            currentState = new State()
            {
                position = oTransform.position,
                rotation = oTransform.rotation
            };
            //Debug.Log("current : " + currentState.position + " / " + currentState.rotation + " // " + oTransform.eulerAngles);
            desiredState = new State()
            {
                position = targetPosition,
                rotation = targetRotation
            };
            //Debug.Log("target : " + desiredState.position + " / " + desiredState.rotation);
            initialRootPosition = fullHierarchy[0].position;

            double[,] Js = ds_dtheta(p);

            double[,] DT_D = new double[p, p];
            //Root rotation
            for (int i = 0; i < 3; i++)
            {
                DT_D[i, i] = 1d;
            }
            //nonRoot rotation
            for (int i = 3; i < hierarchySize * 3; i++)
            {
                if (fullHierarchy[i / 3].TryGetComponent<HumanGoalController>(out HumanGoalController controller))
                {
                    DT_D[i, i] = controller.stiffness;
                }
                else
                {
                    DT_D[i, i] = 1d;
                }
            }
            //root position
            for (int i = 0; i < 3; i++)
            {
                int j = 3 * hierarchySize + i;
                DT_D[j, j] = 1d;
            }

            double[,] Delta_s_prime = new double[7, 1];
            for (int i = 0; i < 3; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            for (int i = 0; i < 4; i++)
            {
                Delta_s_prime[i + 3, 0] = desiredState.rotation[i] - currentState.rotation[i];
            }

            double wm = 100f;
            double wd = 1f;

            Q_opt = Add(Add(Multiply(2d * wm, Multiply(Transpose(Js), Js)), Multiply(2d * wd, DT_D)), Multiply((double)Mathf.Pow(10, -6), Identity(p)));

            double[,] B_opt = Multiply(-2d * wm, Multiply(Transpose(Js), Delta_s_prime));
            b_opt = ArrayToColumnArray(B_opt);

            lowerBound = InitializeUBound(p);
            upperBound = InitializeVBound(p);

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
            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(p, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);
            alglib.minqpsetscale(state_opt, s);

            //alglib.minqpsetalgoquickqp(state_opt, 0.0, 0.0, 0.0, 0, true);
            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);
            if (rep.terminationtype != 7) return true;
            else return false;
        }

        private bool Apply()
        {
            double[] new_theta = new double[p];
            for (int i = 0; i < 3; i++)
            {
                int j = 3 * hierarchySize + i;
                new_theta[j] = delta_theta[j] + theta[j];
            }
            for (int l = 0; l < hierarchySize; l++)
            {
                Transform currentTransform = fullHierarchy[l];
                int i = l * 3;
                currentTransform.localRotation *= Quaternion.Euler((float)delta_theta[i], (float)delta_theta[i + 1], (float)delta_theta[i + 2]);
            }
            Transform rootTransform = fullHierarchy[0];
            int k = hierarchySize * 3;
            rootTransform.localPosition = new Vector3((float)new_theta[k], (float)new_theta[k + 1], (float)new_theta[k + 2]);

            if (Vector3.Distance(currentState.position, desiredState.position) < Vector3.Distance(oTransform.position, desiredState.position))
            {
                rootTransform.position = initialRootPosition;
                for (int l = 0; l < hierarchySize; l++)
                {
                    fullHierarchy[l].rotation = initialRotations[l];
                }
            }


            return true;
        }

        private double[] GetAllValues(int p)
        {
            double[] theta = new double[p];
            initialRotations = new List<Quaternion>();
            for (int l = 0; l < hierarchySize; l++)
            {
                Transform currentTransform = fullHierarchy[l];
                theta[3 * l + 0] = Mathf.DeltaAngle(0, currentTransform.localEulerAngles.x);
                theta[3 * l + 1] = Mathf.DeltaAngle(0, currentTransform.localEulerAngles.y);
                theta[3 * l + 2] = Mathf.DeltaAngle(0, currentTransform.localEulerAngles.z);
                initialRotations.Add(currentTransform.rotation);
            }
            Transform root = fullHierarchy[0];
            theta[3 * hierarchySize + 0] = root.localPosition.x;
            theta[3 * hierarchySize + 1] = root.localPosition.y;
            theta[3 * hierarchySize + 2] = root.localPosition.z;
            return theta;
        }

        double[,] ds_dtheta(int p)
        {
            double[,] Js = new double[7, p];
            float dtheta = 1f;

            for (int l = 0; l < hierarchySize; l++)
            {
                Transform currentTransform = fullHierarchy[l];
                Quaternion currentRotation = currentTransform.localRotation;
                for (int i = 0; i < 3; i++)
                {
                    Vector3 rotation = Vector3.zero;
                    rotation[i] = 1;
                    currentTransform.localRotation *= Quaternion.Euler(rotation);

                    Vector3 plusPosition = oTransform.position;
                    Quaternion plusRotation = oTransform.rotation;

                    currentTransform.localRotation = currentRotation;

                    Vector3 minusPosition = oTransform.position;
                    Quaternion minusRotation = oTransform.rotation;

                    int col = 3 * l + i;

                    Js[0, col] = (plusPosition.x - minusPosition.x) / dtheta;
                    Js[1, col] = (plusPosition.y - minusPosition.y) / dtheta;
                    Js[2, col] = (plusPosition.z - minusPosition.z) / dtheta;
                    Js[3, col] = (plusRotation.x - minusRotation.x) / dtheta;
                    Js[4, col] = (plusRotation.y - minusRotation.y) / dtheta;
                    Js[5, col] = (plusRotation.z - minusRotation.z) / dtheta;
                    Js[6, col] = (plusRotation.w - minusRotation.w) / dtheta;
                }
            }
            Transform rootTransform = fullHierarchy[0];
            Vector3 rootPosition = rootTransform.localPosition;
            for (int i = 0; i < 3; i++)
            {
                rootPosition[i] += dtheta;
                rootTransform.localPosition = rootPosition;

                Vector3 plusPosition = oTransform.position;
                Quaternion plusRotation = oTransform.rotation;

                rootPosition[i] -= dtheta;
                rootTransform.localPosition = rootPosition;

                Vector3 minusPosition = oTransform.position;
                Quaternion minusRotation = oTransform.rotation;

                int col = 3 * hierarchySize + i;

                Js[0, col] = (plusPosition.x - minusPosition.x) / dtheta;
                Js[1, col] = (plusPosition.y - minusPosition.y) / dtheta;
                Js[2, col] = (plusPosition.z - minusPosition.z) / dtheta;
                Js[3, col] = (plusRotation.x - minusRotation.x) / dtheta;
                Js[4, col] = (plusRotation.y - minusRotation.y) / dtheta;
                Js[5, col] = (plusRotation.z - minusRotation.z) / dtheta;
                Js[6, col] = (plusRotation.w - minusRotation.w) / dtheta;
            }
            return Js;
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
            for (int i = 0; i < hierarchySize * 3; i++)
            {
                //u[i] = theta[i] < -90 ? 0 : -10;
                if (theta[i] < -90) u[i] = 0;
                else u[i] = -10;

                //Vector3 rotation = Vector3.zero;
                //rotation[i % 3] = -10;
                //Vector3 maxRotation = Quaternion.Euler(rotation) * new Vector3((float)theta[(i / 3) * 3], (float)theta[(i / 3) * 3 + 1], (float)theta[(i / 3) * 3 + 2]);
                //if (Mathf.Abs(maxRotation.x) > 90 || Mathf.Abs(maxRotation.y) > 90 || Mathf.Abs(maxRotation.z) > 90) u[i] = 0;
                //else u[i] = -5;
            }
            for (int j = hierarchySize * 3; j < hierarchySize * 3 + 3; j++)
            {
                u[j] = -10d;
            }
            return u;
        }
        double[] InitializeVBound(int n)
        {
            double[] v = new double[n];
            for (int i = 0; i < hierarchySize * 3; i++)
            {
                //v[i] = theta[i] > 90 ? 0 : 10;
                if (theta[i] > 90) v[i] = 0;
                else v[i] = 10;

                //Vector3 rotation = Vector3.zero;
                //rotation[i % 3] = 10;
                //Vector3 currentRotation = new Vector3((float)theta[(i / 3) * 3], (float)theta[(i / 3) * 3 + 1], (float)theta[(i / 3) * 3 + 2]);
                //Vector3 maxRotation = Quaternion.Euler(rotation) * currentRotation;
                //if (Mathf.Abs(maxRotation.x) > 90 || Mathf.Abs(maxRotation.y) > 90 || Mathf.Abs(maxRotation.z) > 90) v[i] = 0;
                //else v[i] = 5;
            }
            for (int j = hierarchySize * 3; j < hierarchySize * 3 + 3; j++)
            {
                v[j] = 10d;
            }
            return v;
        }
    }
}
