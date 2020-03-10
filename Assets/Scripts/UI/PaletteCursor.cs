﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class PaletteCursor : MonoBehaviour
    {
        public GameObject tools = null;

        private Vector3 initialCursorLocalPosition = Vector3.zero;
        private bool isOnAWidget = false;
        private Transform widgetTransform = null;
        private UIElement widgetHit = null;
        private AudioSource audioClick = null;

        private Transform currentShapeTransform = null;
        private Transform arrowCursor = null;
        private Transform grabberCursor = null;

        void Start()
        {
            // Get the initial transform of the cursor mesh, in order to
            // be able to restore it when we go out of a widget.
            MeshFilter mf = GetComponentInChildren<MeshFilter>();
            GameObject go = mf.gameObject;
            Transform t = go.transform;
            Vector3 lp = t.localPosition;

            audioClick = GetComponentInChildren<AudioSource>();

            initialCursorLocalPosition = GetComponentInChildren<MeshFilter>().gameObject.transform.localPosition;

            arrowCursor = transform.Find("Arrow");
            grabberCursor = transform.Find("Grabber");
            ShowCursorShape(0); // arrow
            HideAllCursors();
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // Device rotation
                Vector3 position;
                Quaternion rotation;
                VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);

                // The main cursor object always follows the controller
                // so that the collider sticks to the actual hand position.
                transform.localPosition = position;
                transform.localRotation = rotation;

                if (isOnAWidget)
                {
                    Vector3 localCursorColliderCenter = GetComponent<SphereCollider>().center;
                    Vector3 worldCursorColliderCenter = transform.TransformPoint(localCursorColliderCenter);

                    if (widgetHit != null && widgetHit.HandlesCursorBehavior())
                    {
                        widgetHit.HandleCursorBehavior(worldCursorColliderCenter, ref currentShapeTransform);
                        // TODO: est-ce qu'on gere le son dans les widgets, ou au niveau du curseur ???
                        //       On peut faire ca dans le HandleCursorBehavior().
                        //audioClick.Play();
                    }
                    else
                    { 
                        //Vector3 localWidgetPosition = widgetTransform.InverseTransformPoint(cursorShapeTransform.position);
                        Vector3 localWidgetPosition = widgetTransform.InverseTransformPoint(worldCursorColliderCenter);
                        Vector3 localProjectedWidgetPosition = new Vector3(localWidgetPosition.x, localWidgetPosition.y, 0.0f);
                        Vector3 worldProjectedWidgetPosition = widgetTransform.TransformPoint(localProjectedWidgetPosition);
                        currentShapeTransform.position = worldProjectedWidgetPosition;

                        // Haptic intensity as we go deeper into the widget.
                        float intensity = Mathf.Clamp01(0.001f + 0.999f * localWidgetPosition.z / UIElement.collider_min_depth_deep);
                        intensity *= intensity; // ease-in
                        VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<UIVolumeTag>() != null)
            {
                // deactivate all tools
                //tools.SetActive(false);
                if (!CommandManager.IsUndoGroupOpened())
                {
                    ToolsUIManager.Instance.ShowTools(false);
                    ShowCursorShape(0); // arrow
                }
            }
            else if (other.gameObject.tag == "UICollider")
            {
                isOnAWidget = true;
                widgetTransform = other.transform;
                widgetHit = other.GetComponent<UIElement>();
                VRInput.SendHaptic(VRInput.rightController, 0.015f, 0.5f);
                audioClick.Play();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<UIVolumeTag>() != null)
            {
                // reactivate all tools
                //tools.SetActive(true);
                ToolsUIManager.Instance.ShowTools(true);
                HideAllCursors();
            }
            else if (other.gameObject.tag == "UICollider")
            {
                isOnAWidget = false;
                widgetTransform = null;
                widgetHit = null;
                GetComponentInChildren<MeshFilter>().gameObject.transform.localPosition = initialCursorLocalPosition;
            }
        }

        // 0: arrow, 1: box
        public void ShowCursorShape(int shape)
        {
            HideAllCursors();
            switch (shape)
            {
                case 0: arrowCursor.gameObject.SetActive(true); currentShapeTransform = arrowCursor.transform; break;
                case 1: grabberCursor.gameObject.SetActive(true); currentShapeTransform = grabberCursor.transform; break;
            }
        }

        private void HideAllCursors()
        {
            arrowCursor.gameObject.SetActive(false);
            grabberCursor.gameObject.SetActive(false);

            //MeshRenderer[] rr = GetComponentsInChildren<MeshRenderer>();
            //foreach (MeshRenderer r in rr)
            //{
            //    r.enabled = false;
            //}
        }
    }
}