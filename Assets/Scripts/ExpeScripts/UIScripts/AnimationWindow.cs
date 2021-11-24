using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using VRtist;

public class AnimationWindow : EditorWindow
{
    [MenuItem("Animation/Main Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<AnimationWindow>();

        window.titleContent = new GUIContent("Animation main window");

        window.minSize = new Vector2(250, 50);
    }


    
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
            AnimationCurve curve = new AnimationCurve();
            EditorGUILayout.CurveField(curve);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

    }

    private void PauseAnimation()
    {
        if (GlobalStateTradi.Animation != null)
        {
            GlobalStateTradi.Animation.Pause();
        }
    }

    private void PlayAnimation()
    {
        if(GlobalStateTradi.Animation != null)
        {
            GlobalStateTradi.Animation.Play();
        }
    }
}
