using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class CommandStartFrame : ICommand
    {

        readonly GameObject gObject;
        readonly int offset;
        readonly List<AnimationSet> animations = new List<AnimationSet>();

        public CommandStartFrame(GameObject obj, int startFrame)
        {
            gObject = obj;

            GetAllAnimations(gObject.transform, animations);

            int firstFrame = int.MaxValue;
            animations.ForEach(x =>
            {
                int fframe = x.GetFirstFrame();
                if (fframe < firstFrame) firstFrame = fframe;
            });
            offset = startFrame - firstFrame;
        }

        private void GetAllAnimations(Transform target, List<AnimationSet> animations)
        {
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != animationSet) animations.Add(animationSet);
            foreach (Transform child in target)
            {
                GetAllAnimations(child, animations);
            }
        }



        public override void Redo()
        {
            animations.ForEach(x => x.SetStartOffset(offset));
            GlobalState.Animation.onStartOffsetChanged.Invoke(gObject);
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            animations.ForEach(x => x.SetStartOffset(-offset));
            GlobalState.Animation.onStartOffsetChanged.Invoke(gObject);
        }
    }
}