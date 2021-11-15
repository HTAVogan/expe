using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRtist;
using VRtist.Serialization;

public class Loader : MonoBehaviour
{
    bool alreadyLoaded = false;
    string currentProjectName = "myScene";
    string path = "";
    string pathToScene = "";
    private readonly Dictionary<string, Mesh> loadedMeshes = new Dictionary<string, Mesh>();
    public GameObject rootTransform;
    // Update is called once per frame

    private void Awake()
    {
        pathToScene = Application.persistentDataPath + "/saves/" + "myScene" + "/scene.vrtist";
        path = Application.persistentDataPath + "/saves/" + "myScene" + "/";
    }
    void Update()
    {
        if(GlobalStateTradi.Instance.isReadyToLoad && !alreadyLoaded)
        {
            alreadyLoaded = true;
           
            SceneData sceneData = new SceneData();
                SerializationManager.Load(pathToScene, sceneData);


            // Objects            
            foreach (ObjectData data in sceneData.objects)
            {
                LoadObject(data);
            }

            // Lights
     

   

            // Load animations & constraints
            AnimationEngineTradi.Instance.fps = sceneData.fps;
            AnimationEngineTradi.Instance.StartFrame = sceneData.startFrame;
            AnimationEngineTradi.Instance.EndFrame = sceneData.endFrame;

            foreach (AnimationData data in sceneData.animations)
            {
                LoadAnimation(data);
            }

            AnimationEngineTradi.Instance.CurrentFrame = sceneData.currentFrame;
            rootTransform.transform.Rotate(Vector3.right, -90);
        }  
    }

    private void LoadObject(ObjectData data)
    {
        GameObject gobject;
        string absoluteMeshPath;
        Transform importedParent = null;

        // Check for import
        if (data.isImported)
        {
            try
            {
                importedParent = new GameObject("__VRtist_tmp_load__").transform;
                absoluteMeshPath = data.meshPath;
                // Don't use async import since we may reference the game object for animations or constraints
                // and the object must be loaded before we do so
                absoluteMeshPath = absoluteMeshPath.Replace("\\", "/");
                GlobalStateTradi.GeometryImporter.ImportObject(absoluteMeshPath, importedParent, true);
                if (importedParent.childCount == 0)
                    return;
                gobject = importedParent.GetChild(0).gameObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load external object: " + e.Message);
                return;
            }
        }
        else
        {
            absoluteMeshPath = path + data.meshPath;
            gobject = new GameObject(data.name);
        }

        LoadCommonData(gobject, data);
        gobject.name = data.name;

        // Mesh
        if (null != data.meshPath && data.meshPath.Length > 0)
        {
            if (!data.isImported)
            {
                if (!loadedMeshes.TryGetValue(absoluteMeshPath, out Mesh mesh))
                {
                    MeshData meshData = new MeshData();
                    SerializationManager.Load(absoluteMeshPath, meshData);
                    mesh = meshData.CreateMesh();
                    loadedMeshes.Add(absoluteMeshPath, mesh);
                }
                gobject.AddComponent<MeshFilter>().sharedMesh = mesh;
                gobject.AddComponent<MeshRenderer>().materials = LoadMaterials(data);
                gobject.AddComponent<MeshCollider>();
            }

            if (!data.visible)
            {
                foreach (Component component in gobject.GetComponents<Component>())
                {
                    Type componentType = component.GetType();
                    var prop = componentType.GetProperty("enabled");
                    if (null != prop)
                    {
                        prop.SetValue(component, data.visible);
                    }
                }
            }

        }

        SceneManager.AddObject(gobject);

        if (data.parentPath.Length > 0)
            SceneManager.SetObjectParent(gobject, rootTransform);
        gobject.transform.SetParent(rootTransform.transform);
        if (data.isImported)
        {
            ParametersController controller = gobject.AddComponent<ParametersController>();
            controller.isImported = true;
            controller.importPath = data.meshPath;

            if (null != importedParent)
                Destroy(importedParent.gameObject);
        }
    }

    private void LoadCommonData(GameObject gobject, ObjectData data)
    {
        if (null != data.tag && data.tag.Length > 0)
        {
            gobject.tag = data.tag;
        }

        gobject.transform.localPosition = data.position;
        gobject.transform.localRotation = data.rotation;
        gobject.transform.localScale = data.scale;
        gobject.name = data.name;

        if (data.lockPosition || data.lockRotation || data.lockScale)
        {
            ParametersController controller = gobject.GetComponent<ParametersController>();
            if (null == controller)
                controller = gobject.AddComponent<ParametersController>();
            controller.lockPosition = data.lockPosition;
            controller.lockRotation = data.lockRotation;
            controller.lockScale = data.lockScale;
        }
        gobject.transform.parent = rootTransform.transform;
    }

    private Material[] LoadMaterials(ObjectData data)
    {
        Material[] materials = new Material[data.materialsData.Count];
        for (int i = 0; i < data.materialsData.Count; ++i)
        {
            materials[i] = data.materialsData[i].CreateMaterial(path);
        }
        return materials;
    }

    private void LoadAnimation(AnimationData data)
    {
        Transform animTransform = rootTransform.transform.Find(data.objectPath);
        if (null == animTransform)
        {
            Debug.LogWarning($"Object name not found for animation: {data.objectPath}");
            return;
        }
        GameObject gobject = animTransform.gameObject;

        // Create animation
        AnimationSet animSet = new AnimationSet(gobject);
        foreach (CurveData curve in data.curves)
        {
            List<AnimationKey> keys = new List<AnimationKey>();
            foreach (KeyframeData keyData in curve.keyframes)
            {
                keys.Add(new AnimationKey(keyData.frame, keyData.value, keyData.interpolation));
            }

            animSet.SetCurve(curve.property, keys);
        }
        SceneManager.SetObjectAnimations(gobject, animSet);
    }



}
