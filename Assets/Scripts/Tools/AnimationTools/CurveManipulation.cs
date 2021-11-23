using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class CurveManipulation
    {
        private bool isHuman;
        public GameObject Target;
        public int Frame;

        public struct ObjectData
        {
            public AnimationSet Animation;
            public Matrix4x4 InitialParentMatrix;
            public Matrix4x4 InitialParentMatrixWorldToLocal;
            public Matrix4x4 InitialTRS;
            public float ScaleIndice;
            public TangentSimpleSolver Solver;
        }
        private ObjectData objectData;

        public struct HumanData
        {
            public HumanGoalController Controller;
            public List<AnimationSet> Animations;
            public AnimationSet ObjectAnimation;
            public Matrix4x4 InitFrameMatrix;
            public TangentHumanSolver Solver;
        }
        private HumanData humanData;

        private Matrix4x4 initialMouthMatrix;


        private int startFrame;
        private int endFrame;

        private AnimationTool.CurveEditMode manipulationMode;


        public CurveManipulation(GameObject target, HumanGoalController controller, int frame, Transform mouthpiece, AnimationTool.CurveEditMode manipMode, int zoneSize)
        {
            isHuman = true;
            manipulationMode = manipMode;
            this.zoneSize = zoneSize;
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            Frame = frame;
            Target = target;

            List<AnimationSet> previousSets = new List<AnimationSet>();
            controller.AnimToRoot.ForEach(x =>
            {
                if (null != x) previousSets.Add(new AnimationSet(x));
            });
            humanData = new HumanData()
            {
                Animations = previousSets,
                Controller = controller,
                ObjectAnimation = new AnimationSet(controller.Animation),
                InitFrameMatrix = controller.FrameMatrix(frame)
            };
            AddSegmentHierarchy(controller, frame);
            AddSegmentKeyframes(frame, controller.Animation);
        }

        public CurveManipulation(GameObject target, int frame, Transform mouthpiece, AnimationTool.CurveEditMode manipMode, int zoneSize)
        {
            isHuman = false;
            manipulationMode = manipMode;
            this.zoneSize = zoneSize;
            AnimationSet previousSet = GlobalState.Animation.GetObjectAnimation(target);
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            Target = target;
            Frame = frame;


            if (!previousSet.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = target.transform.localPosition.x;
            if (!previousSet.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = target.transform.localPosition.y;
            if (!previousSet.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = target.transform.localPosition.z;
            if (!previousSet.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = target.transform.localEulerAngles.x;
            if (!previousSet.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = target.transform.localEulerAngles.y;
            if (!previousSet.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = target.transform.localEulerAngles.z;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = target.transform.localScale.x;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = target.transform.localScale.y;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = target.transform.localScale.z;

            Vector3 initialPosition = new Vector3(posx, posy, posz);
            Quaternion initialRotation = Quaternion.Euler(rotx, roty, rotz);
            Vector3 initialScale = new Vector3(scax, scay, scaz);

            objectData = new ObjectData()
            {
                Animation = new AnimationSet(previousSet),
                InitialParentMatrix = target.transform.parent.localToWorldMatrix,
                InitialParentMatrixWorldToLocal = target.transform.parent.worldToLocalMatrix,
                InitialTRS = Matrix4x4.TRS(initialPosition, initialRotation, initialScale),
                ScaleIndice = 1f
            };
            if (manipulationMode == AnimationTool.CurveEditMode.Segment) AddSegmentKeyframes(frame, previousSet);
        }

        public void DragCurve(Transform mouthpiece, float scaleIndice)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            if (isHuman)
            {
                DragHuman(transformation);
            }
            else DragObject(transformation, scaleIndice);
        }

        private void DragHuman(Matrix4x4 transformation)
        {
            Matrix4x4 target = transformation * humanData.InitFrameMatrix;
            Maths.DecomposeMatrix(target, out Vector3 targetPos, out Quaternion targetRot, out Vector3 targetScale);
            TangentHumanSolver solver = new TangentHumanSolver(targetPos, targetRot, humanData.Controller.Animation, humanData.Controller.AnimToRoot, Frame, zoneSize);
            solver.TrySolver();
            humanData.Solver = solver;
            GlobalState.Animation.onChangeCurve.Invoke(humanData.Controller.RootController.gameObject, AnimatableProperty.PositionX);
        }

        private void DragObject(Matrix4x4 transformation, float scaleIndice)
        {
            Matrix4x4 transformed = objectData.InitialParentMatrixWorldToLocal *
                transformation * objectData.InitialParentMatrix *
                objectData.InitialTRS;

            Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion qrotation, out Vector3 scale);
            Vector3 rotation = qrotation.eulerAngles;
            scale *= scaleIndice;

            Interpolation interpolation = GlobalState.Settings.interpolation;
            AnimationKey posX = new AnimationKey(Frame, position.x, interpolation);
            AnimationKey posY = new AnimationKey(Frame, position.y, interpolation);
            AnimationKey posZ = new AnimationKey(Frame, position.z, interpolation);
            AnimationKey rotX = new AnimationKey(Frame, rotation.x, interpolation);
            AnimationKey rotY = new AnimationKey(Frame, rotation.y, interpolation);
            AnimationKey rotZ = new AnimationKey(Frame, rotation.z, interpolation);
            AnimationKey scalex = new AnimationKey(Frame, scale.z, interpolation);
            AnimationKey scaley = new AnimationKey(Frame, scale.z, interpolation);
            AnimationKey scalez = new AnimationKey(Frame, scale.z, interpolation);

            switch (manipulationMode)
            {
                case AnimationTool.CurveEditMode.AddKeyframe:
                    AddFilteredKeyframe(Target, posX, posY, posZ, rotX, rotY, rotZ, scalex, scaley, scalez);
                    break;
                case AnimationTool.CurveEditMode.Zone:
                    AddFilteredKeyframeZone(Target, posX, posY, posZ, rotX, rotY, rotZ, scalex, scaley, scalez);
                    break;
                case AnimationTool.CurveEditMode.Segment:
                    objectData.Solver = new TangentSimpleSolver(position, qrotation, GlobalState.Animation.GetObjectAnimation(Target), Frame, zoneSize);
                    objectData.Solver.TrySolver();
                    GlobalState.Animation.onChangeCurve.Invoke(Target, AnimatableProperty.PositionX);
                    break;
                case AnimationTool.CurveEditMode.Tangents:
                    break;
            }
        }

        public void ReleaseCurve(Transform mouthpiece, float scaleIndice)
        {
            if (isHuman) ReleaseHuman();
            else ReleaseObject(mouthpiece, scaleIndice);
        }

        private void ReleaseObject(Transform mouthpiece, float scaleIndice)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            Matrix4x4 transformed = objectData.InitialParentMatrixWorldToLocal *
                transformation * objectData.InitialParentMatrix *
                objectData.InitialTRS;

            Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion qrotation, out Vector3 scale);
            Vector3 rotation = qrotation.eulerAngles;
            scale *= scaleIndice;
            GlobalState.Animation.SetObjectAnimations(Target, objectData.Animation);

            CommandGroup group = new CommandGroup("Add Keyframe");
            switch (manipulationMode)
            {
                case AnimationTool.CurveEditMode.AddKeyframe:
                    new CommandAddKeyframes(Target, Frame, position, rotation, scale).Submit();
                    break;
                case AnimationTool.CurveEditMode.Zone:
                    new CommandAddKeyframes(Target, Frame, zoneSize, position, rotation, scale).Submit();
                    break;
                case AnimationTool.CurveEditMode.Segment:
                    Dictionary<AnimatableProperty, List<AnimationKey>> keyframeList = new Dictionary<AnimatableProperty, List<AnimationKey>>();

                    for (int prop = 0; prop < 6; prop++)
                    {
                        AnimatableProperty property = (AnimatableProperty)prop;
                        keyframeList.Add(property, new List<AnimationKey>());
                        int firstKey = Mathf.Max(0, objectData.Solver.RequiredKeyframeIndices[0] - 1);
                        int lastKey = Mathf.Min(objectData.Solver.ObjectAnimation.GetCurve(property).keys.Count - 1, objectData.Solver.RequiredKeyframeIndices[objectData.Solver.RequiredKeyframeIndices.Count - 1] + 1);
                        for (int i = firstKey; i <= lastKey; i++)
                        {
                            keyframeList[property].Add(objectData.Solver.ObjectAnimation.GetCurve(property).keys[i]);
                        }
                    }
                    new CommandAddKeyframes(Target, Frame, zoneSize, keyframeList).Submit();
                    break;
            }
            group.Submit();
        }

        private void ReleaseHuman()
        {
            List<GameObject> objectList = new List<GameObject>();
            List<Dictionary<AnimatableProperty, List<AnimationKey>>> keyframesLists = new List<Dictionary<AnimatableProperty, List<AnimationKey>>>();

            int index = 0;
            for (int i = 0; i < humanData.Controller.PathToRoot.Count; i++)
            {
                if (humanData.Controller.AnimToRoot[i] == null) continue;
                keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                for (int prop = 0; prop < 6; prop++)
                {
                    AnimatableProperty property = (AnimatableProperty)prop;
                    List<AnimationKey> keys = new List<AnimationKey>();
                    Curve curve = humanData.Controller.AnimToRoot[i].GetCurve(property);

                    curve.GetKeyIndex(humanData.Solver.requiredKeyframe[0], out int beforKey);
                    if (beforKey > 0) keys.Add(curve.keys[beforKey - 1]);

                    for (int k = 0; k < humanData.Solver.requiredKeyframe.Count; k++)
                    {
                        curve.GetKeyIndex(humanData.Solver.requiredKeyframe[k], out int keyIndex);
                        keys.Add(curve.keys[keyIndex]);
                    }

                    curve.GetKeyIndex(humanData.Solver.requiredKeyframe[humanData.Solver.requiredKeyframe.Count - 1], out int afterKey);
                    if (afterKey < curve.keys.Count - 1) keys.Add(curve.keys[afterKey + 1]);
                    keyframesLists[keyframesLists.Count - 1].Add(property, keys);
                }
                GlobalState.Animation.SetObjectAnimations(humanData.Animations[index].transform.gameObject, humanData.Animations[index]);
                objectList.Add(humanData.Animations[index].transform.gameObject);
                index++;
            }

            keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
            for (int prop = 0; prop < 6; prop++)
            {
                AnimatableProperty property = (AnimatableProperty)prop;
                List<AnimationKey> keys = new List<AnimationKey>();
                Curve curve = humanData.Controller.Animation.GetCurve(property);

                curve.GetKeyIndex(humanData.Solver.requiredKeyframe[0], out int beforKey);
                if (beforKey > 0) keys.Add(curve.keys[beforKey - 1]);

                for (int k = 0; k < humanData.Solver.requiredKeyframe.Count; k++)
                {
                    curve.GetKeyIndex(humanData.Solver.requiredKeyframe[k], out int keyIndex);
                    keys.Add(curve.keys[keyIndex]);
                }
                curve.GetKeyIndex(humanData.Solver.requiredKeyframe[humanData.Solver.requiredKeyframe.Count - 1], out int afterKey);
                if (afterKey < curve.keys.Count - 1) keys.Add(curve.keys[afterKey + 1]);

                keyframesLists[keyframesLists.Count - 1].Add(property, keys);
            }
            GlobalState.Animation.SetObjectAnimations(Target, humanData.ObjectAnimation);
            objectList.Add(Target);

            GlobalState.Animation.onChangeCurve.Invoke(humanData.Animations[0].transform.gameObject, AnimatableProperty.PositionX);
            CommandGroup group = new CommandGroup("Add Keyframe");
            new CommandAddKeyframes(humanData.Controller.RootController.gameObject, objectList, Frame, zoneSize, keyframesLists).Submit();
            group.Submit();
        }

        private void AddSegmentKeyframes(int frame, AnimationSet animation)
        {
            if (!animation.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = animation.transform.localPosition.x;
            if (!animation.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = animation.transform.localPosition.y;
            if (!animation.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = animation.transform.localPosition.z;
            if (!animation.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = animation.transform.localEulerAngles.x;
            if (!animation.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = animation.transform.localEulerAngles.y;
            if (!animation.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = animation.transform.localEulerAngles.z;
            if (!animation.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = animation.transform.localScale.x;
            if (!animation.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = animation.transform.localScale.y;
            if (!animation.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = animation.transform.localScale.z;

            AddFilteredKeyframeTangent(animation.transform.gameObject,
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

        private void AddSegmentHierarchy(HumanGoalController controller, int frame)
        {
            for (int i = 0; i < controller.AnimToRoot.Count; i++)
            {
                AnimationSet anim = controller.AnimToRoot[i];
                if (null != anim)
                    AddSegmentKeyframes(frame, anim);
            }
        }


        private void AddFilteredKeyframeTangent(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scaleX, AnimationKey scaleY, AnimationKey scaleZ)
        {
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationX, rotX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationY, rotY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationZ, rotZ, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleX, scaleX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleY, scaleY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleZ, scaleZ, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionX, posX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionY, posY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionZ, posZ, zoneSize, false);


        }

        private void AddFilteredKeyframe(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.RotationX, rotX, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.RotationY, rotY, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.RotationZ, rotZ, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.ScaleX, scalex, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.ScaleY, scaley, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.ScaleZ, scalez, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.PositionY, posY, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.PositionZ, posZ, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.PositionX, posX);
        }

        private void AddFilteredKeyframeZone(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.RotationX, rotX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.RotationY, rotY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.RotationZ, rotZ, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.ScaleX, scalex, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.ScaleY, scaley, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.ScaleZ, scalez, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.PositionX, posX, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.PositionY, posY, zoneSize, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.PositionZ, posZ, zoneSize);
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

    }

}