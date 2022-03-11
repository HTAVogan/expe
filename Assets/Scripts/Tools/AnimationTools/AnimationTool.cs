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
        private Transform ContSlider;
        private Transform SkeletonDisplay;
        private Transform OffsetLabel;

        private int zoneSize;
        private float tanCont;

        private int startFrame;
        private int endFrame;

        private LineRenderer lastLine;
        private Texture2D lastTexture;

        public Color DefaultColor;
        public Color ZoneColor;

        private CurveManipulation curveManip;
        private PoseManipulation poseManip;

        private List<GameObject> SelectedCurves = new List<GameObject>();

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

        private float offsetValue;
        public float Offsetvalue
        {
            get { return offsetValue; }
            set
            {
                offsetValue = value;
                GlobalState.Settings.curveForwardOffset = offsetValue;
                OffsetLabel.GetComponent<UILabel>().Text = "Offset : " + offsetValue;
            }
        }

        public void AddOffset()
        {
            Offsetvalue += 0.5f;
        }
        public void RemoveOffset()
        {
            Offsetvalue -= 0.5f;
        }

        public void SetCurveMode()
        {
            Mode = EditMode.Curve;
        }
        public void SetPoseMode()
        {
            Mode = EditMode.Pose;
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
            CurveMode = CurveEditMode.Segment;
        }

        public void SetTangentMode()
        {
            CurveMode = CurveEditMode.Tangents;
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

        public void SetTanCont(float value)
        {
            tanCont = value;
            ContSlider.GetComponent<UISlider>().Value = tanCont;
        }

        public void SetSkeleton(bool value)
        {
            Debug.Log(value);
            GlobalState.Settings.DisplaySkeletons = value;
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
            ContSlider = panel.Find("Tangents");
            SkeletonDisplay = panel.Find("Skeleton");
            OffsetLabel = panel.Find("OffsetValue");
            SkeletonDisplay.GetComponent<UICheckbox>().Checked = GlobalState.Settings.DisplaySkeletons;

            Offsetvalue = GlobalState.Settings.CurveForwardOffset;
            zoneSize = Mathf.RoundToInt(ZoneSlider.GetComponent<UISlider>().Value);
            CurveMode = CurveEditMode.AddKeyframe;
            Mode = EditMode.Curve;
            PoseMode = PoseEditMode.FK;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (GameObject select in Selection.SelectedObjects)
            {
                if (select.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
                {
                    HumanGoalController[] GoalController = controller.GetComponentsInChildren<HumanGoalController>();
                    for (int i = 0; i < GoalController.Length; i++)
                    {
                        GoalController[i].UseGoal(true);
                    }
                }
            }
            GlobalState.Instance.onGripWorldEvent.AddListener(OnGripWorld);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (GameObject select in Selection.SelectedObjects)
            {
                if (select.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
                {
                    HumanGoalController[] GoalController = controller.GetComponentsInChildren<HumanGoalController>();
                    for (int i = 0; i < GoalController.Length; i++)
                    {
                        GoalController[i].UseGoal(false);
                    }
                }
            }
            //GlobalState.Instance.onGripWorldEvent.RemoveListener(OnGripWorld);
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
        public void OnGripWorld(bool state)
        {
            if (state && poseManip != null) EndPose();
            if (state && curveManip != null) ReleaseCurve();
            if (state && movedObjects.Count > 0) EndDragObject();
        }

        #region Ghost&Curve
        public void DrawCurveGhost(GameObject curveObject, Vector3 point)
        {
            LineRenderer line = curveObject.GetComponent<LineRenderer>();
            int frame = GetFrameFromPoint(line, point);
            GameObject gobject = CurveManager.GetObjectFromCurve(curveObject);
            DrawCurveGhost(gobject, frame);
            if (CurveMode == CurveEditMode.Zone || CurveMode == CurveEditMode.Segment) DrawZone(line, frame - zoneSize, frame + zoneSize);
            if (CurveMode == CurveEditMode.Tangents)
            {
                AnimationSet anim = GlobalState.Animation.GetObjectAnimation(gobject);
                Curve curve = anim.GetCurve(AnimatableProperty.PositionX);
                int prev = curve.GetPreviousKeyFrame(frame);
                int next = curve.GetNextKeyFrame(frame);
                DrawZone(line, prev, next);

            }
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
            if (CurveMode == CurveEditMode.Zone || CurveMode == CurveEditMode.Segment || CurveMode == CurveEditMode.Tangents) DrawZoneDrag();
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

        public void DrawZone(LineRenderer line, int start, int end)
        {
            lastTexture = (Texture2D)line.material.mainTexture;
            if (lastTexture == null)
            {
                lastTexture = new Texture2D(line.positionCount, 1, TextureFormat.RGBA32, false);
                line.material.mainTexture = lastTexture;
            }

            ApplyTexture(start, end);
            lastLine = line;
        }
        public void DrawZoneDrag()
        {
            if (CurveManager.TryGetLine(curveManip.Target, out LineRenderer line))
            {
                line.material.mainTexture = lastTexture;
            }
        }

        private void ApplyTexture(int start, int end)
        {
            NativeArray<Color32> colors = lastTexture.GetRawTextureData<Color32>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (i < start || i > end)
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
            poseManip = new PoseManipulation(controller.transform, controller.PathToRoot, mouthpiece, controller.RootController, PoseMode);
        }
        public bool DragPose(Transform mouthpiece)
        {
            if (poseManip == null) return false;
            poseManip.SetDestination(mouthpiece);
            poseManip.TrySolver();
            return true;
        }

        public void EndPose()
        {
            poseManip.GetCommand().Submit();
            if (GlobalState.Animation.autoKeyEnabled) new CommandAddKeyframes(poseManip.MeshController.gameObject, false).Submit();
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
                curveManip = new CurveManipulation(target, controller, frame, mouthpiece, CurveMode, zoneSize, (double)tanCont);
            }
            else
            {
                curveManip = new CurveManipulation(target, frame, mouthpiece, CurveMode, zoneSize, (double)tanCont);
            }
        }

        internal bool DragCurve(Transform mouthpiece)
        {
            if (curveManip == null) return false;
            scaleIndice = 1f;
            curveManip.DragCurve(mouthpiece, scaleIndice);
            return true;
        }

        public void ReleaseCurve()
        {
            curveManip.ReleaseCurve();
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

        //public void SelectCurve(GameObject curve, Transform mouthpiece)
        //{
        //    GameObject target = CurveManager.GetObjectFromCurve(curve);
        //    if (SelectedCurves.Contains(target))
        //        return;
        //    SelectedCurves.Add(target);
        //    AnimationEngine.Instance.OnCurveSelectionChanged.Invoke(SelectedCurves);
        //}

        //public void ClearSelectCurves()
        //{
        //    SelectedCurves.Clear();
        //    AnimationEngine.Instance.OnCurveSelectionChanged.Invoke(SelectedCurves);
        //}

        #endregion

        #region DragObject

        Matrix4x4 initMouthPieceWorldToLocal;
        List<GameObject> movedObjects = new List<GameObject>();
        Dictionary<GameObject, Matrix4x4> initParentMatrixLtW = new Dictionary<GameObject, Matrix4x4>();
        Dictionary<GameObject, Matrix4x4> initParentMatrixWtL = new Dictionary<GameObject, Matrix4x4>();
        Dictionary<GameObject, Vector3> initPositions = new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, Quaternion> initRotation = new Dictionary<GameObject, Quaternion>();
        Dictionary<GameObject, Vector3> initScale = new Dictionary<GameObject, Vector3>();

        public void StartDragObject(GameObject gobject, Transform mouthpiece)
        {
            initMouthPieceWorldToLocal = mouthpiece.worldToLocalMatrix;

            initParentMatrixLtW[gobject] = gobject.transform.parent.localToWorldMatrix;
            initParentMatrixWtL[gobject] = gobject.transform.parent.worldToLocalMatrix;
            initPositions[gobject] = gobject.transform.localPosition;
            initRotation[gobject] = gobject.transform.localRotation;
            initScale[gobject] = gobject.transform.localScale;
            movedObjects.Add(gobject);
        }

        public void DragObject(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initMouthPieceWorldToLocal;
            movedObjects.ForEach(x =>
            {
                Matrix4x4 transformed = initParentMatrixWtL[x] * transformation * initParentMatrixLtW[x] * Matrix4x4.TRS(initPositions[x], initRotation[x], initScale[x]);
                Maths.DecomposeMatrix(transformed, out Vector3 pos, out Quaternion rot, out Vector3 scale);
                x.transform.localPosition = pos;
                x.transform.localRotation = rot;
                x.transform.localScale = scale;
            });
        }

        public void EndDragObject()
        {
            List<Vector3> beginPositions = new List<Vector3>();
            List<Quaternion> beginRotations = new List<Quaternion>();
            List<Vector3> beginScales = new List<Vector3>();
            List<Vector3> endPositions = new List<Vector3>();
            List<Quaternion> endRotations = new List<Quaternion>();
            List<Vector3> endScales = new List<Vector3>();

            foreach (GameObject gobject in movedObjects)
            {
                beginPositions.Add(initPositions[gobject]);
                beginRotations.Add(initRotation[gobject]);
                beginScales.Add(initScale[gobject]);
                endPositions.Add(gobject.transform.localPosition);
                endRotations.Add(gobject.transform.localRotation);
                endScales.Add(gobject.transform.localScale);
            }

            new CommandMoveObjects(movedObjects, beginPositions, beginRotations, beginScales, endPositions, endRotations, endScales).Submit();
            movedObjects.Clear();
        }
        #endregion

    }

}