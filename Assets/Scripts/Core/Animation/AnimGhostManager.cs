using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class AnimGhostManager : MonoBehaviour
    {

        public class Node
        {
            public GameObject Target;
            public AnimationSet objectAnimation;
            public List<Node> Childrens;
            public GameObject Sphere;
            public List<GameObject> Link;
            public int Frame;

            private Vector3 worldPosition;

            public void ClearNode()
            {
                Childrens.ForEach(x => x.ClearNode());
                Childrens.Clear();
                Destroy(Sphere);
                Link.ForEach(x => Destroy(x));
                Link.Clear();
            }

            public Node(GameObject targetObject, int frame, Transform parentNode, Matrix4x4 parentMatrix, float scale = 1f)
            {
                Target = targetObject;
                Frame = frame;
                objectAnimation = GlobalState.Animation.GetObjectAnimation(Target);
                Childrens = new List<Node>();
                Link = new List<GameObject>();
                if (null == objectAnimation) return;
                Matrix4x4 objectMatrix = parentMatrix * objectAnimation.GetTranformMatrix(frame);
                Maths.DecomposeMatrix(objectMatrix, out worldPosition, out Quaternion objectRotation, out Vector3 objectScale);

                if (Target.TryGetComponent<HumanGoalController>(out HumanGoalController controller) && controller.IsGoal)
                {
                    Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                }
                else
                {
                    Sphere = new GameObject();
                }
                Sphere.transform.parent = parentNode;
                Sphere.transform.SetPositionAndRotation(worldPosition, objectRotation);
                Sphere.transform.localScale = Vector3.one * scale;
                Sphere.name = targetObject.name + "-" + frame;
                if (null != controller && !controller.name.Contains("Hips"))
                {
                    GameObject link = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    link.transform.parent = parentNode;
                    link.transform.localPosition = Sphere.transform.localPosition / 2f;
                    link.transform.up = Sphere.transform.position - parentNode.position;
                    link.transform.localScale = new Vector3(scale, Sphere.transform.localPosition.magnitude / 2f, scale);
                    Link.Add(link);
                }



                if (targetObject.transform.childCount > 3) scale = 0.5f;
                else scale = 1f;
                foreach (Transform child in targetObject.transform)
                {
                    Childrens.Add(new Node(child.gameObject, frame, Sphere.transform, objectMatrix, scale));
                }
            }

            public void UpdateNode(Matrix4x4 parentMatrix)
            {
                if (null == objectAnimation) return;
                Matrix4x4 objectMatrix = parentMatrix * objectAnimation.GetTranformMatrix(Frame);
                Maths.DecomposeMatrix(objectMatrix, out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale);
                if (null != Sphere)
                {
                    Sphere.transform.SetPositionAndRotation(objectPosition, objectRotation);
                }
                Childrens.ForEach(x => x.UpdateNode(objectMatrix));
            }

            public void SetOffset(Vector3 offset)
            {
                Sphere.transform.position = worldPosition + offset;
            }
        }

        public Dictionary<SkinMeshController, Dictionary<int, Node>> ghostDictionary;
        public Transform world;
        public Transform GhostParent;
        private bool isAnimTool;

        private bool showSkeleton;

        private float currentOffset;

        public void Start()
        {
            ghostDictionary = new Dictionary<SkinMeshController, Dictionary<int, Node>>();
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);
            GlobalState.Animation.onFrameEvent.AddListener(UpdateOffset);
            ToolsUIManager.Instance.OnToolChangedEvent += OnToolChanged;

            showSkeleton = GlobalState.Settings.DisplaySkeletons;
            currentOffset = GlobalState.Settings.CurveForwardOffset;
        }

        public void Update()
        {
            if (showSkeleton != GlobalState.Settings.DisplaySkeletons)
            {
                showSkeleton = GlobalState.Settings.DisplaySkeletons;
                if (showSkeleton)
                {
                    UpdateFromSelection();
                }
                else
                {
                    ClearGhosts();
                }
            }

            if (currentOffset != GlobalState.Settings.CurveForwardOffset)
            {
                currentOffset = GlobalState.Settings.CurveForwardOffset;
                UpdateOffset(GlobalState.Animation.CurrentFrame);
            }
        }

        private void OnToolChanged(object sender, ToolChangedArgs args)
        {
            bool switchToAnim = args.toolName == "Animation";
            if (switchToAnim && !isAnimTool) UpdateFromSelection();
            if (!switchToAnim && isAnimTool) ClearGhosts();
            isAnimTool = switchToAnim;
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            UpdateFromSelection();
        }

        void UpdateFromSelection()
        {
            if (ToolsManager.CurrentToolName() != "Animation" || !showSkeleton) return;
            ClearGhosts();
            foreach (GameObject gObject in Selection.SelectedObjects)
            {
                if (gObject.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
                {
                    CreateGhost(controller);
                }
            }
        }

        public void OnCurveChanged(GameObject gObject, AnimatableProperty property)
        {
            if (ToolsManager.CurrentToolName() != "Animation" || !showSkeleton) return;
            //Debug.Log("on curve changed " + gObject);
            if (gObject.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
            {
                //Debug.Log("curve humang controller " + controller);
                if (ghostDictionary.TryGetValue(controller, out Dictionary<int, Node> value))
                {
                    //Debug.Log("has value");
                    foreach (KeyValuePair<int, Node> pair in value)
                    {
                        pair.Value.ClearNode();
                    }
                }
                CreateGhost(controller);
            }
        }

        public void OnAnimationRemoved(GameObject gobject)
        {
            if (gobject.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
            {
                if (ghostDictionary.TryGetValue(controller, out Dictionary<int, Node> value))
                {
                    foreach (KeyValuePair<int, Node> pair in value)
                    {
                        pair.Value.ClearNode();
                    }
                    ghostDictionary.Remove(controller);
                }
            }
        }

        private void CreateGhost(SkinMeshController controller)
        {
            AnimationSet HipsAnim = GlobalState.Animation.GetObjectAnimation(controller.RootObject.gameObject);
            if (null == HipsAnim) return;

            AnimationSet rootAnim = GlobalState.Animation.GetObjectAnimation(controller.gameObject);
            ghostDictionary[controller] = new Dictionary<int, Node>();
            Curve rotX = HipsAnim.GetCurve(AnimatableProperty.RotationX);
            rotX.keys.ForEach(x =>
            {
                Matrix4x4 rootMatrix = rootAnim != null ? rootAnim.GetTranformMatrix(x.frame) : Matrix4x4.TRS(controller.transform.localPosition, controller.transform.localRotation, controller.transform.localScale); ;
                ghostDictionary[controller].Add(x.frame,
                    new Node(controller.RootObject.gameObject, x.frame, GhostParent, controller.transform.parent.localToWorldMatrix * rootMatrix, controller.transform.localScale.magnitude * 5));
            });
            UpdateOffset(GlobalState.Animation.CurrentFrame);
        }

        public void UpdateOffset(int currentFrame)
        {
            foreach (KeyValuePair<SkinMeshController, Dictionary<int, Node>> pair in ghostDictionary)
            {
                Vector3 forwardVector = (pair.Key.transform.forward * pair.Key.transform.localScale.x) * currentOffset;
                foreach (KeyValuePair<int, Node> node in pair.Value)
                {
                    int offsetSize = currentFrame - node.Key;
                    node.Value.SetOffset(forwardVector * offsetSize);
                }
            }
        }

        private void ClearGhosts()
        {
            foreach (KeyValuePair<SkinMeshController, Dictionary<int, Node>> pair in ghostDictionary)
            {
                foreach (KeyValuePair<int, Node> nodePairs in pair.Value)
                {
                    nodePairs.Value.ClearNode();
                }
            }
            ghostDictionary.Clear();
        }
    }

}