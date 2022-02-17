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
        private bool lockTangents = true;

        public CommandAddKeyframeTangent(GameObject obj, AnimatableProperty property, int frame, int startFrame, int endFrame, List<AnimationKey> keysChanged)
        {
            gObject = obj;
            this.property = property;
            oldKeys = new List<AnimationKey>();
            newKeys = keysChanged;

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet) return;
            Curve curve = animationSet.GetCurve(property);

            curve.GetTangentKeys(startFrame, endFrame, ref oldKeys);
            //if (property == AnimatableProperty.PositionX)
            //{
            //    string deb = "new keys : ";
            //    keysChanged.ForEach(x => deb += " " + x.frame);
            //    deb += " old keys ";
            //    oldKeys.ForEach(x => deb += " " + x.frame);
            //    Debug.Log(deb);
            //}
        }

        //public CommandAddKeyframeTangent(GameObject obj, AnimatableProperty property, int frame, int start, int end, List<AnimationKey> keysChanged)
        //{
        //    gObject = obj;
        //    this.property = property;
        //    oldKeys = new List<AnimationKey>();
        //    newKeys = keysChanged;
        //    AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
        //    if (null == animationSet) return;
        //    Curve curve = animationSet.GetCurve(property);
        //    lockTangents = true;
        //    curve.GetTangentKeys(frame, start, end, ref oldKeys);
        //}

        public override void Redo()
        {
            oldKeys.ForEach(x =>
            {
                if (!newKeys.Exists(y => x.frame == y.frame))
                {
                    SceneManager.RemoveKeyframe(gObject, property, x, false, true);
                }
            });
            newKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x), false, lockTangents));
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            newKeys.ForEach(x =>
            {
                if (!oldKeys.Exists(y => x.frame == y.frame))
                {
                    SceneManager.RemoveKeyframe(gObject, property, x, false, true);
                }
            });
            oldKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x), false, lockTangents));
        }
    }
}
