using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AnimationTrigger : MonoBehaviour
    {
        [SerializeField] private AnimationTool animator;
        // Start is called before the first frame update


        private List<GameObject> hoveredCurves = new List<GameObject>();
        private bool isGrip;

        public void OnTriggerEnter(Collider other)
        {
            if (other.tag != "Curve" || hoveredCurves.Contains(other.gameObject)) return;

            hoveredCurves.Add(other.gameObject);
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.tag != "Curve" || !hoveredCurves.Contains(other.gameObject) || isGrip) return;
            hoveredCurves.Remove(other.gameObject);
            if (hoveredCurves.Count == 0) animator.HideGhost();
        }

        public void Update()
        {
            if (hoveredCurves.Count > 0)
            {
                if (!isGrip) animator.DrawCurveGhost(hoveredCurves[0], GetCollisionPoint(hoveredCurves[0], transform.position));
                else animator.DrawCurveGhost();
            }
            bool triggerState = VRInput.GetValue(VRInput.primaryController, CommonUsages.gripButton);
            if (triggerState && hoveredCurves.Count > 0)
            {
                animator.DragCurve(hoveredCurves[0], transform.position, transform.rotation);
                isGrip = true;  
            }
            else if (isGrip)
            {
                animator.ReleaseCurve(transform.position, transform.eulerAngles);
                hoveredCurves.Clear();
                animator.HideGhost();
                isGrip = false;
            }

        }

        public Vector3 GetCollisionPoint(GameObject gameObject, Vector3 position)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            return collider.ClosestPoint(position);
        }
    }

}