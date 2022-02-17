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
            public Vector3 LowerAngleBound;
            public Vector3 UpperAngleBound;
            public float weight;
        }

        public List<Joint> JointsList;


        public void GenerateGoalController(SkinMeshController rootController, Transform transform, List<Transform> path)
        {
            string boneName = transform.name;
            if (boneName.Contains("mixamorig:")) boneName = boneName.Split(':')[1];
            Joint joint = JointsList.Find(x => x.Name == boneName);
            if (null != joint)
            {
                HumanGoalController controller = transform.gameObject.AddComponent<HumanGoalController>();
                SphereCollider collider = transform.gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                controller.SetPathToRoot(rootController, path);
                controller.stiffness = joint.stiffness;
                controller.weight = joint.weight;   
                controller.IsGoal = joint.isGoal;
                controller.ShowCurve = joint.showCurve;
                controller.LowerAngleBound = joint.LowerAngleBound;
                controller.UpperAngleBound = joint.UpperAngleBound;
            }
            path.Add(transform);
            foreach (Transform child in transform)
            {
                GenerateGoalController(rootController, child, new List<Transform>(path));
            }
        }

    }
}
