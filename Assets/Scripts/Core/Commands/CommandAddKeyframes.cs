/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to add keyframes to all supported properties of an object.
    /// </summary>
    public class CommandAddKeyframes : CommandGroup
    {
        readonly GameObject gObject;
        public CommandAddKeyframes(GameObject obj, bool updateCurve = true) : base("Add Keyframes")
        {
            gObject = obj;
            Interpolation interpolation = GlobalState.Settings.interpolation;
            int frame = GlobalState.Animation.CurrentFrame;

            new CommandAddKeyframe(gObject, AnimatableProperty.PositionX, frame, gObject.transform.localPosition.x, interpolation, updateCurve).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionY, frame, gObject.transform.localPosition.y, interpolation, updateCurve).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionZ, frame, gObject.transform.localPosition.z, interpolation, updateCurve).Submit();

            // convert to ZYX euler
            Vector3 angles = gObject.transform.localEulerAngles;
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationX, frame, angles.x, interpolation, updateCurve).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationY, frame, angles.y, interpolation, updateCurve).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationZ, frame, angles.z, interpolation, updateCurve).Submit();

            CameraController controller = gObject.GetComponent<CameraController>();
            LightController lcontroller = gObject.GetComponent<LightController>();

            if (null != controller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraFocal, frame, controller.focal, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraFocus, frame, controller.Focus, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraAperture, frame, controller.aperture, interpolation, updateCurve).Submit();
            }
            else if (null != lcontroller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.Power, frame, lcontroller.Power, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorR, frame, lcontroller.Color.r, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorG, frame, lcontroller.Color.g, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorB, frame, lcontroller.Color.b, interpolation, updateCurve).Submit();
            }
            else
            {
                // Scale
                Vector3 scale = gObject.transform.localScale;
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleX, frame, scale.x, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleY, frame, scale.y, interpolation, updateCurve).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleZ, frame, scale.z, interpolation, updateCurve).Submit();
            }

            if (obj.TryGetComponent<SkinMeshController>(out SkinMeshController skinController))
            {

                foreach (Transform child in gObject.transform)
                {
                    RecursiveAddKeyFrame(child, frame, interpolation);
                }
            }
        }

        private void RecursiveAddKeyFrame(Transform target, int frame, Interpolation interpolation)
        {
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionX, frame, target.localPosition.x, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionY, frame, target.localPosition.y, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.PositionZ, frame, target.localPosition.z, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationX, frame, target.localEulerAngles.x, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationY, frame, target.localEulerAngles.y, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.RotationZ, frame, target.localEulerAngles.z, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleX, frame, target.localScale.x, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleY, frame, target.localScale.y, interpolation, false).Submit();
            new CommandAddKeyframe(target.gameObject, AnimatableProperty.ScaleZ, frame, target.localScale.z, interpolation, false).Submit();

            foreach (Transform child in target)
            {
                RecursiveAddKeyFrame(child, frame, interpolation);
            }
        }

        public CommandAddKeyframes(GameObject obj, int frame, Vector3 position, Vector3 rotation, Vector3 scale) : base("Add Keyframes")
        {
            gObject = obj;
            Interpolation interpolation = GlobalState.Settings.interpolation;

            new CommandAddKeyframe(gObject, AnimatableProperty.PositionX, frame, position.x, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionY, frame, position.y, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionZ, frame, position.z, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationX, frame, rotation.x, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationY, frame, rotation.y, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationZ, frame, rotation.z, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.ScaleX, frame, scale.x, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.ScaleY, frame, scale.y, interpolation, false).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.ScaleZ, frame, scale.z, interpolation, false).Submit();
        }

        public CommandAddKeyframes(GameObject obj, int frame, int zoneSize, Vector3 position, Vector3 rotation, Vector3 scale) : base("Add Keyframes")
        {
            gObject = obj;
            Interpolation interpolation = GlobalState.Settings.interpolation;

            new CommandAddKeyframeZone(gObject, AnimatableProperty.RotationX, frame, rotation.x, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.RotationY, frame, rotation.y, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.RotationZ, frame, rotation.z, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.ScaleX, frame, scale.x, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.ScaleY, frame, scale.y, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.ScaleZ, frame, scale.z, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.PositionX, frame, position.x, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.PositionY, frame, position.y, zoneSize, interpolation).Submit();
            new CommandAddKeyframeZone(gObject, AnimatableProperty.PositionZ, frame, position.z, zoneSize, interpolation).Submit();
        }

        public CommandAddKeyframes(GameObject obj, int frame, int zoneSize, Vector3 position, Vector3 rotation, Vector3 scale, bool zone)
        {
            gObject = obj;
            Interpolation interpolation = GlobalState.Settings.interpolation;

            new CommandAddKeyframeSegment(gObject, AnimatableProperty.RotationX, frame, rotation.x, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.RotationY, frame, rotation.y, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.RotationZ, frame, rotation.z, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.ScaleX, frame, scale.x, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.ScaleY, frame, scale.y, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.ScaleZ, frame, scale.z, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.PositionX, frame, position.x, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.PositionY, frame, position.y, zoneSize, interpolation).Submit();
            new CommandAddKeyframeSegment(gObject, AnimatableProperty.PositionZ, frame, position.z, zoneSize, interpolation).Submit();
        }

        public CommandAddKeyframes(GameObject obj, List<GameObject> objs, int frame, int zoneSize, List<Dictionary<AnimatableProperty, List<AnimationKey>>> newKeys)
        {
            gObject = obj;
            for (int l = 0; l < objs.Count; l++)
            {
                GameObject go = objs[l];
                for (int i = 0; i < 6; i++)
                {
                    AnimatableProperty property = (AnimatableProperty)i;
                    new CommandAddKeyframeTangent(go, property, frame, zoneSize, newKeys[l][property]).Submit();
                }
            }
        }

        public CommandAddKeyframes(GameObject obj, int frame, int zoneSize, Dictionary<AnimatableProperty, List<AnimationKey>> newKeys)
        {
            gObject = obj;
            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                new CommandAddKeyframeTangent(gObject, property, frame, zoneSize, newKeys[property]).Submit();
            }
        }

        public CommandAddKeyframes(GameObject obj, int frame, int start, int end, Dictionary<AnimatableProperty, List<AnimationKey>> newKeys)
        {
            gObject = obj;
            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                new CommandAddKeyframeTangent(gObject, property, frame, start, end, newKeys[property]).Submit();
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
