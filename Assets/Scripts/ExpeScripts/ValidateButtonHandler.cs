using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRtist;
using UnityEngine.SceneManagement;
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
    private void Start()
    {
        time = 0;
        finished = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (!finished)
            time += Time.deltaTime;
        
    }

    public void OnValidateButton()
    {
        finished = true;

        StreamWriter writer = new StreamWriter(path, true);
        ActionVRCount counterAction = global.GetComponent<ActionVRCount>();
        //writer.WriteLine("Eval mode;Time spent; Percent of similitudes; Number of actions; Actions done; Translation for each animated GO");
        if (GlobalState.translations != null)
        {
            string line = evalMode + ";" + time.ToString() + ";" + gostManager.GetComponent<GostManager>().GetPercent().ToString() + ";" + counterAction.numberOfAction.ToString() + ";";
            foreach (var item in counterAction.inputsDone)
            {
                line += item.Key + " : " + item.Value + "/";
            }
            line += ";";
            foreach (var item in GlobalState.translations)
            {
                line += item.Key.name + ":";
                foreach (var vec in item.Value)
                {
                    line += vec.ToString() + "|";
                }
                line += ";";
            }

        
            writer.WriteLine(line);
            writer.Close();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("Launcher");

    }
}
