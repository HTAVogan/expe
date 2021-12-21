using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddKeyframeSegment : ICommand
    {

        public CommandAddKeyframeSegment(GameObject obj, AnimatableProperty property, int frame, float value, int zoneSize, Interpolation interpolation)
        {

        }

        public override void Redo()
        {

        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {

        }

    }
}
