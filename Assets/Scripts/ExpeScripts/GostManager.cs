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


    public AnimationClip perfectThrow;
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
        Debug.Log(0f / 40f);
    }


    public void CalculateSimilitudeButton()
    {
        GetPercent();
    }

    [ContextMenu("Put throw perfect")]
    public void MoveClip()
    {
        if(perfectThrow != null)
        {

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
        Animator joleenAnimator;
        Animator abeAnimator;
        Animator initBottleAnimator;
        AnimationClip Throw, Dye, BottleAnimationClip;
        abePerBones = new Dictionary<string, List<float>>();
        joleenPerBones = new Dictionary<string, List<float>>();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
        {
            joleenAnimator = joleen.GetComponent<Animator>();
            abeAnimator = abe.GetComponent<Animator>();
            initBottleAnimator = bottleInit.GetComponent<Animator>();
            joleenAnimator.enabled = true;
            abeAnimator.enabled = true;
            initBottleAnimator.enabled = true;
            Throw = GetComponent<ClipManager>().Throw;
            Dye = GetComponent<ClipManager>().Dye;
            BottleAnimationClip = GetComponent<ClipManager>().BottleClip;
        }
        #region useless but necessary to compile
        else
        {
            Throw =new AnimationClip();
            Dye = new AnimationClip();
            BottleAnimationClip = new AnimationClip();
        }
        #endregion
        if (abe != null && joleen != null && bottleInit != null)
        {
            float AllFrameSumAbe = 0f;
            float AllFrameSumJoleen = 0f;
            float AllFrameSumBottle = 0f;

            float percentAbe = 0f;
            float percentJoleen = 0f;
            float percentBottle = 0f;
            int startFrame, endFrame;

            var gostJoleenHumanGoalControllers = gostJoleen.GetComponentsInChildren<HumanGoalController>();
            var gostAbebeHumanGoalControllers = gostAbe.GetComponentsInChildren<HumanGoalController>();
            var joleenHumanGoalControllers = joleen.GetComponentsInChildren<HumanGoalController>();
            var abeHumanGoalControllers = abe.GetComponentsInChildren<HumanGoalController>();
            joleenPerBones.Add("Sum", new List<float>());
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
                int counterJoleen = 0;
                float joleenDiffSum = 0;
                int counterAbe = 0;
                float abeDiffSum = 0f;

                #region tradi
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                {
                    Throw.SampleAnimation(joleen, i / 60f);
                    Dye.SampleAnimation(abe, i / 60f);
                    BottleAnimationClip.SampleAnimation(bottleInit, i / 60f);
                    foreach (var joleenController in gostJoleenHumanGoalControllers)
                    {
                        int start, end;
                        KeyValuePair<int, int> startend = GetStartAndEndFrame(joleenController);
                        start = startend.Key;
                        end = startend.Value;
                        if (gostAndOrigin.TryGetValue(joleenController.gameObject, out GameObject origin) && joleenController.Animation != null && i>=start && i<=end)
                        {
                            //Debug.Log("Abe controller  = " + joleenController + " i is " + i + "start is " + start + " end is " + end);
                            //Vector3 vecByHips = origin.transform.position;
                            Vector3 vecByHips = joleen.transform.InverseTransformPoint(origin.transform.position);
                            Vector3 diff = vecByHips - joleenController.LocalFramePosition(i);
                            Debug.Log(joleenController + ": "  + "vec by hips : " + vecByHips + "diff is :" + diff);
                            joleenDiffSum += 1 - Mathf.Clamp01(diff.magnitude / delta);
                            counterJoleen++;
                            if (joleenPerBones.TryGetValue(joleenController.name, out List<float> value))
                            {
                                //value.Add(1 - Mathf.Clamp01(diff.magnitude / delta));
                                value.Add(diff.magnitude );
                            }
                            else
                            {
                                // joleenPerBones.Add(joleenController.name, new List<float> { 1 - Mathf.Clamp01(diff.magnitude / delta) });
                                joleenPerBones.Add(joleenController.name, new List<float> { diff.magnitude });
                            }
                        }

                    }
                    if(counterJoleen !=0)
                    AllFrameSumJoleen += joleenDiffSum / counterJoleen;
                    joleenPerBones["Sum"].Add(joleenDiffSum / counterJoleen);
                    // dye
                    foreach (var abeController in gostAbebeHumanGoalControllers)
                    {
                        int start, end;
                        KeyValuePair<int, int> startend = GetStartAndEndFrame(abeController);
                        start = startend.Key;
                        end = startend.Value;
                      
                        if (gostAndOrigin.TryGetValue(abeController.gameObject, out GameObject origin) && abeController.Animation != null && i >= start && i <= end)
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
                    if(counterAbe!=0)
                    AllFrameSumAbe += abeDiffSum / counterAbe;
                    //bouteille
                    int startBottle, endBottle;
                    KeyValuePair<int, int> startendBottle = GetStartAndEndFrame(bottle);
                    startBottle = startendBottle.Key;
                    endBottle = startendBottle.Value;
                    if (i<= endBottle && i >= startBottle)
                    {
                        Vector3 diffBottle = joleen.transform.InverseTransformPoint(bottleInit.transform.position) - gostJoleen.transform.InverseTransformPoint(bottle.transform.localPosition);
                        AllFrameSumBottle += 1 - Mathf.Clamp01(diffBottle.magnitude / delta);
                    }
                }
                #endregion
                #region VR
                else
                {
                    foreach (var joleenController in gostJoleenHumanGoalControllers)
                    {
                        Vector3 diff = Vector3.zero;
                        int start, end;
                        KeyValuePair<int, int> startend = GetStartAndEndFrame(joleenController);
                        start = startend.Key;
                        end = startend.Value;
                        if (joleenController.Animation != null && i >= start && i <= end)
                        {
                             diff = joleenHumanGoalControllers[counterJoleen].LocalFramePosition(i)- joleenController.LocalFramePosition(i);
                            joleenDiffSum += 1 - Mathf.Clamp01(diff.magnitude / delta);
                            counterJoleen++;
                            if (joleenPerBones.TryGetValue(joleenController.name, out List<float> value))
                            {
                                value.Add(1 - Mathf.Clamp01(diff.magnitude / delta));
                            }
                            else
                            {
                               joleenPerBones.Add(joleenController.name, new List<float> { 1 - Mathf.Clamp01(diff.magnitude / delta) });
                               
                            }
                        }
                    }
                    if(counterJoleen!=0)
                    AllFrameSumJoleen += joleenDiffSum / counterJoleen;
                    // dye
                    foreach (var abeController in gostAbebeHumanGoalControllers)
                    {
                        Vector3 diff = Vector3.zero;
                        int start, end;
                        KeyValuePair<int, int> startend = GetStartAndEndFrame(abeController);
                        start = startend.Key;
                        end = startend.Value;
                        if (abeController.Animation != null && i >= start && i <= end)
                        {
                             diff = abeHumanGoalControllers[counterAbe].LocalFramePosition(i) - abeController.LocalFramePosition(i);
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
                    if(counterAbe!=0)
                    AllFrameSumAbe += abeDiffSum / counterAbe;
                    //bouteille
                    AnimationSet set = GlobalState.Animation.GetObjectAnimation(bottleInit);
                    Vector3 diffBottle = Vector3.zero;
                    int startBottle, endBottle;
                    KeyValuePair<int, int> startendBottle = GetStartAndEndFrame(bottle);
                    startBottle = startendBottle.Key;
                    endBottle = startendBottle.Value;
                    if (set != null && i <= endBottle && i >= startBottle)
                    {
                        Matrix4x4 matrixInit = set.GetTranformMatrix(i);
                        Matrix4x4 matrixGost = GlobalState.Animation.GetObjectAnimation(bottle).GetTranformMatrix(i);
                        Vector3 initPos = Vector3.zero;
                        Vector3 gostPos = Vector3.zero;
                        Quaternion initQuaternion = Quaternion.identity;
                        Quaternion gostQuaternion = Quaternion.identity;
                        Vector3 initScale = Vector3.zero;
                        Vector3 gostScale = Vector3.zero;
                        Maths.DecomposeMatrix(matrixInit, out initPos, out initQuaternion, out initScale);
                        Maths.DecomposeMatrix(matrixGost, out gostPos, out gostQuaternion, out gostScale);
                        diffBottle = (initPos - joleenHumanGoalControllers[0].LocalFramePosition(i)) - (gostPos - gostJoleenHumanGoalControllers[0].LocalFramePosition(i));
                        AllFrameSumBottle += 1 - Mathf.Clamp01(diffBottle.magnitude / delta);
                    }



                   
                }
                #endregion
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
            Transform t = getOriginalTransform(goal, item.gameObject);
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
            writer.WriteLine(item.Key + ": " + ListToString(item.Value));
        }
        writer.WriteLine("Abe : ");
        foreach (var item in abePerBones)
        {
            writer.WriteLine(item.Key + ": " + ListToString(item.Value));
        }

        writer.Close();
    }


    private KeyValuePair<int,int> GetStartAndEndFrame(HumanGoalController controller)
    {
        int start, end;
        AnimationSet set;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
        {
            set = GlobalStateTradi.Animation.GetObjectAnimation(controller.gameObject);
        }
        else
        {
            set = GlobalState.Animation.GetObjectAnimation(controller.gameObject);
        }
        if (set != null)
        {
            Curve curve = set.GetCurve(AnimatableProperty.PositionX);
            start = set.StartFrame;
            end = curve.keys[curve.keys.Count - 1].frame;
        }
        else
        {
            set = controller.Animation;
            if(set == null)
            {
                start = 0;
                end = 1;
            }
            else
            {
                start = controller.Animation.StartFrame;
                Curve curve = set.GetCurve(AnimatableProperty.PositionX);
                end = curve.keys[curve.keys.Count - 1].frame;
            }
        }
        return new KeyValuePair<int, int>(start, end);
    }

    private KeyValuePair<int, int> GetStartAndEndFrame(GameObject controller)
    {
        int start, end;
        AnimationSet set;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
        {
            set = GlobalStateTradi.Animation.GetObjectAnimation(controller);
        }
        else
        {
            set = GlobalState.Animation.GetObjectAnimation(controller);
        }
        if (set != null)
        {
            Curve curve = set.GetCurve(AnimatableProperty.PositionX);
            start = set.StartFrame;
            end = curve.keys[curve.keys.Count - 1].frame;
        }
        else
        {
            start = 0;
            end = 1;
        }
        return new KeyValuePair<int, int>(start, end);
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
                 var temp = gostJoleen.GetComponentsInChildren<Renderer>();
                 Texture texture = joleen.GetComponentInChildren<Renderer>().material.GetTexture("_ColorMap");
                 foreach (var item in temp)
                 {
                     item.material = gostMaterial;
                     item.material.SetTexture("_ColorMap", texture);
                 }
                if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                    GlobalState.Animation.CopyAnimation(joleen, gostJoleen);
                else
                    GlobalStateTradi.Animation.CopyAnimation(joleen, gostJoleen);
                gostJoleen.transform.position += Vector3.forward*2;
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
                gostAbe.transform.position += Vector3.forward*2;
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
                bottle.transform.position += Vector3.forward*2;
                MoveAnimation(bottle);
                checkAnimationsOfGosts(bottle);
                Destroy(bottle.GetComponent<Animator>());
            }
        }
        areGostGenerated = true;
    }


    void CreateDictionnaryGostOrigin(GameObject gost, GameObject origin)
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
        bottleGost.transform.SetParent(go.transform);
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
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
        }
        //go.transform.position = bottleInit.transform.position;

        go.transform.position = Vector3.forward*2;
   

    }


}
