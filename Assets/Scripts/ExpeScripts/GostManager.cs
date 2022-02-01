using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRtist;

public class GostManager : MonoBehaviour
{
    private GameObject gostJoleen;
    private GameObject gostAbe;
    private GameObject bottle;
    public Material gostMaterial;

    private GameObject joleen;
    private GameObject abe;
    private GameObject bottleInit;
    [Range(1f, 100f)]
    public float delta;

    public AnimationClip clipBottle;
    public RuntimeAnimatorController controllerGostBottle;
    public Dictionary<string, float> percents;
    public Dictionary<string, List<float>> abePerBones;
    public Dictionary<string, List<float>> joleenPerBones;
    public string path = "Assets/Resources/resultsPerBone.txt";
    private Dictionary<GameObject, GameObject> gostAndOrigin = new Dictionary<GameObject, GameObject>();
    public bool areGostGenerated = false;


    private float time = 0f;
    public float timeSinceGost = 0f;

    private void Start()
    {
        percents = new Dictionary<string, float>();
        percents.Add("Joleen", 0);
        percents.Add("Abe", 0);
        percents.Add("Botlle", 0);
        abePerBones = new Dictionary<string, List<float>>();
        joleenPerBones = new Dictionary<string, List<float>>();
       }
    private void Update()
    {
        time += Time.deltaTime;
        if (time >= 1)
        {
            time = 0;
            //float percent = GetPercent();
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
            {
                GlobalState.Animation.ClearAnimationsOnDeletedObject();
            }
        }
        if (areGostGenerated)
        {
            timeSinceGost += Time.deltaTime;
        }
    }

    
    /// <summary>
    /// Get the animation percent of similitaries between gost and user animations
    /// </summary>
    /// <returns>% of similtaries</returns>
    public float GetPercent()
    {
        float ret = 0;
        //todo enable animators for users characters
        Animator joleenAnimator = joleen.GetComponent<Animator>();
        Animator abeAnimator = abe.GetComponent<Animator>();
        Animator initBottleAnimator = bottleInit.GetComponent<Animator>();
        joleenAnimator.enabled = true;
        abeAnimator.enabled = true;
        initBottleAnimator.enabled = true;
        AnimationClip Throw, Dye, BottleAnimationClip;
        Throw = GetComponent<ClipManager>().Throw;
        Dye = GetComponent<ClipManager>().Dye;
        BottleAnimationClip = GetComponent<ClipManager>().BottleClip;
        if (abe != null && joleen != null && bottleInit != null)
        {
            float AllFrameSumAbe = 0f;
            float AllFrameSumJoleen = 0f;
            float AllFrameSumBottle = 0f;

            float percentAbe = 0f;
            float percentJoleen = 0f;
            float percentBottle = 0f;
            int startFrame, endFrame;

            var joleenHumanGoalControllers = gostJoleen.GetComponentsInChildren<HumanGoalController>();
            var abeHumanGoalControllers = gostAbe.GetComponentsInChildren<HumanGoalController>();
            //var bottleHumanGoalController = bottleInit.GetComponent<HumanGoalController>();
            //var gostJoleenHumanGoalControllers = gostJoleen.GetComponentsInChildren<HumanGoalController>();
            //var gostAbeHumanGoalControllers = gostAbe.GetComponentsInChildren<HumanGoalController>();
            //var gostBottleHumanGoalControllers = bottle.GetComponent<HumanGoalController>();
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
            {
                startFrame = GlobalState.Animation.StartFrame;
                endFrame = GlobalState.Animation.EndFrame;
            }
            else
            {
                startFrame = GlobalStateTradi.Animation.StartFrame;
                endFrame = GlobalStateTradi.Animation.EndFrame;
            }

            for (int i = startFrame; i < endFrame; i++)
            {

                //todo put the animator into the i frame and evaluate position from unity animator and ubi engine for gosts
                //animator[clip.name].time = frame/60
                //joleenAnimator.
                Throw.SampleAnimation(joleen, i / 60);
                Dye.SampleAnimation(abe, i / 60);
                BottleAnimationClip.SampleAnimation(bottleInit, i / 60);
                int counterJoleen = 0;
                float joleenDiffSum = 0;
                int counterAbe = 0;
                float abeDiffSum = 0f;
                //throw
                foreach (var joleenController in joleenHumanGoalControllers)
                {
                    if(gostAndOrigin.TryGetValue(joleenController.gameObject,out GameObject origin) && joleenController.Animation != null)
                    {
                        Vector3 vecByHips = joleen.transform.InverseTransformPoint(origin.transform.position);
                        Vector3 diff = vecByHips - joleenController.LocalFramePosition(i);
                        if (i < 5)
                            Debug.Log("Bone : " + joleenController.name + "Origine pos : " + vecByHips + " clone local pos : " + joleenController.LocalFramePosition(i));
                        joleenDiffSum += 1 - Mathf.Clamp01(diff.magnitude / delta);
                        counterJoleen++;
                        if (joleenPerBones.TryGetValue(joleenController.name, out List<float> value))
                        {
                            value.Add( 1 - Mathf.Clamp01(diff.magnitude / delta));
                        }
                        else
                        {
                            joleenPerBones.Add(joleenController.name,new List<float> { 1 - Mathf.Clamp01(diff.magnitude / delta) });
                        }
                    }
                   
                }
                AllFrameSumJoleen += joleenDiffSum / counterJoleen;
                // dye
                foreach (var abeController in abeHumanGoalControllers)
                {
                    if(gostAndOrigin.TryGetValue(abeController.gameObject,out GameObject origin) && abeController.Animation != null)
                    {
                        Vector3 vecByHips = abe.transform.InverseTransformPoint(origin.transform.position);
                        Vector3 diff = vecByHips - abeController.LocalFramePosition(i);
                        abeDiffSum += 1 - Mathf.Clamp01(diff.magnitude / delta);
                        counterAbe++;
                        if (abePerBones.TryGetValue(abeController.name, out List<float> value))
                        {
                            value.Add(1 - Mathf.Clamp01(diff.magnitude / delta));
                        }
                        else
                        {
                            abePerBones.Add(abeController.name, new List<float> { 1 - Mathf.Clamp01(diff.magnitude / delta) });
                        }
                    }
                    
                }
                AllFrameSumAbe += abeDiffSum / counterAbe;
                //bouteille
                Vector3 diffBottle =joleen.transform.InverseTransformPoint(bottleInit.transform.position) - gostJoleen.transform.InverseTransformPoint(bottle.transform.localPosition);
                AllFrameSumBottle += 1 - Mathf.Clamp01(diffBottle.magnitude / delta);
            }
            percentJoleen = 100 * (AllFrameSumJoleen / (endFrame - 1));
            percentAbe = 100 * (AllFrameSumAbe / (endFrame - 1));
            percentBottle = 100 * (AllFrameSumBottle / (endFrame - 1));
            if (percents.TryGetValue("Joleen", out float valueJol))
            {
                valueJol = percentJoleen;
                ret += valueJol;
            }
            if (percents.TryGetValue("Abe", out float valueAbe))
            {
                valueAbe = percentAbe;
                ret += valueAbe;
            }
            if (percents.TryGetValue("Bottle", out float valueBottle))
            {
                valueBottle = percentBottle;
                ret += valueBottle;
            }

        }
        writeInFile();
        return (ret / 3f);
    }

