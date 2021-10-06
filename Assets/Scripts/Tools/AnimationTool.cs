using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{

    public class AnimationTool : ToolBase
    {
        [SerializeField] private NavigationOptions navigation;

        public Anim3DCurveManager CurveManager;
        public Material GhostMaterial;
        private GameObject ghost;
        private float deadzone = 0.3f;

        private Matrix4x4 initialMouthMatrix;
        private Matrix4x4 initialParentMatrix;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;
        private float scaleIndice;

        private Transform AddKeyModeButton;
        private Transform ZoneModeButton;
        private Transform ZoneSlider;

        public enum EditMode { AddKeyframe, Zone }
        private EditMode currentMode;

        private int zoneSize;

        private LineRenderer lastLine;
        private Texture2D lastTexture;

        public Color DefaultColor;
        public Color ZoneColor;

        public EditMode CurrentMode
        {
            set
            {
                if (value == currentMode) return;
                GetModeButton(currentMode).Selected = false;
                currentMode = value;
                GetModeButton(currentMode).Selected = true;
                Debug.Log(currentMode);
            }
            get { return currentMode; }
        }

        public struct DraggedCurveData
        {
            public GameObject target;
            public AnimationSet Animation;
            public int Frame;
        }

        private DraggedCurveData dragData = new DraggedCurveData() { Frame = -1 };

        public void SetAddKeyMode()
        {
            CurrentMode = EditMode.AddKeyframe;
        }
        public void SetZoneMode()
        {
            CurrentMode = EditMode.Zone;
        }

        public void SetZoneSize(float value)
        {
            zoneSize = Mathf.RoundToInt(value);
            ZoneSlider.GetComponent<UISlider>().Value = zoneSize;

        }

        protected override void Awake()
        {
            base.Awake();

            AddKeyModeButton = panel.Find("AddKey");
            ZoneModeButton = panel.Find("Zone");
            ZoneSlider = panel.Find("ZoneSize");

            zoneSize = Mathf.RoundToInt(ZoneSlider.GetComponent<UISlider>().Value);
            CurrentMode = EditMode.AddKeyframe;
        }

        private UIButton GetModeButton(EditMode mode)
        {
            switch (mode)
            {
                case EditMode.AddKeyframe: return AddKeyModeButton.GetComponent<UIButton>();
                case EditMode.Zone: return ZoneModeButton.GetComponent<UIButton>();
                default: return null;
            }
        }

        protected override void DoUpdate()
        {
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 val = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if (val != Vector2.zero)
                {
                    float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000f;
                    if (dragData.Frame == -1)
                    {
                        float selectorRadius = mouthpiece.localScale.x;
                        if (val.y > deadzone) selectorRadius *= scaleFactor;
                        if (val.y < deadzone) selectorRadius /= scaleFactor;
                        selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                        mouthpiece.localScale = Vector3.one * selectorRadius;
                    }
                    else
                    {
                        if (val.y > deadzone) scaleIndice *= scaleFactor;
                        if (val.y < deadzone) scaleIndice /= scaleFactor;
                        scaleIndice = Mathf.Clamp(scaleIndice, 0.001f, 100f);
                    }
                }
            }
        }

        public void DrawCurveGhost(GameObject curveObject, Vector3 point)
        {
            LineRenderer line = curveObject.GetComponent<LineRenderer>();
            int frame = GetFrameFromPoint(line, point);
            GameObject gobject = CurveManager.GetObjectFromCurve(curveObject);
            DrawCurveGhost(gobject, frame);
            if (currentMode == EditMode.Zone) DrawZone(line, frame);
        }

        public void DrawCurveGhost(GameObject gobject, int frame)
        {
            if (gobject == null) return;
            if (null == ghost) CreateGhost();
            ShowGhost(true);
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
            if (currentMode == EditMode.Zone) DrawZoneDrag();
        }

        public void DrawZone(LineRenderer line, int frame)
        {
            lastTexture = (Texture2D)line.material.mainTexture;
            if (lastTexture == null)
            {
                lastTexture = new Texture2D(line.positionCount, 1, TextureFormat.RGBA32, false);
                line.material.mainTexture = lastTexture;
            }

            ApplyTexture(frame);
            lastLine = line;
        }
        public void DrawZoneDrag()
        {
            if (CurveManager.TryGetLine(dragData.target, out LineRenderer line))
            {
                line.material.mainTexture = lastTexture;
            }
        }

        private void ApplyTexture(int frame)
        {
            NativeArray<Color32> colors = lastTexture.GetRawTextureData<Color32>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (i < (frame - zoneSize) || i > frame + zoneSize)
                {
                    colors[i] = DefaultColor;
                }
                else
                {
                    colors[i] = ZoneColor;
                }
            }
            lastTexture.Apply();
        }

        public void ResetColor()
        {
            if (null == lastLine) return;
            lastLine.material.mainTexture = null;
        }

        public void ReleaseCurve(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = ghost.transform.parent.worldToLocalMatrix *
                transformation * initialParentMatrix *
                Matrix4x4.TRS(initialPosition, initialRotation, initialScale);

            Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion qrotation, out Vector3 scale);
            Vector3 rotation = qrotation.eulerAngles;
            scale *= scaleIndice;
            GlobalState.Animation.SetObjectAnimations(dragData.target, dragData.Animation);
            CommandGroup group = new CommandGroup("Add Keyframe");
            if (currentMode == EditMode.AddKeyframe) new CommandAddKeyframes(dragData.target, dragData.Frame, position, rotation, scale).Submit();
            if (currentMode == EditMode.Zone) new CommandAddKeyframes(dragData.target, dragData.Frame, zoneSize, position, rotation, scale).Submit();
            group.Submit();

            dragData.Frame = -1;
        }

        internal void DragCurve(Transform mouthpiece)
        {
            int frame = dragData.Frame;
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = ghost.transform.parent.worldToLocalMatrix *
                transformation * initialParentMatrix *
                Matrix4x4.TRS(initialPosition, initialRotation, initialScale);

            Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion qrotation, out Vector3 scale);
            Vector3 rotation = qrotation.eulerAngles;
            scale *= scaleIndice;

            Interpolation interpolation = GlobalState.Settings.interpolation;
            AnimationKey posX = new AnimationKey(frame, position.x, interpolation);
            AnimationKey posY = new AnimationKey(frame, position.y, interpolation);
            AnimationKey posZ = new AnimationKey(frame, position.z, interpolation);
            AnimationKey rotX = new AnimationKey(frame, rotation.x, interpolation);
            AnimationKey rotY = new AnimationKey(frame, rotation.y, interpolation);
            AnimationKey rotZ = new AnimationKey(frame, rotation.z, interpolation);
            AnimationKey scalex = new AnimationKey(frame, scale.z, interpolation);
            AnimationKey scaley = new AnimationKey(frame, scale.z, interpolation);
            AnimationKey scalez = new AnimationKey(frame, scale.z, interpolation);

            if (currentMode == EditMode.AddKeyframe)
            {
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionX, posX);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionY, posY);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionZ, posZ);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationX, rotX);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationY, rotY);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationZ, rotZ);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.ScaleX, scalex);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.ScaleY, scaley);
                GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.ScaleZ, scalez);
            }
            if (currentMode == EditMode.Zone)
            {
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.PositionX, posX, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.PositionY, posY, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.PositionZ, posZ, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.RotationX, rotX, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.RotationY, rotY, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.RotationZ, rotZ, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.ScaleX, scalex, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.ScaleY, scaley, zoneSize);
                GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.ScaleZ, scalez, zoneSize);
            }

        }

        public void StartDrag(GameObject gameObject, Transform mouthpiece)
        {
            LineRenderer line = gameObject.GetComponent<LineRenderer>();
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            int frame = GetFrameFromPoint(line, mouthpiece.position);
            AnimationSet previousSet = GlobalState.Animation.GetObjectAnimation(target);
            dragData = new DraggedCurveData() { Animation = new AnimationSet(previousSet), target = target, Frame = frame };

            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            initialParentMatrix = ghost.transform.parent.localToWorldMatrix;
            initialPosition = ghost.transform.localPosition;
            initialRotation = ghost.transform.localRotation;
            initialScale = ghost.transform.localScale;
            scaleIndice = 1f;
        }

        internal void ShowGhost(bool state)
        {
            ghost.SetActive(state);
            foreach (Transform child in mouthpiece)
            {
                child.gameObject.SetActive(!state);
            }
            if (!state) ResetColor();
        }

        private int GetFrameFromPoint(LineRenderer line, Vector3 point)
        {
            Vector3 correctedPoint = line.transform.InverseTransformPoint(point);
            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);
            int closestPoint = 0;
            float closestDistance = Vector3.Distance(positions[0], correctedPoint);
            for (int i = 1; i < line.positionCount; i++)
            {
                float dist = Vector3.Distance(positions[i], correctedPoint);
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