﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter)),
     RequireComponent(typeof(MeshRenderer))]
    public class UIVerticalSliderRail : MonoBehaviour
    {
        public float width;
        public float height;
        public float thickness;
        public float margin;

        public Color _color;
        public Color Color { get { return _color; } set { _color = value; ApplyColor(_color); } }

        void Awake()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#else
            if (Application.isPlaying)
#endif
            {
                width = GetComponent<MeshFilter>().mesh.bounds.size.x;
                height = GetComponent<MeshFilter>().mesh.bounds.size.y;
                thickness = GetComponent<MeshFilter>().mesh.bounds.size.z;
            }
        }

        public void RebuildMesh(float newWidth, float newHeight, float newThickness, float newMargin)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            Mesh theNewMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
            theNewMesh.name = "UISliderRail_GeneratedMesh";
            meshFilter.sharedMesh = theNewMesh;

            width = newWidth;
            height = newHeight;
            thickness = newThickness;
            margin = newMargin;
        }

        private void ApplyColor(Color c)
        {
            GetComponent<MeshRenderer>().sharedMaterial.SetColor("_BaseColor", c);
        }

        public static UIVerticalSliderRail Create(
            string objectName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float thickness,
            float margin,
            Material material,
            Color c)
        {
            GameObject go = new GameObject(objectName);
            go.tag = "UICollider";

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (parent)
            {
                UIElement elem = parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIVerticalSliderRail uiSliderRail = go.AddComponent<UIVerticalSliderRail>();
            uiSliderRail.transform.parent = parent;
            uiSliderRail.transform.localPosition = parentAnchor + relativeLocation;
            uiSliderRail.transform.localRotation = Quaternion.identity;
            uiSliderRail.transform.localScale = Vector3.one;
            uiSliderRail.width = width;
            uiSliderRail.height = height;
            uiSliderRail.thickness = thickness;
            uiSliderRail.margin = margin;

            // Setup the Meshfilter
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = UIUtils.BuildRoundedBox(width, height, margin, thickness);
                //BuildHollowCubeEx(width, height, margin, thickness);
            }

            // Setup the MeshRenderer
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null && material != null)
            {
                Material newMaterial = Instantiate(material);
                newMaterial.name = "UIVerticalSliderRail_Material";
                meshRenderer.sharedMaterial = newMaterial;
                uiSliderRail.Color = c;

                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.renderingLayerMask = 2; // "LightLayer 1"
            }

            return uiSliderRail;
        }
    }
}