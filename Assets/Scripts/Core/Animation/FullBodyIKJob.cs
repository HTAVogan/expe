using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace VRtist
{
    public struct FullBodyIKJob : IAnimationJob
    {
        public struct EffectorHandle
        {
            public TransformSceneHandle effector;
            public float positionWeight;
            public float rotationWeight;
            public float pullWeight;
        }

        public struct HintEffectorHandle
        {
            public TransformSceneHandle hint;
            public float weight;
        }

        public struct BodyEffectorHandle
        {
            public TransformSceneHandle body;
        }

        public struct LookEffectorHandle
        {
            public TransformSceneHandle lookAt;
            public float eyesWeight;
            public float headWeight;
            public float bodyWeight;
            public float clampWeight;
        }

        public EffectorHandle leftFootEffector;
        public EffectorHandle rightFootEffector;
        public EffectorHandle leftHandEffector;
        public EffectorHandle rightHandEffector;

        public HintEffectorHandle leftKneeHintEffector;
        public HintEffectorHandle rightKneeHintEffector;
        public HintEffectorHandle leftElbowEffector;
        public HintEffectorHandle rightElbowEffector;

        public LookEffectorHandle lookAtEffector;

        public BodyEffectorHandle bodyEffector;
        public Vector3 bodyPosition;

        public struct IKLimbHandle
        {
            public TransformStreamHandle top;
            public TransformStreamHandle middle;
            public TransformStreamHandle end;
            public float maximumExtension;
        }

        public IKLimbHandle leftArm;
        public IKLimbHandle rightArm;
        public IKLimbHandle leftLeg;
        public IKLimbHandle rightLeg;

        public float stiffness;
        public int maxPullIteration;

        public PropertySceneHandle syncGoal;

        private EffectorHandle GetEffectorHandle(AvatarIKGoal goal)
        {
            switch (goal)
            {
                default:
                case AvatarIKGoal.LeftFoot: return leftFootEffector;
                case AvatarIKGoal.RightFoot: return rightFootEffector;
                case AvatarIKGoal.LeftHand: return leftHandEffector;
                case AvatarIKGoal.RightHand: return rightHandEffector;
            }
        }

        private IKLimbHandle GetIKLimbHandle(AvatarIKGoal goal)
        {
            switch (goal)
            {
                default:
                case AvatarIKGoal.LeftFoot: return leftLeg;
                case AvatarIKGoal.RightFoot: return rightLeg;
                case AvatarIKGoal.LeftHand: return leftArm;
                case AvatarIKGoal.RightHand: return rightArm;
            }
        }

        public void SetEffector(AnimationStream stream, AvatarIKGoal goal, ref EffectorHandle handle)
        {
            if (handle.effector.IsValid(stream))
            {
                AnimationHumanStream humanStream = stream.AsHuman();
                humanStream.SetGoalPosition(goal, handle.effector.GetPosition(stream));
                humanStream.SetGoalRotation(goal, handle.effector.GetRotation(stream));
                humanStream.SetGoalWeightPosition(goal, handle.positionWeight);
                humanStream.SetGoalWeightRotation(goal, handle.rotationWeight);
            }
        }

        private void SetHintEffector(AnimationStream stream, AvatarIKHint goal, ref HintEffectorHandle handle)
        {
            if (handle.hint.IsValid(stream))
            {
                AnimationHumanStream humanStream = stream.AsHuman();
                humanStream.SetHintPosition(goal, handle.hint.GetPosition(stream));
                humanStream.SetHintWeightPosition(goal, handle.weight);
            }
        }

        private void SetBodyEffector(AnimationStream stream, ref BodyEffectorHandle handle)
        {
            if (handle.body.IsValid(stream))
            {
                AnimationHumanStream humanStream = stream.AsHuman();
                humanStream.bodyRotation = handle.body.GetRotation(stream);
                humanStream.bodyPosition = handle.body.GetPosition(stream);
            }
        }

        private void SetLookAtEffector(AnimationStream stream, ref LookEffectorHandle handle)
        {
            if (handle.lookAt.IsValid(stream))
            {
                AnimationHumanStream humanStream = stream.AsHuman();
                humanStream.SetLookAtPosition(handle.lookAt.GetPosition(stream));
                humanStream.SetLookAtEyesWeight(handle.eyesWeight);
                humanStream.SetLookAtHeadWeight(handle.headWeight);
                humanStream.SetLookAtBodyWeight(handle.bodyWeight);
                humanStream.SetLookAtClampWeight(handle.clampWeight);
            }
        }

        private void SetMaximumExtension(AnimationStream stream, ref IKLimbHandle handle)
        {
            if (handle.maximumExtension == 0)
            {
                Vector3 top = handle.top.GetPosition(stream);
                Vector3 middle = handle.middle.GetPosition(stream);
                Vector3 end = handle.end.GetPosition(stream);

                Vector3 localMiddle = middle - top;
                Vector3 localEnd = end - middle;
                handle.maximumExtension = localMiddle.magnitude + localEnd.magnitude;
            }
        }

        struct LimbPart
        {
            public Vector3 localPosition;
            public Vector3 goalPosition;
            public float goalWeight;
            public float goalPullWeight;
            public float maximumExtension;
            public float stiffness;
        }

        private void PrepareSolvePull(AnimationStream stream, NativeArray<LimbPart> limbParts)
        {
            AnimationHumanStream humanStream = stream.AsHuman();
            Vector3 BodyPosition = humanStream.bodyPosition;
            for (int goalIter = 0; goalIter < 4; goalIter++)
            {
                EffectorHandle effector = GetEffectorHandle((AvatarIKGoal)goalIter);
                IKLimbHandle limbHandle = GetIKLimbHandle((AvatarIKGoal)goalIter);
                Vector3 top = limbHandle.top.GetPosition(stream);

                limbParts[goalIter] = new LimbPart
                {
                    localPosition = top - BodyPosition,
                    goalPosition = humanStream.GetGoalPosition((AvatarIKGoal)goalIter),
                    goalWeight = humanStream.GetGoalWeightPosition((AvatarIKGoal)goalIter),
                    goalPullWeight = effector.pullWeight,
                    maximumExtension = limbHandle.maximumExtension,
                    stiffness = stiffness
                };
            }
        }

        private Vector3 SolvePull(AnimationStream stream)
        {
            AnimationHumanStream humanStream = stream.AsHuman();

            Vector3 originalBodyPosition = humanStream.bodyPosition;
            Vector3 bodyPosition = originalBodyPosition;

            NativeArray<LimbPart> limbParts = new NativeArray<LimbPart>(4, Allocator.Temp);
            PrepareSolvePull(stream, limbParts);

            for (int iter = 0; iter < maxPullIteration; iter++)
            {
                Vector3 deltaPosition = Vector3.zero;
                for (int goalIter = 0; goalIter < 4; goalIter++)
                {
                    Vector3 top = bodyPosition + limbParts[goalIter].localPosition;
                    Vector3 localForce = limbParts[goalIter].goalPosition - top;
                    float restlengt = limbParts[goalIter].maximumExtension;
                    float currentLenght = localForce.magnitude;

                    localForce.Normalize();
                    float force = Mathf.Max(limbParts[goalIter].stiffness * (currentLenght - restlengt), 0.0f);

                    deltaPosition += (localForce * force * limbParts[goalIter].goalPullWeight * limbParts[goalIter].goalWeight);
                }

                deltaPosition /= (maxPullIteration - iter);
                bodyPosition += deltaPosition;
            }

            limbParts.Dispose();
            return bodyPosition - originalBodyPosition;
        }

        private void Solve(AnimationStream stream)
        {
            AnimationHumanStream humanStream = stream.AsHuman();
            bodyPosition = humanStream.bodyPosition;
            Vector3 bodyPositionDelta = SolvePull(stream);

            bodyPosition += bodyPositionDelta;
            humanStream.bodyPosition = bodyPosition;

            humanStream.SolveIK();
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            SetMaximumExtension(stream, ref leftArm);
            SetMaximumExtension(stream, ref rightArm);
            SetMaximumExtension(stream, ref leftLeg);
            SetMaximumExtension(stream, ref leftLeg);
            SetMaximumExtension(stream, ref rightLeg);
            if (!syncGoal.GetBool(stream))
            {

                SetEffector(stream, AvatarIKGoal.LeftFoot, ref leftFootEffector);
                SetEffector(stream, AvatarIKGoal.RightFoot, ref rightFootEffector);
                SetEffector(stream, AvatarIKGoal.LeftHand, ref leftHandEffector);
                SetEffector(stream, AvatarIKGoal.RightHand, ref rightHandEffector);

                SetHintEffector(stream, AvatarIKHint.LeftKnee, ref leftKneeHintEffector);
                SetHintEffector(stream, AvatarIKHint.RightKnee, ref rightKneeHintEffector);
                SetHintEffector(stream, AvatarIKHint.LeftElbow, ref leftElbowEffector);
                SetHintEffector(stream, AvatarIKHint.RightElbow, ref rightElbowEffector);

                SetLookAtEffector(stream, ref lookAtEffector);

                SetBodyEffector(stream, ref bodyEffector);
                Solve(stream);
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }

}