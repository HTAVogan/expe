using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class textManage : MonoBehaviour
{
    public GameObject gostManager;
    private string Text;
    void Update()
    {
        if (gostManager != null)
        {
            Text = gostManager.GetComponent<GostManager>().GetPercent().ToString();
        }
        gameObject.GetComponent<Text>().text = Text;
    }
}
