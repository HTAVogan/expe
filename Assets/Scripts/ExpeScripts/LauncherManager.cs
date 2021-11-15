using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LauncherManager : MonoBehaviour
{
    public int first;
    public int second;
    public bool tutoHasBeenDone = false;

    public List<int> scenes = new List<int>() { 1, 2, 3 };
    void Awake()
    {
        first = Random.Range(1, 3);
        scenes.Remove(first);
        second = Random.Range(scenes[0], scenes[1]);
    }

}
