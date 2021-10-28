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
                GlobalState.Animation.CopyAnimation(joleen, gostJoleen);
                gostJoleen.transform.position += Vector3.forward;
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
                GlobalState.Animation.CopyAnimation(abe, gostAbe);
                gostAbe.transform.position += Vector3.forward;

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
                GlobalState.Animation.CopyAnimation(bottleInit, bottle);
                bottle.transform.position += Vector3.forward;

            }
        }
        time += Time.deltaTime;
        if(time >= 1)
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
        Vector3 unitAlpha = Vector3.one * delta;
        float ret = 0f;
        var tempGostAbe = gostAbe.GetComponentsInChildren<HumanGoalController>();
        var tempOriginalAbe = abe.GetComponentsInChildren<HumanGoalController>();
        var tempGostJoleen = gostJoleen.GetComponentsInChildren<HumanGoalController>();
        var tempOriginalJoleen = joleen.GetComponentsInChildren<HumanGoalController>();
        float percentAbePerFrame = 0f;
        float percentAbeGlobal = 0f;
        float percentJoleenPerFrame = 0f;
        float percentJoleenGlobal = 0f;
        for (int i = GlobalState.Animation.StartFrame; i <= GlobalState.Animation.EndFrame; i++)
        {
            int counterAbe = 0;
            int counterJoleen = 0;
            float percentAbeSum = 0f;
            float percentJoleenSum = 0f;
            foreach (var item in tempGostAbe)
            {
                counterAbe++;
                Vector3 diff = item.FramePosition(i) -  tempOriginalAbe[counterAbe].FramePosition(i);
                percentAbeSum = (100 * (diff.x / unitAlpha.x) + 100 * (diff.y / unitAlpha.y) + 100 * (diff.z / unitAlpha.z)) / 3;
            }
            percentAbePerFrame += percentAbeSum / counterAbe;
            foreach (var item in tempGostJoleen)
            {
                counterJoleen++;
                Vector3 diff = item.FramePosition(i) - tempOriginalJoleen[counterJoleen].FramePosition(i);
                percentJoleenSum = (100 * (diff.x / unitAlpha.x) + 100 * (diff.y / unitAlpha.y) + 100 * (diff.z / unitAlpha.z)) / 3;
            }
            percentJoleenPerFrame += percentJoleenSum / counterJoleen;
        }
        percentAbeGlobal = percentAbePerFrame;
        percentAbeGlobal /= GlobalState.Animation.EndFrame;
        percentJoleenGlobal = percentJoleenPerFrame;
        percentJoleenGlobal /= GlobalState.Animation.EndFrame;
        ret = (percentJoleenGlobal + percentAbeGlobal) / 2;
        return ret;
    }
}
