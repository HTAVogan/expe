using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GostManager : MonoBehaviour
{
    public GameObject gostJoleen;
    public GameObject gostAbe;
    public GameObject bottle;
    private List<Material> materialsJoleen = new List<Material>();
    private List<Material> materialsAbe = new List<Material>();
    private List<Material> materialsBottle = new List<Material>();

    // Update is called once per frame
    void Update()
    {
        if (gostJoleen == null)
        {
            GameObject joleen = GameObject.Find("Ch34_nonPBR@Throw Object.7818A175.695");
            if (joleen != null)
            {
                gostJoleen = Instantiate(joleen, joleen.transform.parent);
                var temp = gostJoleen.GetComponentsInChildren<Renderer>();
                foreach (var item in temp)
                {
                    materialsJoleen.Add(item.material);
                }
                for (int i = 0; i < materialsJoleen.Count; i++)
                {
                    materialsJoleen[i].SetFloat("_OpacityMap", 1);
                    materialsJoleen[i].SetFloat("_Opacity", 0.2f);
                }
            }
                
        }
        if (gostAbe == null)
        {
            GameObject abe = GameObject.Find("Ch34_nonPBR@Throw Object.7818A175.695");
            if (abe != null)
            {
                gostAbe = Instantiate(abe, abe.transform.parent);
                var temp = gostAbe.GetComponentsInChildren<Renderer>();
               foreach(var item in temp)
                {
                    materialsAbe.Add(item.material);
                }
                for (int i = 0; i < materialsAbe.Count; i++)
                {
                    materialsAbe[i].SetFloat("_OpacityMap", 1);
                    materialsAbe[i].SetFloat("_Opacity", 0.2f);
                }
            }
        }
        if (bottle == null)
        {
            GameObject bottleInit = GameObject.Find("bottle.7818A175.703");
            if (bottleInit != null)
            {

                bottle = Instantiate(bottleInit, bottleInit.transform.parent);
                var temp = bottle.GetComponentsInChildren<Renderer>();
                foreach (var item in temp)
                {
                    materialsBottle.Add(item.material);
                }
                for (int i = 0; i < materialsBottle.Count; i++)
                {
                    materialsBottle[i].SetFloat("_OpacityMap", 1);
                    materialsBottle[i].SetFloat("_Opacity", 0.2f);
                }
            }
        }
    }
}
