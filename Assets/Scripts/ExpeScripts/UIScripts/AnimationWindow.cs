using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using VRtist;

public class AnimationWindows : EditorWindow
{
    [MenuItem("Animation/Main Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationWindows>();

        window.titleContent = new GUIContent("Animation main window");

        window.minSize = new Vector2(250, 50);
    }

    float hSbarValue;
    bool isPlaying = false;
    
    private void OnGUI()
    {
        // Reference to the root of the window.
        GUILayout.BeginArea(new Rect(5, 5, position.width - 10, position.height - 10));
        {
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Play", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
            {
                PlayAnimation();
            }
            if (GUILayout.Button("Pause", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
            {
                PauseAnimation();
            }
            hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 0.10f, 0.10f, 15.0f);
            if (isPlaying)
            {
                this.Focus();
                hSbarValue = GlobalStateTradi.Animation.CurrentFrame/10f;
            }
            if (GlobalStateTradi.Animation != null)
            {
                GlobalStateTradi.Animation.CurrentFrame = Mathf.CeilToInt(hSbarValue*10);
                if (EditorWindow.HasOpenInstances<AnimationWindow>())
                {
                    GetWindow<AnimationWindow>().time = (GlobalStateTradi.Animation.CurrentFrame -1) / 60f;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
   

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
        if(GlobalStateTradi.Animation != null)
        {
            GlobalStateTradi.Animation.Play();
            isPlaying = true;
        }
    }
}
