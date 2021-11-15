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
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
            {

                GlobalState.Animation.onFrameEvent.AddListener(RefreshCollider);
                RefreshCollider(GlobalState.Animation.CurrentFrame);
            }
            else
            {
                GlobalStateTradi.Animation.onFrameEvent.AddListener(RefreshCollider);
                RefreshCollider(GlobalStateTradi.Animation.CurrentFrame);
            }

        }

        public void RefreshCollider(int frame)
        {
            Collider.center = RootObject.localPosition + SkinMesh.localBounds.center;
            Collider.size = SkinMesh.localBounds.size;
        }

        public void OnDisable()
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                GlobalState.Animation.onFrameEvent.RemoveListener(RefreshCollider);
            else
                GlobalStateTradi.Animation.onFrameEvent.RemoveListener(RefreshCollider);

        }
    }
}