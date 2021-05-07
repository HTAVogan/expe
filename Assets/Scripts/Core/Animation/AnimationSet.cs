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
using UnityEngine.Animations;

namespace VRtist
{
    /// <summary>
    /// A set of animations for a given Transform. An animation is a curve on specific properties (location, rotation...).
    /// </summary>
    public class AnimationSet
    {
        public Transform transform;
        public readonly Dictionary<AnimatableProperty, Curve> curves = new Dictionary<AnimatableProperty, Curve>();
        public readonly List<AnimationSet> constraintedAnimations = new List<AnimationSet>();
        public AnimationSet parentConstraint;

        public AnimationSet(GameObject gobject)
        {
            transform = gobject.transform;
            LightController lightController = gobject.GetComponent<LightController>();
            CameraController cameraController = gobject.GetComponent<CameraController>();
            if (null != lightController) { CreateLightCurves(); }
            else if (null != cameraController) { CreateCameraCurves(); }
            else { CreateTransformCurves(); }
        }

        public void EvaluateAnimation(int currentFrame)
        {
            Transform trans = transform;
            Vector3 position = trans.localPosition;
            Vector3 rotation = trans.localEulerAngles;
            Vector3 scale = trans.localScale;

            float power = -1;
            Color color = Color.white;

            float cameraFocal = -1;
            float cameraFocus = -1;
            float cameraAperture = -1;

            foreach (Curve curve in curves.Values)
            {
                if (!curve.Evaluate(currentFrame, out float value))
                    continue;
                switch (curve.property)
                {
                    case AnimatableProperty.PositionX: position.x = value; break;
                    case AnimatableProperty.PositionY: position.y = value; break;
                    case AnimatableProperty.PositionZ: position.z = value; break;

                    case AnimatableProperty.RotationX: rotation.x = value; break;
                    case AnimatableProperty.RotationY: rotation.y = value; break;
                    case AnimatableProperty.RotationZ: rotation.z = value; break;

                    case AnimatableProperty.ScaleX: scale.x = value; break;
                    case AnimatableProperty.ScaleY: scale.y = value; break;
                    case AnimatableProperty.ScaleZ: scale.z = value; break;

                    case AnimatableProperty.Power: power = value; break;
                    case AnimatableProperty.ColorR: color.r = value; break;
                    case AnimatableProperty.ColorG: color.g = value; break;
                    case AnimatableProperty.ColorB: color.b = value; break;

                    case AnimatableProperty.CameraFocal: cameraFocal = value; break;
                    case AnimatableProperty.CameraFocus: cameraFocus = value; break;
                    case AnimatableProperty.CameraAperture: cameraAperture = value; break;
                }
            }

            trans.localPosition = position;
            trans.localEulerAngles = rotation;
            trans.localScale = scale;

            if (power != -1)
            {
                LightController controller = trans.GetComponent<LightController>();
                controller.Power = power;
                controller.Color = color;
            }

            if (cameraFocal != -1 || cameraFocus != -1 || cameraAperture != -1)
            {
                CameraController controller = trans.GetComponent<CameraController>();
                if (cameraFocal != -1)
                    controller.focal = cameraFocal;
                if (cameraFocus != -1)
                    controller.Focus = cameraFocus;
                if (cameraAperture != -1)
                    controller.aperture = cameraAperture;
            }

            if (null != parentConstraint)
            {
                ParentConstraint constraint = transform.GetComponent<ParentConstraint>();
                Vector3 offset = Vector3.Scale(parentConstraint.transform.InverseTransformPoint(transform.position), parentConstraint.transform.lossyScale);
                constraint.SetTranslationOffset(0, offset);
            }
        }

        public Curve GetCurve(AnimatableProperty property)
        {
            curves.TryGetValue(property, out Curve result);
            return result;
        }

        public void SetCurve(AnimatableProperty property, List<AnimationKey> keys)
        {
            if (!curves.TryGetValue(property, out Curve curve))
            {
                Debug.LogError("Curve not found : " + transform.name + " " + property.ToString());
                return;
            }
            curve.SetKeys(keys);
        }

        private void CreatePositionRotationCurves()
        {
            curves.Add(AnimatableProperty.PositionX, new Curve(AnimatableProperty.PositionX));
            curves.Add(AnimatableProperty.PositionY, new Curve(AnimatableProperty.PositionY));
            curves.Add(AnimatableProperty.PositionZ, new Curve(AnimatableProperty.PositionZ));

            curves.Add(AnimatableProperty.RotationX, new Curve(AnimatableProperty.RotationX));
            curves.Add(AnimatableProperty.RotationY, new Curve(AnimatableProperty.RotationY));
            curves.Add(AnimatableProperty.RotationZ, new Curve(AnimatableProperty.RotationZ));
        }

        private void CreateTransformCurves()
        {
            CreatePositionRotationCurves();
            curves.Add(AnimatableProperty.ScaleX, new Curve(AnimatableProperty.ScaleX));
            curves.Add(AnimatableProperty.ScaleY, new Curve(AnimatableProperty.ScaleY));
            curves.Add(AnimatableProperty.ScaleZ, new Curve(AnimatableProperty.ScaleZ));
        }

        private void CreateLightCurves()
        {
            CreatePositionRotationCurves();
            curves.Add(AnimatableProperty.Power, new Curve(AnimatableProperty.Power));
            curves.Add(AnimatableProperty.ColorR, new Curve(AnimatableProperty.ColorR));
            curves.Add(AnimatableProperty.ColorG, new Curve(AnimatableProperty.ColorG));
            curves.Add(AnimatableProperty.ColorB, new Curve(AnimatableProperty.ColorB));
        }

        private void CreateCameraCurves()
        {
            CreatePositionRotationCurves();
            curves.Add(AnimatableProperty.CameraFocal, new Curve(AnimatableProperty.CameraFocal));
            curves.Add(AnimatableProperty.CameraFocus, new Curve(AnimatableProperty.CameraFocus));
            curves.Add(AnimatableProperty.CameraAperture, new Curve(AnimatableProperty.CameraAperture));
        }

        public void ComputeCache()
        {
            foreach (Curve curve in curves.Values)
                curve.ComputeCache();

            constraintedAnimations.ForEach(x => x.ComputeRestrictedCache());
        }

        public void ComputeRestrictedCache()
        {
            foreach (Curve curve in curves.Values) curve.ComputeCache();

            if (null != parentConstraint)
            {
                float[] posXCurve = GetCurve(AnimatableProperty.PositionX).CachedValues;
                float[] posYCurve = GetCurve(AnimatableProperty.PositionY).CachedValues;
                float[] posZCurve = GetCurve(AnimatableProperty.PositionZ).CachedValues;

                float[] parentPosXCurve = parentConstraint.GetCurve(AnimatableProperty.PositionX).CachedValues;
                float[] parentPosYCurve = parentConstraint.GetCurve(AnimatableProperty.PositionY).CachedValues;
                float[] parentPosZcurve = parentConstraint.GetCurve(AnimatableProperty.PositionZ).CachedValues;

                for (int i = 1; i < posXCurve.Length; i++)
                {
                    posXCurve[i] += parentPosXCurve[i] - parentPosXCurve[0];
                    posYCurve[i] += parentPosYCurve[i] - parentPosYCurve[0];
                    posZCurve[i] += parentPosZcurve[i] - parentPosZcurve[0];
                }
            }
        }

        public void ClearCache()
        {
            foreach (Curve curve in curves.Values)
                curve.ClearCache();
        }




    }
}
