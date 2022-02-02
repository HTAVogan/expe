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

            public void ClearNode()
            {
                Childrens.ForEach(x => x.ClearNode());
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
                Maths.DecomposeMatrix(objectMatrix, out Vector3 objectPosition, out Quaternion objectRotation, out Vector3 objectScale);

                Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Sphere.transform.parent = parentNode;
                Sphere.transform.SetPositionAndRotation(objectPosition, objectRotation);
                Sphere.transform.localScale = Vector3.one * scale;
                Sphere.name = targetObject.name + "-" + frame;

                foreach (Transform child in targetObject.transform)
                {
                    Childrens.Add(new Node(child.gameObject, frame, Sphere.transform, objectMatrix));
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
        }

        public Dictionary<GameObject, Dictionary<int, Node>> ghostDictionary;
        public Transform world;
        public Transform GhostParent;

        public void Start()
        {
            ghostDictionary = new Dictionary<GameObject, Dictionary<int, Node>>();
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            if (GlobalState.Settings.display3DCurves)
                UpdateFromSelection();
        }

        void UpdateFromSelection()
        {
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
            //Debug.Log("on curve changed " + gObject);
            if (gObject.TryGetComponent<SkinMeshController>(out SkinMeshController controller))
            {
                //Debug.Log("curve humang controller " + controller);
                if (ghostDictionary.TryGetValue(controller.RootObject.gameObject, out Dictionary<int, Node> value))
                {
                    //Debug.Log("has value");
                    foreach (KeyValuePair<int, Node> pair in value)
                    {
                        pair.Value.UpdateNode(controller.transform.localToWorldMatrix);
                    }
                }
                else
                {
                    CreateGhost(controller);
                }
            }
        }

        private void CreateGhost(SkinMeshController controller)
        {
            AnimationSet rootAnim = GlobalState.Animation.GetObjectAnimation(controller.RootObject.gameObject);
            if (null == rootAnim) return;
            ghostDictionary.Add(controller.RootObject.gameObject, new Dictionary<int, Node>());
            Curve rotX = rootAnim.GetCurve(AnimatableProperty.RotationX);
            rotX.keys.ForEach(x =>
            {
                ghostDictionary[controller.RootObject.gameObject].Add(x.frame, new Node(controller.RootObject.gameObject, x.frame, GhostParent, controller.transform.localToWorldMatrix, controller.transform.localScale.magnitude * 5));
            });
        }

        private void ClearGhosts()
        {
            foreach (KeyValuePair<GameObject, Dictionary<int, Node>> pair in ghostDictionary)
            {
                foreach (KeyValuePair<int, Node> nodePairs in pair.Value)
                {
                    nodePairs.Value.ClearNode();
                }
            }
            ghostDictionary = new Dictionary<GameObject, Dictionary<int, Node>>();
            Debug.Log("clear ghost");
        }
    }

}