/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;


using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    /// <summary>
    /// Global states of the app.
    /// </summary>
    public class GlobalStateTradi : MonoBehaviour
    {
        [Header("Parameters")]
        public static Dictionary<GameObject, List<Vector3>> translations;
        private static Dictionary<GameObject, List<Quaternion>> rotations;
        public static AnimationEngineTradi Animation { get { return AnimationEngineTradi.Instance; } }


        // FPS
        public static int Fps { get; private set; }
        private static int fpsFrameRange = 60;
        private static int[] fpsBuffer = null;
        private static int fpsBufferIndex = 0;
        public bool isReadyToLoad;






        // Geometry Importer
        private GeometryImporter geometryImporter;
        public static GeometryImporter GeometryImporter
        {
            get { return Instance.geometryImporter; }
        }



        // Singleton
        private static GlobalStateTradi instance = null;
        public static GlobalStateTradi Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<GlobalStateTradi>();
                }
                return instance;
            }
        }

        void Awake()
        {
            instance = Instance;

        

            geometryImporter = GetComponent<GeometryImporter>();
        }


        private void Start()
        {
            translations = new Dictionary<GameObject, List<Vector3>>();
            isReadyToLoad = true;
        }

        private void UpdateFps()
        {
 

            // Initialize
            if (null == fpsBuffer || fpsBuffer.Length != fpsFrameRange)
            {
                if (fpsFrameRange <= 0) { fpsFrameRange = 1; }
                fpsBuffer = new int[fpsFrameRange];
                fpsBufferIndex = 0;
            }

            // Bufferize
            fpsBuffer[fpsBufferIndex] = (int)(1f / Time.unscaledDeltaTime);
            ++fpsBufferIndex;
            if (fpsBufferIndex >= fpsFrameRange)
            {
                fpsBufferIndex = 0;
            }

            // Calculate mean fps
            int sum = 0;
            for (int i = 0; i < fpsFrameRange; ++i)
            {
                sum += fpsBuffer[i];
            }
            Fps = sum / fpsFrameRange;
        }

        public static void SetDisplayGizmos(bool value)
        {
            SetGizmosVisible(FindObjectsOfType<LightController>(), value);
            SetGizmosVisible(FindObjectsOfType<CameraController>(), value);
            SetGizmosVisible(FindObjectsOfType<ConstraintLineController>(), value);
            SetDisplayAvatars(value);
        }

        public static void SetDisplayLocators(bool value)
        {
            SetGizmosVisible(FindObjectsOfType<LocatorController>(), value);
        }

        public static void SetDisplayAvatars(bool value)
        {
            SetGizmosVisible(FindObjectsOfType<AvatarController>(), value);
        }

        public static void SetGizmoVisible(GameObject gObject, bool value)
        {
            // Disable colliders
            Collider[] colliders = gObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                collider.enabled = value;
            }

            // Hide geometry
            MeshFilter[] meshFilters = gObject.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                meshFilter.gameObject.SetActive(value);
            }

            // Hide UI
            Canvas[] canvases = gObject.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                canvas.gameObject.SetActive(value);
            }
        }

        public static void SetGizmosVisible(IGizmo[] gizmos, bool value)
        {
            foreach (var gizmo in gizmos)
            {
                gizmo.SetGizmoVisible(value);
            }
        }

    }
}
