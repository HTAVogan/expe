using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandClearRecursiveAnimations : ICommand
    {
        readonly GameObject gObject;
        readonly Dictionary<GameObject, AnimationSet> animationSets;

        public CommandClearRecursiveAnimations(GameObject obj)
        {
            gObject = obj;
            animationSets = new Dictionary<GameObject, AnimationSet>();
            RecursiveClear(obj.transform);
        }

        private void RecursiveClear(Transform target)
        {
            AnimationSet anim = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != anim)
                animationSets.Add(target.gameObject, anim);

            foreach (Transform child in target)
            {
                RecursiveClear(child);
            }
        }

        public override void Redo()
        {
            foreach (KeyValuePair<GameObject, AnimationSet> pair in animationSets)
            {
                SceneManager.ClearObjectAnimations(pair.Key, false);
            }
            GlobalState.Animation.onRemoveAnimation.Invoke(gObject);
        }


        public override void Undo()
        {
            foreach (KeyValuePair<GameObject, AnimationSet> pair in animationSets)
            {
                SceneManager.SetObjectAnimations(pair.Key, pair.Value, false);
            }
            GlobalState.Animation.onAddAnimation.Invoke(gObject);
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}