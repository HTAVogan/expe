using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class SkinMeshController : ParametersController
    {

        public SkinnedMeshRenderer SkinMesh;
        public Transform RootObject;
        public BoxCollider Collider;

        public void Start()
        {
            GlobalState.Animation.onFrameEvent.AddListener(RefreshCollider);
            RefreshCollider(GlobalState.Animation.CurrentFrame);
        }

        public void RefreshCollider(int frame)
        {
            Collider.center = RootObject.localPosition + SkinMesh.localBounds.center;
            Collider.size = SkinMesh.localBounds.size;
        }

        public void OnDisable()
        {
            GlobalState.Animation.onFrameEvent.RemoveListener(RefreshCollider);
        }
    }
}