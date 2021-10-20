using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRtist;

public class GostManager : MonoBehaviour
{
    public GameObject gostJoleen;
    public GameObject gostAbe;
    public GameObject bottle;
    public Material gostMaterial;

    private GameObject joleen;
    private GameObject abe;
    private GameObject bottleInit;
    [Range(0.1f,10f)]
    public float dela;


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
                    item.material.SetTexture("_ColorMap",texture) ;
                }
                GlobalState.Animation.CopyAnimation(bottleInit, bottle);
                bottle.transform.position += Vector3.forward;

            }
        }
    }
    /// <summary>
    /// Get the animation percent of similitaries between gost and user animations
    /// </summary>
    /// <returns>% of similtaries</returns>
    float getPercent()
    {
        float ret = 0f;
        var tempGost =gostAbe.GetComponentsInChildren<HumanGoalController>();
        var tempOriginal = abe.GetComponentsInChildren<HumanGoalController>();
        for(int i= GlobalState.Animation.StartFrame; i<GlobalState.Animation.EndFrame;i++)
        foreach (var item in tempGost)
        {
                Vector3 diff = item.FramePosition(i);
        }
        return ret;
    }
}
