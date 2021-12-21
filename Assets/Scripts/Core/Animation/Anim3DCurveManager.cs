/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Display motion trails of animated objects.
    /// </summary>
    public class Anim3DCurveManager : MonoBehaviour
    {
        private bool displaySelectedCurves = true;
        private readonly Dictionary<GameObject, GameObject> curves = new Dictionary<GameObject, GameObject>();
        public Transform curvesParent;
        public GameObject curvePrefab;

        private readonly float lineWidth = 0.001f;

        private bool isAnimTool = false;

        void Start()
        {
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            GlobalState.Animation.onAddAnimation.AddListener(OnAnimationAdded);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
            ToolsUIManager.Instance.OnToolChangedEvent += OnToolChanged;
        }

        void Update()
        {
            if (displaySelectedCurves != GlobalState.Settings.Display3DCurves)
            {
                displaySelectedCurves = GlobalState.Settings.Display3DCurves;
                if (displaySelectedCurves)
                    UpdateFromSelection();
                else
                    ClearCurves();
            }

            UpdateCurvesWidth();
        }

        void UpdateCurvesWidth()
        {
            foreach (GameObject curve in curves.Values)
            {
                LineRenderer line = curve.GetComponent<LineRenderer>();
                line.startWidth = lineWidth / GlobalState.WorldScale;
                line.endWidth = line.startWidth;
            }
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            if (GlobalState.Settings.Display3DCurves)
                UpdateFromSelection();
        }

        void UpdateFromSelection()
        {
            ClearCurves();
            foreach (GameObject gObject in Selection.SelectedObjects)
            {
                AddCurve(gObject);
            }
        }

        void OnCurveChanged(GameObject gObject, AnimatableProperty property)
        {
            HumanGoalController[] controllers = gObject.GetComponentsInChildren<HumanGoalController>();
            if (controllers.Length > 0)
            {
                UpdateHumanCurve(controllers);
                return;
            }
            if (property != AnimatableProperty.PositionX && property != AnimatableProperty.PositionY && property != AnimatableProperty.PositionZ)
                return;

            if (!Selection.IsSelected(gObject))
                return;

            UpdateCurve(gObject);
        }


        void OnAnimationAdded(GameObject gObject)
        {
            if (!Selection.IsSelected(gObject))
                return;
            UpdateCurve(gObject);
        }

        void OnAnimationRemoved(GameObject gObject)
        {
            if (gObject.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
            {
                RecursiveDeleteCurve(gObject.transform);
            }
            else
            {
                DeleteCurve(gObject);
            }
        }

        void OnToolChanged(object sender, ToolChangedArgs args)
        {
            bool switchToAnim = args.toolName == "Animation";
            if (switchToAnim && !isAnimTool)
            {
                UpdateFromSelection();
            }
            isAnimTool = switchToAnim;
        }

        void ClearCurves()
        {
            foreach (GameObject curve in curves.Values)
                Destroy(curve);
            curves.Clear();
        }

        void DeleteCurve(GameObject gObject)
        {
            if (curves.ContainsKey(gObject))
            {
                Destroy(curves[gObject]);
                curves.Remove(gObject);
            }
        }

        void RecursiveDeleteCurve(Transform target)
        {
            DeleteCurve(target.gameObject);
            foreach (Transform child in target)
            {
                RecursiveDeleteCurve(child);
            }
        }

        void UpdateCurve(GameObject gObject)
        {
            DeleteCurve(gObject);
            AddCurve(gObject);
        }

        private void UpdateHumanCurve(HumanGoalController[] controllers)
        {
            for (int i = 0; i < controllers.Length; i++)
            {
                DeleteCurve(controllers[i].gameObject);
                GetAHumanAnimationCurve(controllers[i]);
            }
        }


        void AddCurve(GameObject gObject)
        {
            if (gObject.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
            {
                AddHumanCurve(gObject, controller);
            }
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
            GameObject curve = Instantiate(curvePrefab, curvesParent);

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

            curves.Add(gObject, curve);
        }

        private void AddHumanCurve(GameObject gObject, SkinMeshController controller)
        {
            if (!controller.RootObject.TryGetComponent<HumanGoalController>(out HumanGoalController goalController))
            {
                return;
            }
            if (ToolsManager.CurrentToolName() == "Animation")
            {
                HumanGoalController[] controllers = goalController.GetComponentsInChildren<HumanGoalController>();
                foreach (HumanGoalController ctrl in controllers)
                {
                    GetAHumanAnimationCurve(ctrl);
                }
            }
            else
            {
                GetAHumanAnimationCurve(goalController);
            }
        }

        private void GetAHumanAnimationCurve(HumanGoalController goalController)
        {
            if (!goalController.ShowCurve) return;
            AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(goalController.gameObject);
            if (null == rootAnimation) return;
            Curve positionX = rootAnimation.GetCurve(AnimatableProperty.RotationX);
            if (positionX.keys.Count == 0) return;
            int frameStart = Mathf.Clamp(positionX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            int frameEnd = Mathf.Clamp(positionX.keys[positionX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

            List<Vector3> positions = new List<Vector3>();
            GameObject curve = Instantiate(curvePrefab, curvesParent);

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
            curves.Add(goalController.gameObject, curve);
        }

        public GameObject GetObjectFromCurve(GameObject curve)
        {
            foreach (KeyValuePair<GameObject, GameObject> pair in curves)
            {
                if (pair.Value == curve) return pair.Key;
            }
            return null;
        }

        public bool TryGetLine(GameObject gobject, out LineRenderer line)
        {
            if (!curves.TryGetValue(gobject, out GameObject value))
            {
                line = null;
                return false;
            }
            return (value.TryGetComponent<LineRenderer>(out line));
        }
    }
}
