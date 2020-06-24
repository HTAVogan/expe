﻿using System;
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

        private int currentShapeId = 0;
        private int previousShapeId = 0;
        private Transform currentShapeTransform = null;
        private Transform arrowCursor = null;
        private Transform grabberCursor = null;
        private IDisposable UIEnabled = null;

        private bool lockedOnAWidget = false;
        private bool isOutOfWidget = true;
        private bool isOutOfVolume = true;

        void Start()
        {
            // Get the initial transform of the cursor mesh, in order to
            // be able to restore it when we go out of a widget.
            MeshFilter mf = GetComponentInChildren<MeshFilter>(true);
            GameObject go = mf.gameObject;
            Transform t = go.transform;
            Vector3 lp = t.localPosition;

            audioClick = GetComponentInChildren<AudioSource>(true);

            initialCursorLocalPosition = GetComponentInChildren<MeshFilter>(true).gameObject.transform.localPosition;

            arrowCursor = transform.Find("Arrow");
            grabberCursor = transform.Find("Grabber");
            SetCursorShape(0); // arrow
            HideAllCursors();

            Utils.OnPrefabInstantiated += OnPrefabInstantiated;
        }

        void Update()
        {
            if (!VRInput.TryGetDevices())
                return;

            // Device rotation
            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);

            // The main cursor object always follows the controller
            // so that the collider sticks to the actual hand position.
            transform.localPosition = position;
            transform.localRotation = rotation;

            if (!UIElement.UIEnabled.Value)
                return;

            if (isOnAWidget || lockedOnAWidget)
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
                    if (UIElement.UIEnabled.Value)
                        VRInput.SendHaptic(VRInput.rightController, 0.005f, intensity);
                }
            }
        }

        private void ReleaseUIEnabledGuard()
        {
            if (null != UIEnabled)
            {
                UIEnabled.Dispose();
                UIEnabled = null;
            }
        }

        void OnPrefabInstantiated(object sender, PrefabInstantiatedArgs args)
        {
            ResetCursor();
        }

        private void ResetCursor()
        {
            ReleaseUIEnabledGuard();
            ToolsUIManager.Instance.ShowTools(true);
            HideAllCursors();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (lockedOnAWidget)
                return;

            if (other.GetComponent<UIVolumeTag>() != null)
            {
                ReleaseUIEnabledGuard();
                if (CommandManager.IsUndoGroupOpened())
                {
                    UIEnabled = UIElement.UIEnabled.SetValue(false);
                    return;
                }

                // deactivate all tools
                //tools.SetActive(false);
                ToolsUIManager.Instance.ShowTools(false);
                SetCursorShape(0); // arrow

                isOutOfVolume = false;
            }
            else if (other.gameObject.tag == "UICollider")
            {
                isOnAWidget = true;
                widgetTransform = other.transform;
                widgetHit = other.GetComponent<UIElement>();
                if (UIElement.UIEnabled.Value)
                {
                    VRInput.SendHaptic(VRInput.rightController, 0.015f, 0.5f);
                    audioClick.Play();
                }

                isOutOfWidget = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<UIVolumeTag>() != null)
            {
                if (!lockedOnAWidget)
                {
                    ResetCursor();
                }
                isOutOfVolume = true;
            }
            else if (other.gameObject.tag == "UICollider")
            {
                if (!lockedOnAWidget)
                {
                    ReleaseWidget();
                }
                isOutOfWidget = true;
            }
        }

        private void ReleaseWidget()
        {
            isOnAWidget = false;
            widgetTransform = null;
            widgetHit = null;
            MeshFilter meshFilter = GetComponentInChildren<MeshFilter>(true);
            if (null != meshFilter)
            {
                meshFilter.gameObject.transform.localPosition = initialCursorLocalPosition;
            }
            SetCursorShape(0);
        }

        public bool IsLockedOnWidget()
        {
            return lockedOnAWidget;
        }

        public bool IsLockedOnThisWidget(Transform other)
        {
            return widgetTransform == other;
        }

        public void LockOnWidget(bool value)
        {
            if (value == lockedOnAWidget)
                return;

            if (value)
            {
                lockedOnAWidget = true;
            }
            else
            {
                lockedOnAWidget = false;
                if (isOutOfWidget)
                {
                    ReleaseWidget();
                }
                if (isOutOfVolume)
                {
                    ResetCursor();
                }
            }
        }

        // 0: arrow, 1: box
        private void SetCursorShape(int shape)
        {
            currentShapeId = shape;

            HideAllCursors();
            switch (shape)
            {
                case 0: arrowCursor.gameObject.SetActive(true); currentShapeTransform = arrowCursor.transform; break;
                case 1: grabberCursor.gameObject.SetActive(true); currentShapeTransform = grabberCursor.transform; break;
            }
        }
        
        public void PushCursorShape(int shape)
        {
            previousShapeId = currentShapeId;
            SetCursorShape(shape);
        }

        public void PopCursorShape()
        {
            SetCursorShape(previousShapeId);
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