    private Transform getOriginalTransform(String goal, GameObject origin)
    {
        if (origin.name == goal)
        {
            return origin.transform;
        }
        foreach (Transform item in origin.transform)
        {
              Transform t =  getOriginalTransform(goal, item.gameObject);
            if (t != null)
            {
                return t;
            }
        }
        return null;
    }
    private void checkAnimationsOfGosts(GameObject go)
    {
        var goGoal = go.GetComponentsInChildren<HumanGoalController>();
        foreach (var item in goGoal)
        {
            item.CheckAnimations();
        }
    }

    private void writeInFile()
    {
        StreamWriter writer = new StreamWriter(path, true);

        writer.WriteLine("session : " + System.DateTime.Now);

        writer.WriteLine("Joleen : ");

      
        foreach (var item in joleenPerBones)
        {
            writer.WriteLine(item.Key + ": " +  ListToString(item.Value));
        }
        writer.WriteLine("Abe : ");
        foreach (var item in abePerBones)
        {
            writer.WriteLine(item.Key + ": " + ListToString(item.Value));
        }

        writer.Close();
    }

    private string ListToString(List<float> list)
    {
        string ret = "";

        foreach (var item in list)
        {
            ret += item.ToString() + ";";
        }
        return ret;
    }
    public void CreateGost()
    {
        if (gostJoleen == null)
        {
            joleen = GameObject.Find("aj@Throw Object.DD5C871E.9");
            if (joleen != null)
            {
                gostJoleen = Instantiate(joleen, joleen.transform.parent);
                /* var temp = gostJoleen.GetComponentsInChildren<Renderer>();
                 Texture texture = joleen.GetComponentInChildren<Renderer>().material.GetTexture("_ColorMap");
                 foreach (var item in temp)
                 {
                     item.material = gostMaterial;
                     item.material.SetTexture("_ColorMap", texture);
                 }*/
                if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                    GlobalState.Animation.CopyAnimation(joleen, gostJoleen);
                else
                    GlobalStateTradi.Animation.CopyAnimation(joleen, gostJoleen);
                gostJoleen.transform.position += Vector3.forward;
                checkAnimationsOfGosts(gostJoleen);
                AnimationManager manager = gameObject.GetComponent<AnimationManager>();
                manager.ClearAnimationFormOrigin(joleen);
                Destroy(gostJoleen.GetComponent<Animator>());
                CreateDictionnaryGostOrigin(gostJoleen, joleen);
            }


        }
        if (gostAbe == null)
        {
            abe = GameObject.Find("aj@Dying.C69DCA00.10");
            if (abe != null)
            {
                gostAbe = Instantiate(abe, abe.transform.parent);
                var temp = gostAbe.GetComponentsInChildren<Renderer>();
                Texture texture = abe.GetComponentInChildren<Renderer>().material.GetTexture("_ColorMap");
                foreach (var item in temp)
                {
                    item.material = gostMaterial;
                    item.material.SetTexture("_ColorMap", texture);
                }
                if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                    GlobalState.Animation.CopyAnimation(abe, gostAbe);
                else
                    GlobalStateTradi.Animation.CopyAnimation(abe, gostAbe);
                gostAbe.transform.position += Vector3.forward;
                checkAnimationsOfGosts(gostAbe);
                gameObject.GetComponent<AnimationManager>().ClearAnimationFormOrigin(abe);
                Destroy(gostAbe.GetComponent<Animator>());
                CreateDictionnaryGostOrigin(gostAbe, abe);
            }
        }
        if (bottle == null)
        {
            bottleInit = GameObject.Find("bottle.7818A175.703");
            if (bottleInit != null)
            {
                bottle = Instantiate(bottleInit, bottleInit.transform.parent);
                DestroyImmediate(bottle.GetComponent<Animator>());
                Animator gostBottleAnim = bottle.AddComponent<Animator>();
                gostBottleAnim.runtimeAnimatorController = controllerGostBottle;
                
                var temp = bottle.GetComponentsInChildren<Renderer>();
                Texture texture = bottleInit.GetComponentInChildren<Renderer>().material.GetTexture("_ColorMap");
                foreach (var item in temp)
                {
                    item.material = gostMaterial;
                    item.material.SetTexture("_ColorMap", texture);
                }

                if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                {
                    GlobalState.Animation.CopyAnimation(bottleInit, bottle);
                    GlobalState.Animation.ClearAnimations(bottleInit);
                }
                else
                {
                    GlobalStateTradi.Animation.CopyAnimation(bottleInit, bottle);
                    GlobalStateTradi.Animation.ClearAnimations(bottleInit);
                }
                gostBottleAnim.enabled = false;
                bottle.transform.position += Vector3.forward;
                MoveAnimation(bottle);
                checkAnimationsOfGosts(bottle);
                Destroy(bottle.GetComponent<Animator>());
            }
        }
        areGostGenerated = true;
    }


    void CreateDictionnaryGostOrigin(GameObject gost,GameObject origin)
    {
        foreach (Transform item in gost.transform)
        {
            string temp = "";
            if (item.name.Contains("Clone"))
            {
                temp = item.name.Replace("(Clone)", "");
            }
            else
            {
                temp = item.name;
            }
            gostAndOrigin.Add(item.gameObject, getOriginalTransform(temp, origin).gameObject);
            CreateDictionnaryGostOrigin(item.gameObject, origin);
        }
    }


    void MoveAnimation(GameObject bottleGost)
    {
        GameObject go = new GameObject();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
        {
            Transform transform = GameObject.Find("World").transform;
            go.transform.SetParent(transform);
            go.transform.rotation = transform.rotation;
        }
        else
        {
            Transform transform = GameObject.Find("RightHanded").transform;
            go.transform.SetParent(transform);
            go.transform.rotation = transform.rotation;
        }
        //go.transform.position = bottleInit.transform.position;

        go.transform.position = Vector3.forward;
        bottleGost.transform.SetParent(go.transform);
        bottleGost.transform.position = Vector3.zero;
       
    }


}
