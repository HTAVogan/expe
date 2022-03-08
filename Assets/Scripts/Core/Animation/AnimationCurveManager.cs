using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class AnimationCurveManager : MonoBehaviour
    {

        private bool displaySelectedCurves = true;
        private readonly Dictionary<GameObject, GameObject> curves = new Dictionary<GameObject, GameObject>();
        public Transform curvesParent;
        public GameObject curvePrefab;

        private readonly float lineWidth = 0.001f;

        protected enum Mode { Classic, Animation, Selected };
        private Mode currentMode;

        private CurveManagerMode managerMode;

        void Start()
        {
            managerMode = new ClassicCurveMode(curvePrefab, curvesParent, lineWidth);
            currentMode = Mode.Classic;

            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            GlobalState.Animation.onAddAnimation.AddListener(OnAnimationAdded);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
            ToolsUIManager.Instance.OnToolChangedEvent += OnToolChanged;
        }

        private void OnCurveSelectionChanged(List<GameObject> gobjects)
        {
            managerMode.UpdateFromSelection(gobjects, curves);
        }

        private void OnCurveChanged(GameObject arg0, AnimatableProperty arg1)
        {
            throw new NotImplementedException();
        }

        private void OnAnimationRemoved(GameObject arg0)
        {
            throw new NotImplementedException();
        }

        private void OnAnimationAdded(GameObject arg0)
        {
            throw new NotImplementedException();
        }

        private void OnSelectionChanged(HashSet<GameObject> arg0, HashSet<GameObject> arg1)
        {
            throw new NotImplementedException();
        }

        void OnToolChanged(object sender, ToolChangedArgs args)
        {

        }

        #region ModeClass
        protected abstract class CurveManagerMode
        {
            protected GameObject curvePrefab;
            protected Transform curvesParent;
            protected float lineWidth;



            public virtual void UpdateFromSelection(List<GameObject> selection, Dictionary<GameObject, GameObject> curves)
            {
                ClearCurves(curves);
                selection.ForEach(x => AddCurve(x, curves));
            }

            public virtual void ClearCurves(Dictionary<GameObject, GameObject> curves)
            {
                foreach (GameObject curve in curves.Values) Destroy(curve);
                curves.Clear();
            }

            public virtual void AddCurve(GameObject gobject, Dictionary<GameObject, GameObject> curves)
            {

            }

            protected void AddObjectCurve(GameObject gObject, Dictionary<GameObject, GameObject> curves)
            {
                AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
                if (null == animationSet)
                {
                    return;
                }

                Curve positionX = animationSet.GetCurve(AnimatableProperty.PositionX);
                Curve positionY = animationSet.GetCurve(AnimatableProperty.PositionY);
                Curve positionZ = animationSet.GetCurve(AnimatableProperty.PositionZ);

                if (null == positionX || null == positionY || null == positionZ)
                    return;

                if (positionX.keys.Count == 0)
                    return;

                if (positionX.keys.Count != positionY.keys.Count || positionX.keys.Count != positionZ.keys.Count)
                    return;

                int frameStart = Mathf.Clamp(positionX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
                int frameEnd = Mathf.Clamp(positionX.keys[positionX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

                Transform curves3DTransform = GlobalState.Instance.world.Find("Curves3D");
                Matrix4x4 matrix = curves3DTransform.worldToLocalMatrix * gObject.transform.parent.localToWorldMatrix;

                List<Vector3> positions = new List<Vector3>();
                for (int i = frameStart; i <= frameEnd; i++)
                {
                    positionX.Evaluate(i, out float x);
                    positionY.Evaluate(i, out float y);
                    positionZ.Evaluate(i, out float z);
                    Vector3 position = new Vector3(x, y, z);
                    position = matrix.MultiplyPoint(position);

                    positions.Add(position);
                }

                int count = positions.Count;
                GameObject curve = curves.TryGetValue(gObject, out GameObject current) ? current : Instantiate(curvePrefab, curvesParent);

                LineRenderer line = curve.GetComponent<LineRenderer>();
                line.positionCount = count;
                for (int index = 0; index < count; index++)
                {
                    line.SetPosition(index, positions[index]);
                }
                line.startWidth = lineWidth / GlobalState.WorldScale;
                line.endWidth = line.startWidth;

                MeshCollider collider = curve.GetComponent<MeshCollider>();
                Mesh lineMesh = new Mesh();
                line.BakeMesh(lineMesh);
                collider.sharedMesh = lineMesh;

                curves[gObject] = curve;
            }

            protected void GetAHumanAnimationCurve(HumanGoalController goalController, Dictionary<GameObject, GameObject> curves)
            {
                if (!goalController.ShowCurve) return;
                AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(goalController.gameObject);
                if (null == rootAnimation) return;
                Curve positionX = rootAnimation.GetCurve(AnimatableProperty.RotationX);
                if (positionX.keys.Count == 0) return;
                int frameStart = Mathf.Clamp(positionX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
                int frameEnd = Mathf.Clamp(positionX.keys[positionX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

                List<Vector3> positions = new List<Vector3>();
                GameObject curve = curves.TryGetValue(goalController.gameObject, out GameObject current) ? current : Instantiate(curvePrefab, curvesParent);

                goalController.CheckAnimations();
                for (int i = frameStart; i <= frameEnd; i++)
                {
                    Vector3 position = curve.transform.InverseTransformDirection(goalController.FramePosition(i));
                    positions.Add(position);
                }
                LineRenderer line = curve.GetComponent<LineRenderer>();
                line.positionCount = positions.Count;
                line.SetPositions(positions.ToArray());

                line.startWidth = lineWidth / GlobalState.WorldScale;
                line.endWidth = line.startWidth;

                MeshCollider collider = curve.GetComponent<MeshCollider>();
                Mesh lineMesh = new Mesh();
                line.BakeMesh(lineMesh);
                collider.sharedMesh = lineMesh;
                curves[goalController.gameObject] = curve;
            }
        }

        protected class ClassicCurveMode : CurveManagerMode
        {
            public ClassicCurveMode(GameObject CurvePrefab, Transform CurvesParent, float curveWidth)
            {
                curvePrefab = CurvePrefab;
                curvesParent = CurvesParent;
                lineWidth = curveWidth;
            }

            public override void AddCurve(GameObject gobject, Dictionary<GameObject, GameObject> curves)
            {
                if (gobject.TryGetComponent(out SkinMeshController skinController) && skinController.RootObject.TryGetComponent(out HumanGoalController controller))
                {
                    GetAHumanAnimationCurve(controller, curves);
                }
                AddObjectCurve(gobject, curves);
            }
        }

        protected class AnimationCurveMode : CurveManagerMode
        {
            public AnimationCurveMode()
            {
            }

            public override void UpdateFromSelection(List<GameObject> selection, Dictionary<GameObject, GameObject> curves)
            {
                throw new NotImplementedException();
            }
        }

        protected class SelectedCurveMode : CurveManagerMode
        {
            public SelectedCurveMode()
            {
            }

            public override void UpdateFromSelection(List<GameObject> selection, Dictionary<GameObject, GameObject> curves)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }

}