using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SimilitudeButton : MonoBehaviour
{

    public GameObject Text;
    public GostManager gostManager;
    // Start is called before the first frame update
  public void ButtonPressed()
    {
        string percent = "";
        if(gostManager != null)
        {
            percent = gostManager.GetPercent().ToString();
        }

        Text.GetComponent<TextMeshProUGUI>().text = percent;
    }
}
