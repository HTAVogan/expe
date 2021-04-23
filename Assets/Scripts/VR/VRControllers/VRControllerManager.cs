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
            public Transform joystickDisplay;
            public Transform mouthpieceHolder;
            public Transform laserHolder;
            public Transform upAxis;
            public Transform paletteHolder;
            public Transform helperHolder;

            public VRController(string rootPath, Transform toolsPalette)
            {
                controllerTransform = toolsPalette.Find(rootPath);
                controllerDisplay = controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
                mouthpieceHolder = controllerTransform.Find("MouthpieceHolder");
                laserHolder = controllerTransform.Find("LaserHolder");
                upAxis = controllerTransform.Find("UpAxis");
                paletteHolder = controllerTransform.Find("PaletteHolder");
                helperHolder = controllerTransform.Find("HelperHolder");
                GetControllerTooltips();
            }

            private void GetControllerTooltips()
            {
                gripDisplay = controllerTransform.Find("GripButtonAnchor/Tooltip");
                triggerDisplay = controllerTransform.Find("TriggerButtonAnchor/Tooltip");
                primaryButtonDisplay = controllerTransform.Find("PrimaryButtonAnchor/Tooltip");
                secondaryButtonDisplay = controllerTransform.Find("SecondaryButtonAnchor/Tooltip");
                joystickDisplay = controllerTransform.Find("JoystickBaseAnchor/Tooltip");
            }
        }

        private VRController rightController;
        private VRController inverseRightController;
        private VRController leftController;
        private VRController inverseLeftController;

        public enum ControllerModel { Index, Quest, Quest2 }


        internal void InitializeControllers(string name)
        {
            switch (name)
            {
                case "Index Controller OpenXR": InitializeControllers(ControllerModel.Index); break;
                case "Oculus Rift S": InitializeControllers(ControllerModel.Quest); break;
                case "Quest": InitializeControllers(ControllerModel.Quest); break;
                case "Quest2": InitializeControllers(ControllerModel.Quest2); break;
            }
            SetRightHanded(GlobalState.Settings.rightHanded);
        }

        public void InitializeControllers(ControllerModel model)
        {
            Debug.Log("init controller " + model.ToString());
            if (null != rightController) rightController.controllerTransform.gameObject.SetActive(false);
            if (null != leftController) leftController.controllerTransform.gameObject.SetActive(false);
            if (null != inverseRightController) inverseRightController.controllerTransform.gameObject.SetActive(false);
            if (null != inverseLeftController) inverseLeftController.controllerTransform.gameObject.SetActive(false);
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
            if (rightController == null) InitializeControllers(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return rightController.controllerTransform;
            else return inverseLeftController.controllerTransform;
        }

        public Transform GetSecondaryControllerTransform()
        {
            if (GlobalState.Settings.rightHanded) return leftController.controllerTransform;
            else return inverseRightController.controllerTransform;
        }

        public Vector3 GetPrimaryControllerUp()
        {
            if (rightController == null) InitializeControllers(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return rightController.upAxis.up;
            else return inverseLeftController.upAxis.up;
        }

        public Vector3 GetSecondaryControllerUp()
        {
            if (GlobalState.Settings.rightHanded) return leftController.upAxis.up;
            else return inverseRightController.upAxis.up;
        }

        public void SetRightHanded(bool value)
        {
            rightController.controllerTransform.gameObject.SetActive(value);
            inverseRightController.controllerTransform.gameObject.SetActive(!value);
            leftController.controllerTransform.gameObject.SetActive(value);
            inverseLeftController.controllerTransform.gameObject.SetActive(!value);

            Transform toolsController = GlobalState.Instance.toolsController;
            Transform paletteController = GlobalState.Instance.paletteController;
          

            // Update controller's displays
            rightController.controllerDisplay.text = "";
            inverseRightController.controllerDisplay.text = "";
            leftController.controllerDisplay.text = "";
            inverseLeftController.controllerDisplay.text = "";

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
            Vector3 currentPalettePosition = paletteController.localPosition;
            if (value)
            {
                palette.SetPositionAndRotation(leftController.paletteHolder.position, leftController.paletteHolder.rotation);
                toolsController.Find("mouthpieces").SetPositionAndRotation(rightController.mouthpieceHolder.position, rightController.mouthpieceHolder.rotation);
                toolsController.Find("SelectionHelper").SetPositionAndRotation(rightController.helperHolder.position, rightController.helperHolder.rotation);
                paletteController.Find("SceneHelper").SetPositionAndRotation(leftController.helperHolder.position, leftController.helperHolder.rotation);
            }
            else
            {
                palette.SetPositionAndRotation(inverseRightController.paletteHolder.position, inverseRightController.paletteHolder.rotation);
                toolsController.Find("mouthpieces").SetPositionAndRotation(inverseLeftController.mouthpieceHolder.position, inverseLeftController.mouthpieceHolder.rotation);
                toolsController.Find("SelectionHelper").SetPositionAndRotation(inverseLeftController.helperHolder.position, inverseLeftController.helperHolder.rotation);
                paletteController.Find("SceneHelper").SetPositionAndRotation(inverseRightController.helperHolder.position, inverseRightController.helperHolder.rotation);
            }
        }


        private void GetControllersValues(string controllerPath)
        {
            Transform toolsController = GlobalState.Instance.toolsController;
            Transform paletteController = GlobalState.Instance.paletteController;

            rightController = new VRController(controllerPath + "/right_controller", toolsController);
            rightController.controllerTransform.gameObject.SetActive(true);

            inverseRightController = new VRController(controllerPath + "/right_controller", paletteController);

            leftController = new VRController(controllerPath + "/left_controller", paletteController);
            leftController.controllerTransform.gameObject.SetActive(true);

            inverseLeftController = new VRController(controllerPath + "/left_controller", toolsController);
        }

        private void GetIndexControllersValues()
        {
            Transform toolsController = GlobalState.Instance.toolsController;
            Transform paletteController = GlobalState.Instance.paletteController;

            rightController = new VRController("ValveIndex/IndexRightPivot/Index_controller_Right", toolsController);
            toolsController.Find("mouthpieces").position = rightController.mouthpieceHolder.position;
            rightController.controllerTransform.gameObject.SetActive(true);

            inverseRightController = new VRController("ValveIndex/IndexRightPivot/Index_controller_Right", paletteController);

            leftController = new VRController("ValveIndex/IndexLeftPivot/Index_controller_Left", paletteController);
            leftController.controllerTransform.gameObject.SetActive(true);

            inverseLeftController = new VRController("ValveIndex/IndexLeftPivot/Index_controller_Left", toolsController);
        }

        internal Transform GetPrimaryTooltipTransform(Tooltips.Location location)
        {
            if (rightController == null) InitializeControllers(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return GetTooltipTransform(rightController, location);
            else return GetTooltipTransform(inverseLeftController, location);
        }


        internal Transform GetSecondaryTooltipTransform(Tooltips.Location location)
        {
            if (GlobalState.Settings.rightHanded) return GetTooltipTransform(leftController, location);
            else return GetTooltipTransform(inverseRightController, location);
        }

        public TextMeshProUGUI GetPrimaryDisplay()
        {
            if (rightController == null) InitializeControllers(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return rightController.controllerDisplay;
            else return inverseLeftController.controllerDisplay;
        }
        public TextMeshProUGUI GetSecondaryDisplay()
        {
            if (GlobalState.Settings.rightHanded) return leftController.controllerDisplay;
            else return inverseRightController.controllerDisplay;
        }

        private Transform GetTooltipTransform(VRController controller, Tooltips.Location location)
        {
            switch (location)
            {
                case Tooltips.Location.Grip: return controller.gripDisplay;
                case Tooltips.Location.Joystick: return controller.joystickDisplay;
                case Tooltips.Location.Primary: return controller.primaryButtonDisplay;
                case Tooltips.Location.Secondary: return controller.secondaryButtonDisplay;
                case Tooltips.Location.Trigger: return controller.triggerDisplay;
                default: return null;
            }
        }

        internal Transform GetLaser()
        {
            if (GlobalState.Settings.rightHanded) return rightController.laserHolder;
            else return inverseLeftController.laserHolder;
        }

    }
}
