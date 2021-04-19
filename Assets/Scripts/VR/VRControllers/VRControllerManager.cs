using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class VRControllerManager : MonoBehaviour
    {
        [System.Serializable]
        public class VRController
        {
            public Transform controllerTransform;
            public TextMeshProUGUI controllerDisplay;
            public Transform gripDisplay;
            public Transform triggerDisplay;
            public Transform primaryButtonDisplay;
            public Transform secondaryButtonDisplay;
            public Transform JoystickDisplay;
            public Transform MouthpieceHolder;
            public Transform LaserHolder;
        }
        private VRController CurrentRightController;
        private VRController InverseRightController;
        private VRController CurrentLeftController;
        private VRController InverseLeftController;

        public enum ControllerModel { Index, Quest, Quest2 }

        public void Start()
        {
        }

        internal void InitializeControllers(string name)
        {
            switch (name)
            {
                case "Index Controller OpenXR": InitializeControllers(ControllerModel.Index); break;

            }
        }
        public void InitializeControllers(ControllerModel model)
        {
            Debug.Log("init controller " + model.ToString());
            if (null != CurrentRightController) CurrentRightController.controllerTransform.gameObject.SetActive(false);
            if (null != CurrentLeftController) CurrentLeftController.controllerTransform.gameObject.SetActive(false);
            if (null != InverseRightController) InverseRightController.controllerTransform.gameObject.SetActive(false);
            if (null != InverseLeftController) InverseLeftController.controllerTransform.gameObject.SetActive(false);
            switch (model)
            {
                case ControllerModel.Index:
                    GetIndexControllersValues();
                    break;
                case ControllerModel.Quest:
                    GetControllersValues("OculusQuest");
                    break;
                case ControllerModel.Quest2: break;
            }
        }

        public Transform GetPrimaryControllerTransform()
        {
            if (CurrentRightController == null) InitializeControllers(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return CurrentRightController.controllerTransform;
            else return InverseLeftController.controllerTransform;
        }

        public Transform GetSecondaryControllerTransform()
        {
            if (GlobalState.Settings.rightHanded) return CurrentLeftController.controllerTransform;
            else return InverseRightController.controllerTransform;
        }

        public void SetRightHanded(bool value)
        {
            CurrentRightController.controllerTransform.gameObject.SetActive(value);
            InverseRightController.controllerTransform.gameObject.SetActive(!value);
            CurrentLeftController.controllerTransform.gameObject.SetActive(value);
            InverseLeftController.controllerTransform.gameObject.SetActive(!value);


            // Update controller's displays
            CurrentRightController.controllerDisplay.text = "";
            InverseRightController.controllerDisplay.text = "";
            CurrentLeftController.controllerDisplay.text = "";
            InverseLeftController.controllerDisplay.text = "";

            // Update tooltips
            Tooltips.HideAll(VRDevice.PrimaryController);
            Tooltips.HideAll(VRDevice.SecondaryController);
            ToolBase tool = ToolsManager.CurrentTool();
            if (null != tool)
            {
                tool.SetTooltips();
            }
            GlobalState.Instance.playerController.HandleCommonTooltipsVisibility();

            Transform palette = GlobalState.Instance.paletteController.Find("PaletteHandle");
            Vector3 currentPalettePosition = palette.localPosition;
            if (GlobalState.Settings.rightHanded)
                palette.localPosition = new Vector3(-0.02f, currentPalettePosition.y, currentPalettePosition.z);
            else
                palette.localPosition = new Vector3(-0.2f, currentPalettePosition.y, currentPalettePosition.z);
        }


        private void GetControllersValues(string controllerPath)
        {
            Transform toolsController = GlobalState.Instance.toolsController;
            Transform paletteController = GlobalState.Instance.paletteController;

            CurrentRightController = new VRController();
            CurrentRightController.controllerTransform = toolsController.Find(controllerPath + "/right_controller");
            CurrentRightController.controllerDisplay = CurrentRightController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            GetControllerTooltips(CurrentRightController);
            CurrentRightController.controllerTransform.gameObject.SetActive(true);

            InverseRightController = new VRController();
            InverseRightController.controllerTransform = paletteController.Find(controllerPath + "/right_controller");
            InverseRightController.controllerDisplay = InverseRightController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            GetControllerTooltips(InverseRightController);

            CurrentLeftController = new VRController();
            CurrentLeftController.controllerTransform = paletteController.Find(controllerPath + "/left_controller");
            CurrentLeftController.controllerDisplay = CurrentLeftController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            GetControllerTooltips(CurrentLeftController);
            CurrentLeftController.controllerTransform.gameObject.SetActive(true);

            InverseLeftController = new VRController();
            InverseLeftController.controllerTransform = toolsController.Find(controllerPath + "/left_controller");
            InverseLeftController.controllerDisplay = InverseLeftController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            GetControllerTooltips(InverseLeftController);
        }

        private void GetIndexControllersValues()
        {
            Transform toolsController = GlobalState.Instance.toolsController;
            Transform paletteController = GlobalState.Instance.paletteController;

            CurrentRightController = new VRController();
            CurrentRightController.controllerTransform = toolsController.Find("ValveIndex/IndexRightPivot/Index_controller_Right");
            CurrentRightController.controllerDisplay = CurrentRightController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            CurrentRightController.MouthpieceHolder = CurrentRightController.controllerTransform.Find("MouthpieceHolder");
            toolsController.Find("mouthpieces").position = CurrentRightController.MouthpieceHolder.position;
            CurrentRightController.LaserHolder = CurrentRightController.controllerTransform.Find("LaserHolder");
            GetControllerTooltips(CurrentRightController);
            CurrentRightController.controllerTransform.gameObject.SetActive(true);

            InverseRightController = new VRController();
            InverseRightController.controllerTransform = paletteController.Find("ValveIndex/IndexRightPivot/Index_controller_Right");
            InverseRightController.controllerDisplay = InverseRightController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            InverseRightController.MouthpieceHolder = InverseRightController.controllerTransform.Find("MouthpieceHolder");
            GetControllerTooltips(InverseRightController);

            CurrentLeftController = new VRController();
            CurrentLeftController.controllerTransform = paletteController.Find("ValveIndex/IndexLeftPivot/Index_controller_Left");
            CurrentLeftController.controllerDisplay = CurrentLeftController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            CurrentLeftController.MouthpieceHolder = CurrentLeftController.controllerTransform.Find("MouthpieceHolder");
            GetControllerTooltips(CurrentLeftController);
            CurrentLeftController.controllerTransform.gameObject.SetActive(true);

            InverseLeftController = new VRController();
            InverseLeftController.controllerTransform = toolsController.Find("ValveIndex/IndexLeftPivot/Index_controller_Left");
            InverseLeftController.controllerDisplay = InverseLeftController.controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
            InverseLeftController.MouthpieceHolder = InverseLeftController.controllerTransform.Find("MouthpieceHolder");
            GetControllerTooltips(InverseLeftController);

        }

        private void GetControllerTooltips(VRController controller)
        {
            Transform root = controller.controllerTransform;
            controller.gripDisplay = root.Find("GripButtonAnchor/Tooltip");
            controller.triggerDisplay = root.Find("TriggerButtonAnchor/Tooltip");
            controller.primaryButtonDisplay = root.Find("PrimaryButtonAnchor/Tooltip");
            controller.secondaryButtonDisplay = root.Find("SecondaryButtonAnchor/Tooltip");
            controller.JoystickDisplay = root.Find("JoystickBaseAnchor/Tooltip");

        }

        internal Transform GetPrimaryTooltipTransform(Tooltips.Location location)
        {
            if (CurrentRightController == null) InitializeControllers(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return GetTooltipTransform(CurrentRightController, location);
            else return GetTooltipTransform(InverseLeftController, location);
        }


        internal Transform GetSecondaryTooltipTransform(Tooltips.Location location)
        {
            if (GlobalState.Settings.rightHanded) return GetTooltipTransform(CurrentLeftController, location);
            else return GetTooltipTransform(InverseRightController, location);
        }
        private Transform GetTooltipTransform(VRController controller, Tooltips.Location location)
        {
            switch (location)
            {
                case Tooltips.Location.Grip: return controller.gripDisplay;
                case Tooltips.Location.Joystick: return controller.JoystickDisplay;
                case Tooltips.Location.Primary: return controller.primaryButtonDisplay;
                case Tooltips.Location.Secondary: return controller.secondaryButtonDisplay;
                case Tooltips.Location.Trigger: return controller.triggerDisplay;
                default: return null;
            }
        }

        internal Transform GetLaser()
        {
            if (GlobalState.Settings.rightHanded) return CurrentRightController.LaserHolder;
            else return InverseRightController.LaserHolder;
        }

    }
}
