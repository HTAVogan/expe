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
        public bool toSetup = false;
        private string baseName = "";

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

        //private GameObject headEffector;
        private GameObject bodyEffector;

        public void Start()
        {
            if (animator == null) animator = gameObject.AddComponent<Animator>();
        }

        public void Update()
        {
            if (toSetup && animator.isInitialized)
            {
                GenerateAvatar();
                CreateEffectors();
                SetupIK();
                CreateGraph();
                toSetup = false;
                Debug.Log("is init");
            }
            if (graph.IsValid())
            {
                graph.Evaluate();
            }
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
            Transform target = animator.GetBoneTransform(bone);

            newEffector.tag = "PhysicObject";
            newEffector.name = bone.ToString() + "_Actuator";
            newEffector.transform.parent = transform;
            newEffector.transform.localPosition = transform.InverseTransformPoint(target.transform.position);
            newEffector.transform.rotation = target.transform.rotation;
            newEffector.transform.localScale = Vector3.one;
            SphereCollider effectorCollider = newEffector.AddComponent<SphereCollider>();
            effectorCollider.radius = 1;
            return newEffector;
        }

        //private GameObject AddHeadEffector()
        //{
        //    GameObject newEffector = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    Transform target = animator.GetBoneTransform(HumanBodyBones.Head);

        //    newEffector.tag = "PhysicObject";
        //    newEffector.name = "LookAt_Actuator";
        //    newEffector.transform.parent = transform;
        //    newEffector.transform.localPosition = transform.InverseTransformPoint(target.transform.forward * 0.1f);
        //    newEffector.transform.rotation = target.transform.rotation;
        //    newEffector.transform.localScale = Vector3.one;
        //    SphereCollider effectorCollider = newEffector.AddComponent<SphereCollider>();
        //    effectorCollider.radius = 1;
        //    return newEffector;
        //}

        internal void Setup()
        {
            //toSetup = true;
        }

        private void GenerateAvatar()
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Hips"))
                {
                    humanRoot = child;
                    baseName = humanRoot.name.Split(':')[0];
                }
            }
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

            //SetupLookAtEffector(ref job.lookAtEffector, headEffector);
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

            //headEffector = AddBoneEffector(HumanBodyBones.Head);
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
            boneNames["Hips"] = baseName + ":Hips";
            boneNames["Spine"] = baseName + ":Spine";
            boneNames["Chest"] = baseName + ":Spine1";
            boneNames["Head"] = baseName + ":Head";
            boneNames["LeftFoot"] = baseName + ":LeftFoot";
            boneNames["LeftLowerLeg"] = baseName + ":LeftLeg";
            boneNames["LeftUpperLeg"] = baseName + ":LeftUpLeg";
            boneNames["LeftHand"] = baseName + ":LeftHand";
            boneNames["LeftLowerArm"] = baseName + ":LeftForeArm";
            boneNames["LeftUpperArm"] = baseName + ":LeftArm";
            boneNames["LeftShoulder"] = baseName + ":LeftShoulder";
            boneNames["RightFoot"] = baseName + ":RightFoot";
            boneNames["RightLowerLeg"] = baseName + ":RightLeg";
            boneNames["RightUpperLeg"] = baseName + ":RightUpLeg";
            boneNames["RightHand"] = baseName + ":RightHand";
            boneNames["RightLowerArm"] = baseName + ":RightForeArm";
            boneNames["RightUpperArm"] = baseName + ":RightArm";
            boneNames["RightShoulder"] = baseName + ":RightShoulder";
        }

        private void GetBoneTransforms(Transform root, List<string> boneName, Dictionary<string, Transform> dictionary)
        {
            if (boneName.Contains(root.name)) dictionary[root.name] = root;
            foreach (Transform child in root)
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

            thisBone.rotation = Quaternion.identity;

            thisBone.scale = boneTransform.localScale;
            return thisBone;
        }

        private void AddMixamoBones(List<SkeletonBone> skeleton)
        {
            List<string> names = new List<string>()
            {
                this.name, baseName+":Hips", baseName+":LeftUpLeg", baseName+":LeftLeg",baseName+":LeftFoot",baseName+":LeftToeBase",baseName+":RightUpLeg",baseName+":RightLeg",
                baseName+":RightFoot",baseName+":RightToeBase",baseName+":Spine",baseName+":Spine1",baseName+":Spine2",baseName+":LeftShoulder",baseName+":LeftArm",baseName+":LeftForeArm",
                baseName+":LeftHand",baseName+":Neck",baseName+":Head",baseName+":RightShoulder",baseName+":RightArm",baseName+":RightForeArm",baseName+":RightHand"
            };
            Dictionary<string, Transform> dictionary = new Dictionary<string, Transform>();
            GetBoneTransforms(transform, names, dictionary);
            AddMixamoSkeletonBones(skeleton, dictionary);
            //names.ForEach(x =>
            //{
            //    skeleton.Add(GetBone(humanRoot, x, dictionary));
            //});
        }

        public Vector3 GetBonePosition(string name, Dictionary<string, Transform> dictionary)
        {
            Debug.Log(name + " / " + dictionary[name].localPosition);
            return dictionary[name].localPosition;
        }

        public Vector3 GetBoneScale(string name, Dictionary<string, Transform> dictionary)
        {
            return dictionary[name].localScale;
        }

        private void AddMixamoSkeletonBones(List<SkeletonBone> skeleton, Dictionary<string, Transform> dictionary)
        {
            skeleton.Add(new SkeletonBone()
            {
                name = gameObject.name,
                position = GetBonePosition(gameObject.name, dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale(gameObject.name, dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Hips",
                position = GetBonePosition("mixamorig:Hips", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:Hips", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftUpLeg",
                position = GetBonePosition("mixamorig:LeftUpLeg", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftUpLeg", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftLeg",
                position = GetBonePosition("mixamorig:LeftLeg", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftLeg", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftFoot",
                position = GetBonePosition("mixamorig:LeftFoot", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftFoot", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftToeBase",
                position = GetBonePosition("mixamorig:LeftToeBase", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftToeBase", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightUpLeg",
                position = GetBonePosition("mixamorig:RightUpLeg", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightUpLeg", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightLeg",
                position = GetBonePosition("mixamorig:RightLeg", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightLeg", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightFoot",
                position = GetBonePosition("mixamorig:RightFoot", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightFoot", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightToeBase",
                position = GetBonePosition("mixamorig:RightToeBase", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightToeBase", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Spine",
                position = GetBonePosition("mixamorig:Spine", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:Spine", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Spine1",
                position = GetBonePosition("mixamorig:Spine1", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:Spine1", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Spine2",
                position = GetBonePosition("mixamorig:Spine2", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:Spine2", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftShoulder",
                position = GetBonePosition("mixamorig:LeftShoulder", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftShoulder", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftArm",
                position = GetBonePosition("mixamorig:LeftArm", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftArm", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftForeArm",
                position = GetBonePosition("mixamorig:LeftForeArm", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftForeArm", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:LeftHand",
                position = GetBonePosition("mixamorig:LeftHand", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:LeftHand", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Neck",
                position = GetBonePosition("mixamorig:Neck", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:Neck", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:Head",
                position = GetBonePosition("mixamorig:Head", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:Head", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightShoulder",
                position = GetBonePosition("mixamorig:RightShoulder", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightShoulder", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightArm",
                position = GetBonePosition("mixamorig:RightArm", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightArm", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightForeArm",
                position = GetBonePosition("mixamorig:RightForeArm", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightForeArm", dictionary)
            });
            skeleton.Add(new SkeletonBone()
            {
                name = "mixamorig:RightHand",
                position = GetBonePosition("mixamorig:RightHand", dictionary),
                rotation = Quaternion.identity,
                scale = GetBoneScale("mixamorig:RightHand", dictionary)
            });
        }
    }

}