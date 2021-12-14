using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/RigConfiguration", fileName = "RigConfig")]
    public class RigConfiguration : ScriptableObject
    {
        [System.Serializable]
        public class Joint
        {
            public string Name;
            public float stiffness;
            public bool isGoal;
            public bool showCurve;
        }

        public List<Joint> JointsList;


        public void GenerateGoalController(SkinMeshController rootController, Transform transform, List<Transform> path)
        {
            Joint joint = JointsList.Find(x => x.Name == transform.name);
            if (null != joint)
            {
                HumanGoalController controller = transform.gameObject.AddComponent<HumanGoalController>();
                SphereCollider collider = transform.gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                controller.SetPathToRoot(rootController, path);
                controller.stiffness = joint.stiffness;
                controller.IsGoal = joint.isGoal;
                controller.ShowCurve = joint.showCurve;
            }
            path.Add(transform);
            foreach (Transform child in transform)
            {
                GenerateGoalController(rootController, child, new List<Transform>(path));
            }
        }

    }
}
