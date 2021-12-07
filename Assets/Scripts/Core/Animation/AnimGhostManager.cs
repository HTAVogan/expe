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
            public Transform Target;
            public List<Node> Childrens;
            public GameObject Sphere;
            public List<GameObject> Link;

            public void ClearNode()
            {
                Childrens.ForEach(x => x.ClearNode());
                Destroy(Sphere);
                Link.ForEach(x => Destroy(x));
            }

        }

        public List<Node> rootCharacters;

        public void Start()
        {
            rootCharacters = new List<Node>();
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            if (GlobalState.Settings.display3DCurves)
                UpdateFromSelection();
        }

        void UpdateFromSelection()
        {
            ClearGhosts();
        }

        private void ClearGhosts()
        {
            rootCharacters.ForEach(x =>
            {
                x.ClearNode();
            });
        }
    }

}