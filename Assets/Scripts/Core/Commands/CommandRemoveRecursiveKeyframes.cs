using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class CommandRemoveRecursiveKeyframes : CommandGroup
    {
        readonly GameObject gObject;


        public CommandRemoveRecursiveKeyframes(GameObject obj) : base("Remove Keyframes")
        {
            gObject = obj;
            int frame = GlobalState.Animation.CurrentFrame;
            RecursiveRemove(obj.transform, frame);
        }

        public void RecursiveRemove(Transform target, int frame)
        {
            AnimationSet anim = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != anim)
            {
                foreach (Curve curve in anim.curves.Values)
                {
                    if (curve.HasKeyAt(frame)) new CommandRemoveKeyframe(target.gameObject, curve.property, frame, false).Submit();
                }
            }
            foreach (Transform child in target)
            {
                RecursiveRemove(child, frame);
            }
        }

        public override void Undo()
        {
            base.Undo();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }

        public override void Redo()
        {
            base.Redo();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }

        public override void Submit()
        {
            base.Submit();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }

    }

}