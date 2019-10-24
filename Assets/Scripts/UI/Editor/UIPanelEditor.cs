﻿using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(UIPanel))]
public class SomeScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.HelpBox("Rebuild the mesh of the UIPanel", MessageType.Info);

        UIPanel uiPanel = (UIPanel)target;
        if (GUILayout.Button("Rebuild"))
        {
            uiPanel.RebuildMesh();
        }
    }

    private void OnSceneGUI()
    {
        //if (Event.current.type == EventType.Repaint)

        UIPanel uiPanel = target as UIPanel;
        
        Transform T = uiPanel.transform;
        
        Vector3 posLeft   = T.position + (Vector3)(T.localToWorldMatrix * -new Vector4(uiPanel.width / 2.0f, 0, 0, 1));
        Vector3 posRight  = T.position + (Vector3)(T.localToWorldMatrix *  new Vector4(uiPanel.width / 2.0f, 0, 0, 1));
        Vector3 posBottom = T.position + (Vector3)(T.localToWorldMatrix * -new Vector4(0, uiPanel.height / 2.0f, 0, 1));
        Vector3 posTop    = T.position + (Vector3)(T.localToWorldMatrix *  new Vector4(0, uiPanel.height / 2.0f, 0, 1));
        float handleSize = uiPanel.radius * 2.0f;
        Vector3 snap = Vector3.one * 0.1f;

        EditorGUI.BeginChangeCheck();

        Handles.color = Handles.xAxisColor;
        Vector3 newTargetPosition_left = Handles.FreeMoveHandle(posLeft, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);
        Vector3 newTargetPosition_right = Handles.FreeMoveHandle(posRight, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

        Handles.color = Handles.yAxisColor;
        Vector3 newTargetPosition_bottom = Handles.FreeMoveHandle(posBottom, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);
        Vector3 newTargetPosition_top = Handles.FreeMoveHandle(posTop, Quaternion.identity, handleSize, snap, Handles.SphereHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Change Dimensions");

            Vector3 deltaLeft   = newTargetPosition_left - posLeft;
            Vector3 deltaRight  = newTargetPosition_right - posRight;
            Vector3 deltaBottom = newTargetPosition_bottom - posBottom;
            Vector3 deltaTop    = newTargetPosition_top - posTop;

            if (Vector3.SqrMagnitude(deltaLeft) > Mathf.Epsilon)
            {
                uiPanel.Width = 2.0f * Vector3.Magnitude(T.position - newTargetPosition_left) / T.localScale.x;
            }
            else if (Vector3.SqrMagnitude(deltaRight) > Mathf.Epsilon)
            {
                uiPanel.Width = 2.0f * Vector3.Magnitude(T.position - newTargetPosition_right) / T.localScale.x;
            }
            else if (Vector3.SqrMagnitude(deltaBottom) > Mathf.Epsilon)
            {
                uiPanel.Height = 2.0f * Vector3.Magnitude(T.position - newTargetPosition_bottom) / T.localScale.y;
            }
            else if (Vector3.SqrMagnitude(deltaTop) > Mathf.Epsilon)
            {
                uiPanel.Height = 2.0f * Vector3.Magnitude(T.position - newTargetPosition_top) / T.localScale.y;
            }
        }
    }
}
