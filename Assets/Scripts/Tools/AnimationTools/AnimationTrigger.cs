using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AnimationTrigger : MonoBehaviour
    {
        [SerializeField] private AnimationTool animator;


        private List<GameObject> hoveredCurves = new List<GameObject>();
        private List<HumanGoalController> hoveredGoals = new List<HumanGoalController>();
        private bool isGrip;
        private List<GameObject> dragedObject = new List<GameObject>();

        public void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Curve" && !hoveredCurves.Contains(other.gameObject)) hoveredCurves.Add(other.gameObject);
            if (other.tag == "Goal" && other.TryGetComponent<HumanGoalController>(out HumanGoalController controller) && !hoveredGoals.Contains(controller))
            {
                hoveredGoals.Add(controller);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (!isGrip && other.tag == "Curve" && hoveredCurves.Contains(other.gameObject))
            {
                hoveredCurves.Remove(other.gameObject);
                if (hoveredCurves.Count == 0) animator.ShowGhost(false);
            }
            if (!isGrip && other.tag == "Goal" && other.TryGetComponent<HumanGoalController>(out HumanGoalController controller) && hoveredGoals.Contains(controller))
            {
                hoveredGoals.Remove(controller);
            }
        }

        public void Update()
        {
            switch (animator.Mode)
            {
                case AnimationTool.EditMode.Curve:
                    CurveMode();
                    break;
                case AnimationTool.EditMode.Pose:
                    PoseMode();
                    break;
            }
        }

        public void PoseMode()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip,
                () =>
                {
                    if (hoveredGoals.Count > 0)
                    {
                        animator.StartPose(hoveredGoals[0], transform);
                        isGrip = true;
                    }
                    foreach (GameObject gobject in Selection.SelectedObjects)
                    {
                        if (!gobject.TryGetComponent(out SkinMeshController controller))
                        {
                            animator.StartDragObject(gobject, transform);
                            dragedObject.Add(gobject);
                        }
                    }
                },
                () =>
                {
                    if (isGrip)
                    {
                        animator.EndPose();
                        hoveredGoals.Clear();
                        isGrip = false;
                    }
                    if (dragedObject.Count > 0)
                    {
                        dragedObject.ForEach(x => animator.EndDragObject());
                        dragedObject.Clear();
                    }
                });
            if (isGrip) isGrip = animator.DragPose(transform);
            if (dragedObject.Count > 0)
            {
                dragedObject.ForEach(x => animator.DragObject(transform));
            }
            if (hoveredGoals.Count > 0 && hoveredGoals[0] == null) hoveredGoals.RemoveAt(0);

        }

        //private void ShowJoint(HumanGoalController controller)
        //{
        //    MeshFilter filter = controller.gameObject.AddComponent<MeshFilter>();
        //    filter.sharedMesh = gameObject.GetComponent<MeshFilter>().mesh;
        //    MeshRenderer renderer = controller.gameObject.AddComponent<MeshRenderer>();
        //    renderer.material = gameObject.GetComponent<MeshRenderer>().material;
        //}

        //private void HideJoint(HumanGoalController controller)
        //{
        //    if (controller.TryGetComponent<MeshFilter>(out MeshFilter filter))
        //    {
        //        Destroy(filter);
        //    }
        //    if (controller.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
        //    {
        //        Destroy(renderer);
        //    }
        //}

        public void CurveMode()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip,
                () =>
                {
                    if (hoveredCurves.Count > 0)
                    {
                        animator.StartDrag(hoveredCurves[0], transform);
                        isGrip = true;
                    }
                },
                () =>
                {
                    if (isGrip)
                    {
                        animator.ReleaseCurve();
                        hoveredCurves.Clear();
                        animator.ShowGhost(false);
                        isGrip = false;
                    }
                });
            if (isGrip) isGrip = animator.DragCurve(transform);

            if (hoveredCurves.Count > 0 && hoveredCurves[0] == null)
            {
                hoveredCurves.RemoveAt(0);
                animator.ShowGhost(false);
            }
            if (hoveredCurves.Count > 0)
            {
                if (!isGrip) animator.DrawCurveGhost(hoveredCurves[0], transform.position);
                else animator.DrawCurveGhost();
            }

        }

        public Vector3 GetCollisionPoint(GameObject gameObject, Vector3 position)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            return collider.ClosestPoint(position);
        }
    }

}