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
        private Matrix4x4 initialparentMatrixWtL;
        private Matrix4x4 initialTRS;
        private float scaleIndice;

        private Transform AddKeyModeButton;
        private Transform ZoneModeButton;
        private Transform SegmentModeButton;
        private Transform TangentModeButton;
        private Transform ZoneSlider;

        public enum EditMode { AddKeyframe, Zone, Segment, Tangents }
        private EditMode currentMode;

        private int zoneSize;

        private LineRenderer lastLine;
        private Texture2D lastTexture;

        public Color DefaultColor;
        public Color ZoneColor;

        private bool movingHuman = false;


        public EditMode CurrentMode
        {
            set
            {
                if (value == currentMode) return;
                GetModeButton(currentMode).Selected = false;
                currentMode = value;
                GetModeButton(currentMode).Selected = true;
            }
            get { return currentMode; }
        }

        public struct DraggedCurveData
        {
            public GameObject target;
            public AnimationSet Animation;
            public int Frame;
        }
        public struct DraggedHumanData
        {
            public GameObject target;
            public HumanGoalController controller;
            public int frame;
            public List<AnimationSet> animations;
            public AnimationSet objectAnimation;
            public Matrix4x4 initFrameMatrix;
            public TangentHumanSolver solver;
        }

        private DraggedCurveData dragData = new DraggedCurveData() { Frame = -1 };
        private DraggedHumanData humanDragData = new DraggedHumanData();

        public void SetAddKeyMode()
        {
            CurrentMode = EditMode.AddKeyframe;
        }
        public void SetZoneMode()
        {
            CurrentMode = EditMode.Zone;
        }

        public void SetSegmentMode()
        {
            currentMode = EditMode.Segment;
        }

        public void SetTangentMode()
        {
            currentMode = EditMode.Tangents;
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

            zoneSize = Mathf.RoundToInt(ZoneSlider.GetComponent<UISlider>().Value);
            CurrentMode = EditMode.AddKeyframe;
        }

        private UIButton GetModeButton(EditMode mode)
        {
            switch (mode)
            {
                case EditMode.AddKeyframe: return AddKeyModeButton.GetComponent<UIButton>();
                case EditMode.Zone: return ZoneModeButton.GetComponent<UIButton>();
                case EditMode.Segment: return SegmentModeButton.GetComponent<UIButton>();
                case EditMode.Tangents: return TangentModeButton.GetComponent<UIButton>();
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
            if (currentMode == EditMode.Zone || currentMode == EditMode.Segment || currentMode == EditMode.Tangents) DrawZone(line, frame);
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
            DrawCurveGhost(dragData.target, dragData.Frame);
            if (currentMode == EditMode.Zone || currentMode == EditMode.Segment || currentMode == EditMode.Tangents) DrawZoneDrag();
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

        public void StartDrag(GameObject gameObject, Transform mouthpiece)
        {
            LineRenderer line = gameObject.GetComponent<LineRenderer>();
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            int frame = GetFrameFromPoint(line, mouthpiece.position);
            AnimationSet previousSet = GlobalState.Animation.GetObjectAnimation(target);
            dragData = new DraggedCurveData() { Animation = new AnimationSet(previousSet), target = target, Frame = frame };

            if (!previousSet.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = target.transform.localPosition.x;
            if (!previousSet.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = target.transform.localPosition.y;
            if (!previousSet.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = target.transform.localPosition.z;
            if (!previousSet.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = target.transform.localEulerAngles.x;
            if (!previousSet.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = target.transform.localEulerAngles.y;
            if (!previousSet.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = target.transform.localEulerAngles.z;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = target.transform.localScale.x;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = target.transform.localScale.y;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = target.transform.localScale.z;

            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            initialParentMatrix = target.transform.parent.localToWorldMatrix;
            initialparentMatrixWtL = target.transform.parent.worldToLocalMatrix;
            Vector3 initialPosition = new Vector3(posx, posy, posz);
            Quaternion initialRotation = Quaternion.Euler(rotx, roty, rotz);
            Vector3 initialScale = new Vector3(scax, scay, scaz);
            initialTRS = Matrix4x4.TRS(initialPosition, initialRotation, initialScale);
            scaleIndice = 1f;

            if (target.TryGetComponent<HumanGoalController>(out HumanGoalController controller))
            {
                movingHuman = true;
                List<AnimationSet> previousSets = new List<AnimationSet>();
                controller.AnimToRoot.ForEach(x => { if (null != x) previousSets.Add(new AnimationSet(x)); });
                humanDragData = new DraggedHumanData()
                {
                    animations = previousSets,
                    controller = controller,
                    frame = frame,
                    target = target,
                    objectAnimation = new AnimationSet(controller.Animation),
                    initFrameMatrix = controller.FrameMatrix(frame)
                };
                AddSegmentHierarchy(controller, frame);
            }
            if (currentMode == EditMode.Tangents) AddSegmentKeyframes(frame, previousSet);
        }

        internal void DragCurve(Transform mouthpiece)
        {
            int frame = dragData.Frame;
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = initialparentMatrixWtL *
                transformation * initialParentMatrix *
                initialTRS;

            Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion qrotation, out Vector3 scale);
            Vector3 rotation = qrotation.eulerAngles;
            scale *= scaleIndice;

            if (movingHuman)
            {
                Matrix4x4 target = transformation * humanDragData.initFrameMatrix;
                Maths.DecomposeMatrix(target, out Vector3 targetPos, out Quaternion targetRot, out Vector3 targetScale);
                TangentHumanSolver solver = new TangentHumanSolver(targetPos, targetRot, humanDragData.controller.Animation, humanDragData.controller.AnimToRoot, frame, zoneSize);
                solver.TrySolver();
                GlobalState.Animation.onChangeCurve.Invoke(humanDragData.target.gameObject, AnimatableProperty.PositionX);
                humanDragData.solver = solver;
                return;
            }

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
                AddFilteredKeyframe(posX, posY, posZ, rotX, rotY, rotZ, scalex, scaley, scalez);
            }
            if (currentMode == EditMode.Zone)
            {
                AddFilteredKeyframeZone(posX, posY, posZ, rotX, rotY, rotZ, scalex, scaley, scalez);
            }
            if (currentMode == EditMode.Segment)
            {
                AddFilteredKeyframeSegment(dragData.target, posX, posY, posZ, rotX, rotY, rotZ, scalex, scaley, scalez);
            }
            if (currentMode == EditMode.Tangents)
            {
                TangentSimpleSolver solver = new TangentSimpleSolver(position, qrotation, GlobalState.Animation.GetObjectAnimation(dragData.target), dragData.Frame, zoneSize);
                solver.TrySolver();
                GlobalState.Animation.onChangeCurve.Invoke(dragData.target, AnimatableProperty.PositionX);
            }
        }

        public void ReleaseCurve(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = initialparentMatrixWtL *
                transformation * initialParentMatrix *
                initialTRS;

            Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion qrotation, out Vector3 scale);
            Vector3 rotation = qrotation.eulerAngles;
            scale *= scaleIndice;
            if (!movingHuman)
            {
                GlobalState.Animation.SetObjectAnimations(dragData.target, dragData.Animation);
                CommandGroup group = new CommandGroup("Add Keyframe");
                if (currentMode == EditMode.AddKeyframe) new CommandAddKeyframes(dragData.target, dragData.Frame, position, rotation, scale).Submit();
                if (currentMode == EditMode.Zone) new CommandAddKeyframes(dragData.target, dragData.Frame, zoneSize, position, rotation, scale).Submit();
                if (currentMode == EditMode.Segment) new CommandAddKeyframes(dragData.target, dragData.Frame, zoneSize, position, rotation, scale, true).Submit();
                group.Submit();
            }
            else
            {
                List<GameObject> objectList = new List<GameObject>();
                List<Dictionary<AnimatableProperty, List<AnimationKey>>> keyframesLists = new List<Dictionary<AnimatableProperty, List<AnimationKey>>>();

                int index = 0;
                for (int i = 0; i < humanDragData.controller.PathToRoot.Count; i++)
                {
                    if (humanDragData.controller.AnimToRoot[i] == null) continue;
                    keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                    for (int prop = 0; prop < 6; prop++)
                    {
                        AnimatableProperty property = (AnimatableProperty)prop;
                        List<AnimationKey> keys = new List<AnimationKey>();
                        for (int k = 0; k < humanDragData.solver.requiredKeyframeIndices.Count; k++)
                        {
                            int kk = humanDragData.solver.requiredKeyframeIndices[k];
                            keys.Add(humanDragData.controller.AnimToRoot[i].GetCurve(property).keys[humanDragData.solver.requiredKeyframeIndices[k]]);
                        }
                        keyframesLists[keyframesLists.Count - 1].Add(property, keys);
                    }
                    GlobalState.Animation.SetObjectAnimations(humanDragData.animations[index].transform.gameObject, humanDragData.animations[index]);
                    objectList.Add(humanDragData.animations[index].transform.gameObject);
                    index++;
                }

                keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                for (int prop = 0; prop < 6; prop++)
                {
                    AnimatableProperty property = (AnimatableProperty)prop;
                    List<AnimationKey> keys = new List<AnimationKey>();
                    for (int k = 0; k < humanDragData.solver.requiredKeyframeIndices.Count; k++)
                    {
                        keys.Add(humanDragData.controller.Animation.GetCurve(property).keys[humanDragData.solver.requiredKeyframeIndices[k]]);
                    }
                    keyframesLists[keyframesLists.Count - 1].Add(property, keys);
                }
                GlobalState.Animation.SetObjectAnimations(humanDragData.target, humanDragData.objectAnimation);
                objectList.Add(humanDragData.target);

                GlobalState.Animation.onChangeCurve.Invoke(humanDragData.animations[0].transform.gameObject, AnimatableProperty.PositionX);
                CommandGroup group = new CommandGroup("Add Keyframe");
                new CommandAddKeyframes(humanDragData.target, objectList, humanDragData.frame, zoneSize, keyframesLists).Submit();
                group.Submit();

                GlobalState.Animation.onChangeCurve.Invoke(humanDragData.animations[0].transform.gameObject, AnimatableProperty.PositionX);
            }
            movingHuman = false;
            dragData.Frame = -1;
        }

        

        private void AddFilteredKeyframe(AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationX, rotX, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationY, rotY, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.RotationZ, rotZ, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.ScaleX, scalex, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.ScaleY, scaley, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.ScaleZ, scalez, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionX, posX, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionY, posY, false);
            GlobalState.Animation.AddFilteredKeyframe(dragData.target, AnimatableProperty.PositionZ, posZ);
        }

        private void AddFilteredKeyframeZone(AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.RotationX, rotX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.RotationY, rotY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.RotationZ, rotZ, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.ScaleX, scalex, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.ScaleY, scaley, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.ScaleZ, scalez, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.PositionX, posX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.PositionY, posY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(dragData.target, AnimatableProperty.PositionZ, posZ, zoneSize);
        }

        private void AddFilteredKeyframeSegment(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.RotationX, rotX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.RotationY, rotY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.RotationZ, rotZ, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.ScaleX, scalex, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.ScaleY, scaley, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.ScaleZ, scalez, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.PositionX, posX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.PositionY, posY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeSegment(target, AnimatableProperty.PositionZ, posZ, zoneSize);
        }

        private void AddFilteredKeyframeTangent(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationX, rotX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationY, rotY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationZ, rotZ, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleX, scalex, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleY, scaley, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleZ, scalez, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionX, posX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionY, posY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionZ, posZ, zoneSize);
        }

        public void AddSegmentHierarchy(HumanGoalController controller, int frame)
        {
            for (int i = 0; i < controller.AnimToRoot.Count; i++)
            {
                AnimationSet anim = controller.AnimToRoot[i];
                if (null != anim)
                    AddSegmentKeyframes(frame, anim);
            }
        }

        private void AddSegmentKeyframes(int frame, AnimationSet anim)
        {
            if (!anim.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = anim.transform.localPosition.x;
            if (!anim.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = anim.transform.localPosition.y;
            if (!anim.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = anim.transform.localPosition.z;
            if (!anim.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = anim.transform.localEulerAngles.x;
            if (!anim.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = anim.transform.localEulerAngles.y;
            if (!anim.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = anim.transform.localEulerAngles.z;
            if (!anim.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = anim.transform.localScale.x;
            if (!anim.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = anim.transform.localScale.y;
            if (!anim.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = anim.transform.localScale.z;

            AddFilteredKeyframeTangent(anim.transform.gameObject,
                new AnimationKey(frame, posx),
                new AnimationKey(frame, posy),
                new AnimationKey(frame, posz),
                new AnimationKey(frame, rotx),
                new AnimationKey(frame, roty),
                new AnimationKey(frame, rotz),
                new AnimationKey(frame, scax),
                new AnimationKey(frame, scay),
                new AnimationKey(frame, scaz));
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

        
    }

}