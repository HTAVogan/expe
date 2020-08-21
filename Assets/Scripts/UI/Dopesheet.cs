﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VRtist
{
    public class SetKeyInfo
    {
        public string objectName;
        public string channelName;
        public int channelIndex;
        public int frame;
        public float value;
    };

    public class Dopesheet : MonoBehaviour
    {
        [SpaceHeader("Sub Widget Refs", 6, 0.8f, 0.8f, 0.8f)]
        [SerializeField] private Transform mainPanel = null;
        [SerializeField] private UITimeBar timeBar = null;
        [SerializeField] private UILabel firstFrameLabel = null;
        [SerializeField] private UILabel lastFrameLabel = null;
        [SerializeField] private UILabel currentFrameLabel = null;
        private UILabel titleBar = null;

        UICheckbox montage = null;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public IntChangedEvent onAddKeyframeEvent = new IntChangedEvent();
        public IntChangedEvent onRemoveKeyframeEvent = new IntChangedEvent();
        public IntChangedEvent onPreviousKeyframeEvent = new IntChangedEvent();
        public IntChangedEvent onNextKeyframeEvent = new IntChangedEvent();
        public IntChangedEvent onChangeCurrentKeyframeEvent = new IntChangedEvent();

        private int firstFrame = 0;
        private int lastFrame = 250;
        private int currentFrame = 0;

        public int FirstFrame { get { return firstFrame; } set { firstFrame = value; UpdateFirstFrame(); } }
        public int LastFrame { get { return lastFrame; } set { lastFrame = value; UpdateLastFrame(); } }
        public int CurrentFrame { get { return currentFrame; } set { currentFrame = value; UpdateCurrentFrame(); } }

        private GameObject keyframePrefab;
        private ParametersController controller = null;

        public class AnimKey
        {
            public AnimKey(string name, float value)
            {
                this.name = name;
                this.value = value;
            }
            public string name;
            public float value;
        }

        private SortedList<int, List<AnimKey>> keys = new SortedList<int, List<AnimKey>>();
        private bool listenerAdded = false;

        void Start()
        {
            mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                timeBar = mainPanel.Find("TimeBar").GetComponent<UITimeBar>();
                firstFrameLabel = mainPanel.Find("FirstFrameLabel").GetComponent<UILabel>();
                lastFrameLabel = mainPanel.Find("LastFrameLabel").GetComponent<UILabel>();
                currentFrameLabel = mainPanel.Find("CurrentFrameLabel").GetComponent<UILabel>();
                titleBar = transform.parent.Find("TitleBar").GetComponent<UILabel>();
                keyframePrefab = Resources.Load<GameObject>("Prefabs/UI/DOPESHEET/Keyframe");

                montage = mainPanel.Find("Montage").GetComponent<UICheckbox>();
                ShotManager.Instance.MontageModeChangedEvent.AddListener(OnMontageModeChanged);

                GlobalState.Instance.onPlayingEvent.AddListener(OnPlayingChanged);
                GlobalState.Instance.onRecordEvent.AddListener(OnRecordingChanged);                
            }
        }

        private void OnPlayingChanged(bool value)
        {
            if (GlobalState.Instance.recordState != GlobalState.RecordState.Recording)
            {
                titleBar.Pushed = value;
            }
            else
            {
                titleBar.Pushed = false;
            }
        }

        private void OnRecordingChanged(bool value)
        {
            titleBar.Pushed = false;
            titleBar.Hovered = value;
        }

        private void Update()
        {
            bool enable = transform.localScale.x != 0f;
            if(enable)
            {
                if (!listenerAdded)
                    GlobalState.Instance.AddAnimationListener(OnParametersChanged);
            }
            else
            {
                if (listenerAdded)
                    GlobalState.Instance.RemoveAnimationListener(OnParametersChanged);
            }
            listenerAdded = enable;

            if (FirstFrame != GlobalState.startFrame)
            {
                FirstFrame = GlobalState.startFrame;
            }

            if (LastFrame != GlobalState.endFrame)
            {
                LastFrame = GlobalState.endFrame;
            }

            if (CurrentFrame != GlobalState.currentFrame)
            {
                CurrentFrame = GlobalState.currentFrame;
            }
        }

        private void UpdateFirstFrame()
        {
            if (firstFrameLabel != null)
            {
                firstFrameLabel.Text = firstFrame.ToString();
            }
            if (timeBar != null)
            {
                timeBar.MinValue = firstFrame; // updates knob position
            }
        }

        private void UpdateLastFrame()
        {
            if (lastFrameLabel != null)
            {
                lastFrameLabel.Text = lastFrame.ToString();
            }
            if (timeBar != null)
            {
                timeBar.MaxValue = lastFrame; // updates knob position
            }
        }

        private void UpdateCurrentFrame()
        {
            if (currentFrameLabel != null)
            {
                currentFrameLabel.Text = currentFrame.ToString();
            }
            if (timeBar != null)
            {
                timeBar.Value = currentFrame; // changes the knob's position
            }
        }

        public void Show(bool doShow)
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(doShow);
            }
        }

        private void OnMontageModeChanged()
        {
            montage.Checked = ShotManager.Instance.MontageMode;
        }

        protected virtual void OnParametersChanged(GameObject gObject)
        {
            if (null == controller || gObject != controller.gameObject)
                return;
            Clear();

            Dictionary<string, AnimationChannel> channels = GlobalState.Instance.GetAnimationChannels(gObject);
            if (null == channels)
                return;
            foreach (AnimationChannel channel in channels.Values)
            {
                foreach (AnimationKey key in channel.keys)
                {
                    List<AnimKey> keyList = null;
                    if (!keys.TryGetValue(key.time, out keyList))
                    {
                        keyList = new List<AnimKey>();
                        keys[key.time] = keyList;
                    }

                    keyList.Add(new AnimKey(channel.name, key.value));
                }
            }

            TextMeshPro trackLabel = transform.Find("MainPanel/Tracks/Summary/Label/Canvas/Text").GetComponent<TextMeshPro>();
            trackLabel.text = gObject.name;

            Transform keyframes = transform.Find("MainPanel/Tracks/Summary/Keyframes");
            UILabel track = keyframes.gameObject.GetComponent<UILabel>();
            foreach (int time in keys.Keys)
            {
                GameObject keyframe = GameObject.Instantiate(keyframePrefab, keyframes);

                float currentValue = (float)time;
                float pct = (float)(currentValue - firstFrame) / (float)(lastFrame - firstFrame);

                float startX = 0.0f;
                float endX = timeBar.width;
                float posX = startX + pct * (endX - startX);

                Vector3 knobPosition = new Vector3(posX, -0.5f * track.height, 0.0f);

                keyframe.transform.localPosition = knobPosition;
                if (time < FirstFrame || time > LastFrame)
                {
                    keyframe.SetActive(false); // clip out of range keyframes
                }
            }
        }

        public void UpdateFromController(ParametersController controller)
        {            
            this.controller = controller;
            if (this.controller != null)
            {
                OnParametersChanged(controller.gameObject);
            }
            else
            {
                Clear();
            }
        }

        public int GetNextKeyFrame()
        {
            foreach (int t in keys.Keys)
            {
                // TODO: dichotomic search
                if (t > CurrentFrame)
                    return t;
            }

            return FirstFrame;
        }

        public int GetPreviousKeyFrame()
        {
            for(int i = keys.Keys.Count - 1; i >= 0; i--)
            {
                // TODO: dichotomic search
                int t = keys.Keys[i];
                if (t < CurrentFrame)
                    return t;
            }

            return LastFrame;
        }

        public void Clear()
        {
            Transform tracks = transform.Find("MainPanel/Tracks");
            for(int i = 0; i < tracks.childCount; ++i)
            {
                Transform track = tracks.GetChild(i);
                string channelName = track.name;
                string trackName = $"MainPanel/Tracks/{channelName}/Keyframes";
                Transform keyframes = transform.Find(trackName);
                for (int j = keyframes.childCount - 1; j >= 0; j--)
                {
                    Destroy(keyframes.GetChild(j).gameObject);
                }
            }

            keys.Clear();
        }
        
        // called by the slider when moved
        public void OnChangeCurrentFrame(int i)
        {
            CurrentFrame = i;

            FrameInfo info = new FrameInfo() { frame = i };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);

            onChangeCurrentKeyframeEvent.Invoke(i);
        }

        public void OnPrevKeyFrame()
        {
            onPreviousKeyframeEvent.Invoke(CurrentFrame);
        }

        public void OnNextKeyFrame()
        {
            onNextKeyframeEvent.Invoke(CurrentFrame);
        }

        public void OnAddKeyFrame()
        {
            GlobalState.Instance.AddKeyframe();
        }

        public void OnRemoveKeyFrame()
        {
            GlobalState.Instance.RemoveKeyframe();
        }
    }
}
