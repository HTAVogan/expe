using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VRtist
{
    public class HumanIk : MonoBehaviour
    {
        public bool syncGoals = false;

        private Animator animator;
        private Transform humanRoot;
        private bool toSetup = false;

        private FullBodyIKJob job;
        private PlayableGraph graph;
        private AnimationScriptPlayable playable;

        private GameObject leftHandEffector;
        private GameObject rightHandEffector;
        private GameObject leftFootEffector;
        private GameObject rightFootEffector;

        private GameObject leftKneeEffector;
        private GameObject rightKneeEffector;
        private GameObject leftElbowEffector;
        private GameObject rightElbowEffector;

        private GameObject headEffector;
        private GameObject bodyEffector;

        public void Start()
        {
            if (animator == null) animator = gameObject.AddComponent<Animator>();
        }

        public void Update()
        {
            //if (toSetup && animator.isInitialized)
            //{
            //    GenerateAvatar();
            //    CreateEffectors();
            //    SetupIK();
            //    CreateGraph();
            //    toSetup = false;
            //    Debug.Log("is init");
            //}
            //if (graph.IsValid())
            //{
            //    graph.Evaluate();
            //}
        }

        private void CreateGraph()
        {
            if (graph.IsValid()) graph.Destroy();
            graph = PlayableGraph.Create(gameObject.name);
            //AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, new AnimationClip());
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            playable = AnimationScriptPlayable.Create(graph, job, 1);
            //playable.ConnectInput(0, clipPlayable, 0, 1);
            output.SetSourcePlayable(playable);
        }


        private GameObject AddBoneEffector(HumanBodyBones bone)
        {
            GameObject newEffector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newEffector.tag = "PhysicObject";
            newEffector.name = bone.ToString() + "Actuator";
            Transform target = animator.GetBoneTransform(bone);
            newEffector.transform.position = target.transform.position;
            newEffector.transform.rotation = target.transform.rotation;
            newEffector.transform.parent = transform;
            SphereCollider effectorCollider = newEffector.AddComponent<SphereCollider>();
            effectorCollider.radius = 1;
            return newEffector;
        }

        internal void Setup()
        {
            toSetup = true;
        }

        private void GenerateAvatar()
        {
            humanRoot = transform.Find("mixamorig:Hips");
            Dictionary<string, string> boneNames = new Dictionary<string, string>();
            if (humanRoot.name.Contains("mixamorig")) UseMixamoBoneNames(boneNames);

            string[] humanName = HumanTrait.BoneName;
            HumanBone[] humanBones = new HumanBone[boneNames.Count];
            CreateHumanBones(boneNames, humanName, humanBones);

            List<SkeletonBone> skeleton = new List<SkeletonBone>();
            if (humanRoot.name.Contains("mixamorig")) AddMixamoBones(skeleton);

            HumanDescription description = new HumanDescription()
            {
                armStretch = 0.05f,
                feetSpacing = 0.05f,
                hasTranslationDoF = false,
                legStretch = 0.05f,
                lowerArmTwist = 0.5f,
                lowerLegTwist = 0.5f,
                upperArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                human = humanBones,
                skeleton = skeleton.ToArray()
            };
            Avatar newAvatar = AvatarBuilder.BuildHumanAvatar(gameObject, description);
            newAvatar.name = gameObject.name;
            if (newAvatar.isValid)
            {
                animator.avatar = newAvatar;
                animator.Rebind();
            }
            else
            {
                Debug.Log("Invalid avatar");
            }
        }

        private void CreateHumanBones(Dictionary<string, string> boneNames, string[] humanName, HumanBone[] humanBones)
        {
            int j = 0;
            int i = 0;
            while (i < humanName.Length)
            {
                if (boneNames.ContainsKey(humanName[i]))
                {
                    HumanBone humanBone = new HumanBone();
                    humanBone.humanName = humanName[i];
                    humanBone.boneName = boneNames[humanName[i]];
                    humanBone.limit.useDefaultValues = true;
                    humanBones[j++] = humanBone;
                }
                i++;
            }
        }

        private void SetupIK()
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            job = new FullBodyIKJob();
            job.stiffness = 1;
            job.maxPullIteration = 10;

            job.syncGoal = animator.BindSceneProperty(transform, typeof(HumanIk), "syncGoals");

            SetupIKLimbHandle(ref job.leftArm, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
            SetupIKLimbHandle(ref job.rightArm, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
            SetupIKLimbHandle(ref job.leftLeg, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            SetupIKLimbHandle(ref job.rightLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);

            SetupEffector(ref job.rightFootEffector, rightFootEffector);
            SetupEffector(ref job.leftFootEffector, leftFootEffector);
            SetupEffector(ref job.rightHandEffector, rightHandEffector);
            SetupEffector(ref job.leftHandEffector, leftHandEffector);

            SetupHintEffector(ref job.leftElbowEffector, leftElbowEffector);
            SetupHintEffector(ref job.rightElbowEffector, rightElbowEffector);
            SetupHintEffector(ref job.leftKneeHintEffector, leftKneeEffector);
            SetupHintEffector(ref job.rightKneeHintEffector, rightKneeEffector);

            SetupLookAtEffector(ref job.lookAtEffector, headEffector);
            SetupBodyEffector(ref job.bodyEffector, bodyEffector);
        }

        private void CreateEffectors()
        {
            gameObject.tag = "PhysicObject";
            BoxCollider baseCollider = gameObject.AddComponent<BoxCollider>();
            baseCollider.center = new Vector3(0, -20, 0);
            baseCollider.size = new Vector3(100, 20, 100);

            leftHandEffector = AddBoneEffector(HumanBodyBones.LeftHand);
            rightHandEffector = AddBoneEffector(HumanBodyBones.RightHand);
            leftFootEffector = AddBoneEffector(HumanBodyBones.LeftFoot);
            rightFootEffector = AddBoneEffector(HumanBodyBones.RightFoot);

            leftKneeEffector = AddBoneEffector(HumanBodyBones.LeftLowerLeg);
            rightKneeEffector = AddBoneEffector(HumanBodyBones.RightLowerLeg);
            leftElbowEffector = AddBoneEffector(HumanBodyBones.LeftLowerArm);
            rightElbowEffector = AddBoneEffector(HumanBodyBones.RightLowerArm);

            headEffector = AddBoneEffector(HumanBodyBones.Head);
            bodyEffector = AddBoneEffector(HumanBodyBones.Hips);

        }
        private void SetupEffector(ref FullBodyIKJob.EffectorHandle handle, GameObject effector)
        {
            handle.effector = animator.BindSceneTransform(effector.transform);
            handle.positionWeight = 1;
            handle.rotationWeight = 1;
            handle.pullWeight = 1;
        }

        private void SetupIKLimbHandle(ref FullBodyIKJob.IKLimbHandle handle, HumanBodyBones top, HumanBodyBones middle, HumanBodyBones end)
        {
            Debug.Log(animator.GetBoneTransform(top) + " " + animator.GetBoneTransform(middle) + " " + animator.GetBoneTransform(end));
            handle.top = animator.BindStreamTransform(animator.GetBoneTransform(top));
            handle.middle = animator.BindStreamTransform(animator.GetBoneTransform(middle));
            handle.end = animator.BindStreamTransform(animator.GetBoneTransform(end));
        }

        private void SetupHintEffector(ref FullBodyIKJob.HintEffectorHandle handle, GameObject effector)
        {
            handle.hint = animator.BindSceneTransform(effector.transform);
            handle.weight = 1;
        }

        private void SetupLookAtEffector(ref FullBodyIKJob.LookEffectorHandle handle, GameObject effector)
        {

            handle.lookAt = animator.BindSceneTransform(effector.transform);
            handle.eyesWeight = 1;
            handle.headWeight = 1;
            handle.bodyWeight = 1;
            handle.clampWeight = 1;
        }
        private void SetupBodyEffector(ref FullBodyIKJob.BodyEffectorHandle handle, GameObject effector)
        {

            handle.body = animator.BindSceneTransform(effector.transform);
        }

        private void UseMixamoBoneNames(Dictionary<string, string> boneNames)
        {
            boneNames["Hips"] = "mixamorig:Hips";
            boneNames["Spine"] = "mixamorig:Spine";
            boneNames["Chest"] = "mixamorig:Spine1";
            boneNames["Head"] = "mixamorig:Head";
            boneNames["LeftFoot"] = "mixamorig:LeftFoot";
            boneNames["LeftLowerLeg"] = "mixamorig:LeftLeg";
            boneNames["LeftUpperLeg"] = "mixamorig:LeftUpLeg";
            boneNames["LeftHand"] = "mixamorig:LeftHand";
            boneNames["LeftLowerArm"] = "mixamorig:LeftForeArm";
            boneNames["LeftUpperArm"] = "mixamorig:LeftArm";
            boneNames["LeftShoulder"] = "mixamorig:LeftShoulder";
            boneNames["RightFoot"] = "mixamorig:RightFoot";
            boneNames["RightLowerLeg"] = "mixamorig:RightLeg";
            boneNames["RightUpperLeg"] = "mixamorig:RightUpLeg";
            boneNames["RightHand"] = "mixamorig:RightHand";
            boneNames["RightLowerArm"] = "mixamorig:RightForeArm";
            boneNames["RightUpperArm"] = "mixamorig:RightArm";
            boneNames["RightShoulder"] = "mixamorig:RightShoulder";
        }

        private void GetBoneTransforms(Transform root, List<string> boneName, Dictionary<string, Transform> dictionary)
        {
            if (boneName.Contains(root.name)) dictionary[root.name] = root;
            foreach(Transform child in root)
            {
                GetBoneTransforms(child, boneName, dictionary);
            }
        }

        private SkeletonBone GetBone(Transform root, string name, Dictionary<string, Transform> dict)
        {
            SkeletonBone thisBone = new SkeletonBone();
            thisBone.name = name;
            Debug.Assert(dict.ContainsKey(name) && dict[name] != null, " Bone does not exist : " + name);
            Transform boneTransform = dict[name];
            thisBone.position = boneTransform.localPosition;// root.InverseTransformPoint(boneTransform.position);
            if (name.Contains("RightHand"))
            {
                thisBone.rotation = Quaternion.Euler(0, 90, 0);
            }
            else if (name.Contains("LeftHand"))
            {
                thisBone.rotation = Quaternion.Euler(0, -90, 0);
            }
            else
            {
                thisBone.rotation = Quaternion.identity;
            }
            thisBone.scale = boneTransform.localScale;
            return thisBone;
        }

        private void AddMixamoBones(List<SkeletonBone> skeleton)
        {
            List<string> names = new List<string>()
            {
                this.name, "mixamorig:Hips", "mixamorig:LeftUpLeg", "mixamorig:LeftLeg","mixamorig:LeftFoot","mixamorig:LeftToeBase","mixamorig:RightUpLeg","mixamorig:RightLeg",
                "mixamorig:RightFoot","mixamorig:RightToeBase","mixamorig:Spine","mixamorig:Spine1","mixamorig:Spine2","mixamorig:LeftShoulder","mixamorig:LeftArm","mixamorig:LeftForeArm",
                "mixamorig:LeftHand","mixamorig:Neck","mixamorig:Head","mixamorig:RightShoulder","mixamorig:RightArm","mixamorig:RightForeArm","mixamorig:RightHand"
            };
            Dictionary<string, Transform> dictionary = new Dictionary<string, Transform>();
            GetBoneTransforms(transform, names, dictionary);
            names.ForEach(x =>
            {
                skeleton.Add(GetBone(dictionary["mixamorig:Hips"], x, dictionary));
            });
        }

        private void AddMixamoSkeletonBones(List<SkeletonBone> skeleton)
        {
            skeleton.Add(new SkeletonBone()
            {
                name = gameObject.name,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Hips",
                position = new Vector3(0.00348425494f, 1.05444169f, 0.0209799856f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftUpLeg",
                position = new Vector3(-0.0820778161f, -0.067517139f, -0.0159955565f),
                rotation = new Quaternion(2.28283135e-08f, 2.18278746e-08f, 4.5702091e-09f, 1),
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftLeg",
                position = new Vector3(4.11015799e-09f, -0.443704724f, 0.00284642633f),
                rotation = new Quaternion(7.10542736e-15f, -7.27595761e-09f, -7.27595673e-09f, 1),
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftFoot",
                position = new Vector3(-4.7163935e-09f, -0.444278717f, -0.0298219062f),
                rotation = new Quaternion(-2.32830679e-08f, 8.73114914e-09f, -1.03397577e-23f, 1),
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftToeBase",
                position = new Vector3(2.9609879e-08f, -0.0872866958f, 0.107105598f),
                rotation = new Quaternion(-2.32830644e-08f, -5.82076609e-09f, -5.82076742e-09f, 1),
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightUpLeg",
                position = new Vector3(0.0820779577f, -0.0675166175f, -0.0159955937f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightLeg",
                position = new Vector3(-8.43557224e-10f, -0.44370535f, 0.00286156381f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightFoot",
                position = new Vector3(-9.64458202e-09f, -0.444277287f, -0.0298378896f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightToeBase",
                position = new Vector3(2.36043807e-08f, -0.0872866884f, 0.107105605f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Spine",
                position = new Vector3(-9.23415229e-08f, 0.101815879f, 0.0013152092f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Spine1",
                position = new Vector3(-2.51940291e-09f, 0.100834511f, -0.0100080427f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Spine2",
                position = new Vector3(-3.4574863e-09f, 0.0910001099f, -0.0137341712f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftShoulder",
                position = new Vector3(-0.0457044654f, 0.109459847f, -0.0262798797f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftArm",
                position = new Vector3(-0.105923697f, -0.005245829f, -0.0223212f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftForeArm",
                position = new Vector3(-0.278415203f, -8.94286472e-07f, 3.74589092e-07f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftHand",
                position = new Vector3(-0.283288389f, -1.74407177e-07f, 3.78045229e-07f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Neck",
                position = new Vector3(-6.33428554e-09f, 0.16671668f, -0.025161678f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Head",
                position = new Vector3(4.2423185e-09f, 0.0961787477f, 0.0168500748f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightShoulder",
                position = new Vector3(0.045699697f, 0.109461762f, -0.026280174f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightArm",
                position = new Vector3(0.105928436f, -0.00524798362f, -0.0223209858f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightForeArm",
                position = new Vector3(0.278415203f, -3.30792176e-07f, 1.16763104e-07f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightHand",
                position = new Vector3(0.283288389f, -1.58141711e-09f, 5.58160139e-07f),
                rotation = Quaternion.identity,
                scale = Vector3.one
            });
        }
    }

}