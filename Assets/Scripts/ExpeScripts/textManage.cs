using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;



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
        gameObject.GetComponent<TextMeshProUGUI>().text = Text;
    }
}
