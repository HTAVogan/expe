using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LauncherManager : MonoBehaviour
{
    public int first;
    public int second;
    public bool tutoHasBeenDone = false;
    public string pathRes = "Assets/Resources/results.txt";
    public string pathBones = "Assets/Resources/resultsPerBone.txt";

    public List<int> scenes = new List<int>() { 1, 2, 3 };
    void Awake()
    {
        first = Random.Range(1, 4);
        scenes.Remove(first);
        second = Random.Range(scenes[0], scenes[1]);

        StreamWriter writer = new StreamWriter(pathRes, true);
        writer.WriteLine("session : " + System.DateTime.Now + ";");
        writer.Close();


        writer = new StreamWriter(pathBones, true);
        writer.WriteLine("session : " + System.DateTime.Now + ";");
        writer.Close();


    }

    //private void Update()
    //{
    //    Debug.Log(Random.Range(1, 4));
    //}

}
