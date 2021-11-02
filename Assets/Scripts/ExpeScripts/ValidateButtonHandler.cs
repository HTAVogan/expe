using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRtist;
using System.IO;

public class ValidateButtonHandler : MonoBehaviour
{
    // Start is called before the first frame update

    private float time;
    private bool finished;
    public string evalMode;
    public GameObject gostManager;
    public GameObject global;
    public string path = "Assets/Resources/results.txt";

    // Update is called once per frame
    void Update()
    {
        if (!finished)
            time += Time.deltaTime;
        
    }

    public void OnValidate()
    {
        finished = true;

        if(GlobalState.translations != null)
        {
            string line = evalMode + ";" + time.ToString() + ";" + gostManager.GetComponent<GostManager>().GetPercent().ToString() + ";" + global.GetComponent<ActionVRCount>().numberOfAction.ToString() + ";";
            foreach (var item in GlobalState.translations)
            {
                line += item.Key.name + ":";
                foreach (var vec in item.Value)
                {
                    line += vec.ToString() + "|";
                }
                line += ";";
            }

            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine(line);
            writer.Close();
        }
        


    }
}
