using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddKeyframeSegment : ICommand
    {
        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly List<AnimationKey> oldKeys;
        readonly List<AnimationKey> newKeys;

        public CommandAddKeyframeSegment(GameObject obj, AnimatableProperty property, int frame, float value, int zoneSize, Interpolation interpolation)
        {
            gObject = obj;
            this.property = property;
            oldKeys = new List<AnimationKey>();
            newKeys = new List<AnimationKey>();

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet) return;
            Curve curve = animationSet.GetCurve(property);

            AnimationKey newKey = new AnimationKey(frame, value, interpolation);
            curve.GetSegmentKeyChanges(newKey, zoneSize, oldKeys, newKeys);
        }

        public override void Redo()
        {
            oldKeys.ForEach(x => SceneManager.RemoveKeyframe(gObject, property, new AnimationKey(x.frame, x.value, x.interpolation, x.inTangent, x.outTangent), false));
            newKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x.frame, x.value, x.interpolation, x.inTangent, x.outTangent), false));
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            newKeys.ForEach(x => SceneManager.RemoveKeyframe(gObject, property, new AnimationKey(x.frame, x.value, x.interpolation, x.inTangent, x.outTangent), false));
            oldKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x.frame, x.value, x.interpolation, x.inTangent, x.outTangent), false));
        }

    }
}