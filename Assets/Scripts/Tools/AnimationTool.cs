using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class AnimationTool : ToolBase
    {
        public Anim3DCurveManager CurveManager;
        public Material GhostMaterial;
        private GameObject ghost;

        public struct DraggedCurveData
        {
            public GameObject target;
            public AnimationSet Animation;
            public int Frame;
        }

        private DraggedCurveData dragData = new DraggedCurveData() { Frame = -1 };

        protected override void DoUpdate()
        {

        }

        public void Start()
        {
            Debug.Log("animation tool started");
        }

        public void DrawCurveGhost(GameObject target, Vector3 point)
        {
            LineRenderer line = target.GetComponent<LineRenderer>();
            Vector3 correctedPoint = line.transform.InverseTransformPoint(point);
            int frame = GetFrameFromPoint(line, correctedPoint);
            GameObject gobject = CurveManager.GetObjectFromCurve(target);
            DrawCurveGhost(gobject, frame);
        }

        public void DrawCurveGhost(GameObject gobject, int frame)
        {
            if (null == ghost) CreateGhost();
            ghost.SetActive(true);
            MeshFilter ghostFilter = ghost.GetComponent<MeshFilter>();
            if (gobject.TryGetComponent<MeshFilter>(out MeshFilter objectMesh))
            {
                ghostFilter.mesh = objectMesh.mesh;
            }
            else
            {
                Debug.Log("Implement default ghost mesh");
            }
            AnimationSet set = GlobalState.Animation.GetObjectAnimation(gobject);
            set.EvaluateTransform(frame, ghost.transform);
        }

        internal void DrawCurveGhost()
        {
            DrawCurveGhost(dragData.target, dragData.Frame);
        }

        internal void ReleaseCurve(Vector3 position, Vector3 rotation)
        {
            Debug.Log("release curve");
            position = dragData.target.transform.parent.InverseTransformPoint(position);
            GlobalState.Animation.SetObjectAnimations(dragData.target, dragData.Animation);
            CommandGroup group = new CommandGroup("Add Keyframe");
            new CommandAddKeyframes(dragData.target, dragData.Frame, position, rotation);
            group.Submit();
        }

        internal void DragCurve(GameObject gameObject, Vector3 position, Vector3 rotation)
        {
            if (dragData.Frame == -1)
            {
                Debug.Log("start drag curve");
                LineRenderer line = gameObject.GetComponent<LineRenderer>();
                GameObject target = CurveManager.GetObjectFromCurve(gameObject);
                int frame = GetFrameFromPoint(line, position);
                StartDrag(target, frame);
            }
            else
            {
                int frame = dragData.Frame;
                position = dragData.target.transform.parent.InverseTransformPoint(position);
                Debug.Log("drag curve");
                Interpolation interpolation = GlobalState.Settings.interpolation;
                AnimationKey posX = new AnimationKey(frame, position.x, interpolation);
                AnimationKey posY = new AnimationKey(frame, position.y, interpolation);
                AnimationKey posZ = new AnimationKey(frame, position.z, interpolation);
                AnimationKey rotX = new AnimationKey(frame, rotation.x, interpolation);
                AnimationKey rotY = new AnimationKey(frame, rotation.y, interpolation);
                AnimationKey rotZ = new AnimationKey(frame, rotation.z, interpolation);

                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionX, posX);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionY, posY);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionZ, posZ);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationX, rotX);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationY, rotY);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationZ, rotZ);
            }
        }

        private void StartDrag(GameObject target, int frame)
        {
            AnimationSet previousSet = GlobalState.Animation.GetObjectAnimation(target);
            dragData = new DraggedCurveData() { Animation = new AnimationSet(previousSet), target = target, Frame = frame };
        }

        internal void HideGhost()
        {
            ghost.SetActive(false);
        }

        private int GetFrameFromPoint(LineRenderer line, Vector3 point)
        {
            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);
            int closestPoint = 0;
            float closestDistance = Vector3.Distance(positions[0], point);
            for (int i = 1; i < line.positionCount; i++)
            {
                float dist = Vector3.Distance(positions[i], point);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPoint = i;
                }
            }
            return closestPoint + GlobalState.Animation.StartFrame;
        }

        private void CreateGhost()
        {
            ghost = new GameObject();
            ghost.name = "AnimationGhost";
            ghost.transform.parent = CurveManager.curvesParent;
            ghost.AddComponent<MeshRenderer>();
            ghost.AddComponent<MeshFilter>();

            ghost.GetComponent<MeshRenderer>().material = GhostMaterial;
        }
    }

}