using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddKeyframeTangent : ICommand
    {

        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly List<AnimationKey> oldKeys;
        readonly List<AnimationKey> newKeys;

        public CommandAddKeyframeTangent(GameObject obj, AnimatableProperty property, int frame, int zoneSize, List<AnimationKey> keysChanged)
        {
            gObject = obj;
            this.property = property;
            oldKeys = new List<AnimationKey>();
            newKeys = keysChanged;

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet) return;
            Curve curve = animationSet.GetCurve(property);

            curve.GetTangentKeys(frame, zoneSize, ref oldKeys);
        }

        public override void Redo()
        {
            oldKeys.ForEach(x => SceneManager.RemoveKeyframe(gObject, property, x, false));
            newKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x), false));
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            newKeys.ForEach(x => SceneManager.RemoveKeyframe(gObject, property, x, false));
            oldKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x), false));
        }
    }
}
