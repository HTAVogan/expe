using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using VRtist;
using UnityEngine.InputSystem;
using System.IO;

public class AnimationWindows : EditorWindow
{

    static ActionCountTradi counter;

    [MenuItem("Animation/Main Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationWindows>();

        window.titleContent = new GUIContent("Animation main window");

        window.minSize = new Vector2(250, 50);

        counter = GlobalStateTradi.Animation.ActionCountTradi;
        
    }

    float hSbarValue;
    bool isPlaying = false;
    float similitudes;
    bool isGhostAlreadyGen = false;
    bool isPreviewAllow = true;
    public string path = "Assets/Resources/results.txt";
    private string evalMode = "1";
    public Vector3 originalPos;

    private bool isOriginal = true;
    private bool isReturnPos = false;
    private void OnGUI()
    {
        // Reference to the root of the window.


        GUILayout.BeginArea(new Rect(5, 5, position.width - 10, position.height - 10));
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Play", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
            {
                PlayAnimation();
            }
            if (GUILayout.Button("Pause", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
            {
                PauseAnimation();
            }
            hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 0.10f, 0.10f, 30.0f);
            if (isPlaying)
            {
                this.Focus();
                hSbarValue = GlobalStateTradi.Animation.CurrentFrame / 10f;
            }
            if (GlobalStateTradi.Animation != null)
            {
                GlobalStateTradi.Animation.CurrentFrame = Mathf.CeilToInt(hSbarValue * 10);
                if (EditorWindow.HasOpenInstances<AnimationWindow>())
                {
                    GetWindow<AnimationWindow>().time = ((GlobalStateTradi.Animation.CurrentFrame - 1) / 60f);
                    GetWindow<AnimationWindow>().previewing = isPreviewAllow;
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();
            if (!isGhostAlreadyGen)
                if (GUILayout.Button("Generate Ghosts", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                {
                    GenerateGosts();
                }
            if (GUILayout.Button("Place ghost to original pos", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
            {
                PlaceGost();
            }
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Calculate similitudes", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
            {
                Calculate();
            }
       
       

            GUILayout.Label(similitudes.ToString());

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Validate", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
            {
                Validate();
            }
            GUILayout.EndArea();
         
        }


    }

    private void PlaceGost()
    {
        GlobalStateTradi.Animation.gostManager.PosGost();
    }

    private void Validate()
    {
        StreamWriter writer = new StreamWriter(path, true);
        ActionCountTradi counter = GameObject.Find("ActionCounter").GetComponent<ActionCountTradi>();

        writer.WriteLine("TimeOfEval;Eval mode;Time spent; Percent of similitudes; frames number with a keyframe;Number of actions; Actions done; Translation for each animated GO");
        string line = "";
        line = System.DateTime.Now + ";" + evalMode + ";" + GlobalStateTradi.Animation.gostManager.timeSinceGost.ToString() + ";" + GlobalStateTradi.Animation.gostManager.GetComponent<GostManager>().GetPercent().ToString() + ";" + GetKeyFrameNumber().ToString()+";" + counter.actionsCount.ToString() + ";";
        foreach (var item in counter.actions)
        {
            line += item.Key + " : " + item.Value + "/";
        }
        line += ";";
        if (GlobalStateTradi.translations != null)
        {

            foreach (var item in GlobalStateTradi.translations)
            {
                line += item.Key.name + ":";
                foreach (var vec in item.Value)
                {
                    line += vec.ToString() + "|";
                }
                line += ";";
            }

        }
        writer.WriteLine(line);
        writer.Close();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Launcher");
    }
    private void AllowPreview()
    {
        isPreviewAllow = !isPreviewAllow;
    }

    private void Calculate()
    {
        if (GlobalStateTradi.Animation != null)
        {
            similitudes = GlobalStateTradi.Animation.gostManager.GetPercent();
        }
    }

    private void GenerateGosts()
    {
        GlobalStateTradi.Animation.gostManager.CreateGost();
        isGhostAlreadyGen = true;
    }

    private void PauseAnimation()
    {

        if (GlobalStateTradi.Animation != null)
        {
            GlobalStateTradi.Animation.Pause();

            isPlaying = false;
        }
    }

    private void PlayAnimation()
    {
        if (GlobalStateTradi.Animation != null)
        {
            GlobalStateTradi.Animation.Play();
            isPlaying = true;
        }
    }

    private int GetKeyFrameNumber()
    {

        return 0;
    }
}
