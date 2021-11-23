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

        private float scaleIndice;

        private Transform AddKeyModeButton;
        private Transform ZoneModeButton;
        private Transform SegmentModeButton;
        private Transform TangentModeButton;
        private Transform ZoneSlider;
        private Transform CurveModeButton;
        private Transform PoseModeButton;
        private Transform FKModeButton;
        private Transform IKModeButton;

        private int zoneSize;

        private int startFrame;
        private int endFrame;

        private LineRenderer lastLine;
        private Texture2D lastTexture;

        public Color DefaultColor;
        public Color ZoneColor;

        private CurveManipulation curveManip;
        private PoseManipulation poseManip;

        public enum EditMode { Curve, Pose }
        private EditMode editMode = EditMode.Pose;
        public EditMode Mode
        {
            get { return editMode; }
            set
            {
                GetModeButton(editMode).Checked = false;
                editMode = value;
                GetModeButton(editMode).Checked = true;
            }
        }

        public enum CurveEditMode { AddKeyframe, Zone, Segment, Tangents }
        private CurveEditMode curveMode;
        public CurveEditMode CurveMode
        {
            set
            {
                GetCurveModeButton(curveMode).Checked = false;
                curveMode = value;
                GetCurveModeButton(curveMode).Checked = true;
            }
            get { return curveMode; }
        }

        public enum PoseEditMode { FK, IK }
        private PoseEditMode poseMode;


        public PoseEditMode PoseMode
        {
            get { return poseMode; }
            set
            {
                GetPoseModeButton(poseMode).Checked = false;
                poseMode = value;
                GetPoseModeButton(poseMode).Checked = true;
            }
        }


        public void SetCurveMode()
        {
            editMode = EditMode.Curve;
        }
        public void SetPoseMode()
        {
            editMode = EditMode.Pose;
        }

        public void SetAddKeyMode()
        {
            CurveMode = CurveEditMode.AddKeyframe;
        }
        public void SetZoneMode()
        {
            CurveMode = CurveEditMode.Zone;
        }

        public void SetSegmentMode()
        {
            curveMode = CurveEditMode.Segment;
        }

        public void SetTangentMode()
        {
            curveMode = CurveEditMode.Tangents;
        }

        public void SetFKMode()
        {
            PoseMode = PoseEditMode.FK;
        }

        public void SetIKMode()
        {
            PoseMode = PoseEditMode.IK;
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
            SegmentModeButton = panel.Find("Segment");
            TangentModeButton = panel.Find("Tangent");
            ZoneSlider = panel.Find("ZoneSize");
            CurveModeButton = panel.Find("Curve");
            PoseModeButton = panel.Find("Pose");
            FKModeButton = panel.Find("FK");
            IKModeButton = panel.Find("IK");

            zoneSize = Mathf.RoundToInt(ZoneSlider.GetComponent<UISlider>().Value);
            CurveMode = CurveEditMode.AddKeyframe;
            editMode = EditMode.Curve;
        }

        private UIButton GetCurveModeButton(CurveEditMode mode)
        {
            switch (mode)
            {
                case CurveEditMode.AddKeyframe: return AddKeyModeButton.GetComponent<UIButton>();
                case CurveEditMode.Zone: return ZoneModeButton.GetComponent<UIButton>();
                case CurveEditMode.Segment: return SegmentModeButton.GetComponent<UIButton>();
                case CurveEditMode.Tangents: return TangentModeButton.GetComponent<UIButton>();
                default: return null;
            }
        }

        private UIButton GetModeButton(EditMode mode)
        {
            switch (mode)
            {
                case EditMode.Curve: return CurveModeButton.GetComponent<UIButton>();
                case EditMode.Pose: return PoseModeButton.GetComponent<UIButton>();
                default: return null;
            }
        }

        private UIButton GetPoseModeButton(PoseEditMode mode)
        {
            switch (mode)
            {
                case PoseEditMode.FK: return FKModeButton.GetComponent<UIButton>();
                case PoseEditMode.IK: return IKModeButton.GetComponent<UIButton>();
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
                    if (null == curveManip)
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

        #region Ghost&Curve
        public void DrawCurveGhost(GameObject curveObject, Vector3 point)
        {
            LineRenderer line = curveObject.GetComponent<LineRenderer>();
            int frame = GetFrameFromPoint(line, point);
            GameObject gobject = CurveManager.GetObjectFromCurve(curveObject);
            DrawCurveGhost(gobject, frame);
            if (curveMode == CurveEditMode.Zone || curveMode == CurveEditMode.Segment || curveMode == CurveEditMode.Tangents) DrawZone(line, frame);
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
                AnimationSet set = GlobalState.Animation.GetObjectAnimation(gobject);
                set.EvaluateTransform(frame, ghost.transform);
            }
            else if (gobject.TryGetComponent<HumanGoalController>(out HumanGoalController controller))
            {
                Matrix4x4 matrix = controller.FrameMatrix(frame);
                Maths.DecomposeMatrix(matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);
                ghost.transform.SetPositionAndRotation(position, rotation);
            }
        }

        internal void DrawCurveGhost()
        {
            DrawCurveGhost(curveManip.Target, curveManip.Frame);
            if (curveMode == CurveEditMode.Zone || curveMode == CurveEditMode.Segment || curveMode == CurveEditMode.Tangents) DrawZoneDrag();
        }
        internal void ShowGhost(bool state)
        {
            if (null == ghost) return;
            ghost.SetActive(state);
            foreach (Transform child in mouthpiece)
            {
                child.gameObject.SetActive(!state);
            }
            if (!state) ResetColor();
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
            if (CurveManager.TryGetLine(curveManip.Target, out LineRenderer line))
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
        #endregion

        #region PoseMode

        public void StartPose(HumanGoalController controller, Transform mouthpiece)
        {
            poseManip = new PoseManipulation(controller.transform, controller.PathToRoot, mouthpiece, PoseMode);
        }
        public void DragPose(Transform mouthpiece)
        {
            poseManip.SetDestination(mouthpiece);
            poseManip.TrySolver();
        }

        public void EndPose(Transform mouthpiece)
        {
            poseManip.GetCommand().Submit();
            poseManip = null;
        }

        #endregion

        #region CurveMode
        public void StartDrag(GameObject gameObject, Transform mouthpiece)
        {

            LineRenderer line = gameObject.GetComponent<LineRenderer>();
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            int frame = GetFrameFromPoint(line, mouthpiece.position);

            if (target.TryGetComponent<HumanGoalController>(out HumanGoalController controller))
            {
                curveManip = new CurveManipulation(target, controller, frame, mouthpiece, CurveMode, zoneSize);
            }
            else
            {
                curveManip = new CurveManipulation(target, frame, mouthpiece, curveMode, zoneSize);
            }
        }

        internal void DragCurve(Transform mouthpiece)
        {
            scaleIndice = 1f;
            curveManip.DragCurve(mouthpiece, scaleIndice);
        }

        public void ReleaseCurve(Transform mouthpiece)
        {
            curveManip.ReleaseCurve(mouthpiece, scaleIndice);
            curveManip = null;
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

        #endregion
    }

}