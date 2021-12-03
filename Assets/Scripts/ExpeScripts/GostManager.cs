using System.Collections;
using System.Collections.Generic;
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
    [Range(0.1f, 10f)]
    public float delta;

    private float time = 0f;
    // Update is called once per frame
    void Update()
    {
        if (gostJoleen == null)
        {
            joleen = GameObject.Find("Ch34_nonPBR@Throw Object.7818A175.695");
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
                gostJoleen.transform.position += Vector3.forward;
                checkAnimationsOfGosts(gostJoleen);
                AnimationManager manager = gameObject.GetComponent<AnimationManager>();
                manager.ClearAnimationFormOrigin(joleen);

            }


        }
        if (gostAbe == null)
        {
            abe = GameObject.Find("Ch39_nonPBR@Dying.7818A175.698");
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

            }
        }
        if (bottle == null)
        {
            bottleInit = GameObject.Find("bottle.7818A175.703");
            if (bottleInit != null)
            {

                bottle = Instantiate(bottleInit, bottleInit.transform.parent);
                var temp = bottle.GetComponentsInChildren<Renderer>();
                Texture texture = bottleInit.GetComponentInChildren<Renderer>().material.GetTexture("_ColorMap");
                foreach (var item in temp)
                {
                    item.material = gostMaterial;
                    item.material.SetTexture("_ColorMap", texture);
                }
                if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Tradi"))
                    GlobalState.Animation.CopyAnimation(bottleInit, bottle);
                else
                    GlobalStateTradi.Animation.CopyAnimation(bottleInit, bottle);
                bottle.transform.position += Vector3.forward;
                checkAnimationsOfGosts(bottle);
                gameObject.GetComponent<AnimationManager>().ClearAnimationFormOrigin(bottle);

            }
        }
        time += Time.deltaTime;
        if (time >= 1)
        {
            time = 0;
            float percent = GetPercent();
        }

    }
    /// <summary>
    /// Get the animation percent of similitaries between gost and user animations
    /// </summary>
    /// <returns>% of similtaries</returns>
    public float GetPercent()
    {
        if (abe != null && joleen != null)
        {
            Vector3 unitAlpha = Vector3.one * delta;
            int startFrame, endFrame;
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

            float ret = 0f;
            var tempGostAbe = gostAbe.GetComponentsInChildren<HumanGoalController>();
            var tempOriginalAbe = abe.GetComponentsInChildren<HumanGoalController>();
            var tempGostJoleen = gostJoleen.GetComponentsInChildren<HumanGoalController>();
            var tempOriginalJoleen = joleen.GetComponentsInChildren<HumanGoalController>();
            float percentAbePerFrame = 0f;
            float percentAbeGlobal = 0f;
            float percentJoleenPerFrame = 0f;
            float percentJoleenGlobal = 0f;
            for (int i = startFrame; i <= endFrame; i++)
            {
                int counterAbe = 0;
                int counterJoleen = 0;
                float percentAbeSum = 0f;
                float percentJoleenSum = 0f;
                foreach (var item in tempGostAbe)
                {
                    Vector3 diff = item.LocalFramePosition(i) - tempOriginalAbe[counterAbe].LocalFramePosition(i);
                   // Debug.Log(diff);
                    percentAbeSum += (100 * (diff.x / unitAlpha.x) + 100 * (diff.y / unitAlpha.y) + 100 * (diff.z / unitAlpha.z)) / 3;
                    counterAbe++;
                }
                percentAbePerFrame += percentAbeSum / counterAbe;
                // Debug.Log("percent abe on this "+i +" is : " + percentJoleenPerFrame + "because sub diff was : "+ percentAbeSum + " and counter was " + counterAbe);
                foreach (var item in tempGostJoleen)
                {

                    Vector3 diff = item.LocalFramePosition(i) - tempOriginalJoleen[counterJoleen].LocalFramePosition(i);
                    percentJoleenSum += (100 * (diff.x / unitAlpha.x) + 100 * (diff.y / unitAlpha.y) + 100 * (diff.z / unitAlpha.z)) / 3;
                    counterJoleen++;
                }
                percentJoleenPerFrame += percentJoleenSum / counterJoleen;
            }
            percentAbeGlobal = percentAbePerFrame;
            percentAbeGlobal /= endFrame;
            percentJoleenGlobal = percentJoleenPerFrame;
            percentJoleenGlobal /= endFrame;
            ret = (percentJoleenGlobal + percentAbeGlobal) / 2;
           // Debug.Log("Percent actually : " + (100 - ret));
            return 100 - ret;
        }
        return 0;

    }

    private void checkAnimationsOfGosts(GameObject go)
    {
        var goGoal = go.GetComponentsInChildren<HumanGoalController>();
        foreach (var item in goGoal)
        {
            item.CheckAnimations();
        }
    }


}
