// GENERATED AUTOMATICALLY FROM 'Assets/Resources/ExpeResources/ActionCounter.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @ActionCounter : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @ActionCounter()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""ActionCounter"",
    ""maps"": [
        {
            ""name"": ""CounterInGame"",
            ""id"": ""1fb37b73-3c5b-46bf-8182-772a95e01f4c"",
            ""actions"": [
                {
                    ""name"": ""New action"",
                    ""type"": ""Button"",
                    ""id"": ""7a300aaf-8f81-4396-b594-dc64c3ad27a7"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""RighClick"",
                    ""type"": ""Button"",
                    ""id"": ""a6f87a02-b805-45c1-80f9-f39d603e4053"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ScrollClick"",
                    ""type"": ""Button"",
                    ""id"": ""db84a7b4-674d-427b-9477-bb9e7436a712"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""f42aa585-894f-455a-829e-e48546a72c8f"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""New action"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f340a895-2c31-424c-991c-f3fcc442b6b8"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RighClick"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f66501a1-d407-47e6-96fe-4e63b0c2847b"",
                    ""path"": ""<Mouse>/middleButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ScrollClick"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // CounterInGame
        m_CounterInGame = asset.FindActionMap("CounterInGame", throwIfNotFound: true);
        m_CounterInGame_Newaction = m_CounterInGame.FindAction("New action", throwIfNotFound: true);
        m_CounterInGame_RighClick = m_CounterInGame.FindAction("RighClick", throwIfNotFound: true);
        m_CounterInGame_ScrollClick = m_CounterInGame.FindAction("ScrollClick", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // CounterInGame
    private readonly InputActionMap m_CounterInGame;
    private ICounterInGameActions m_CounterInGameActionsCallbackInterface;
    private readonly InputAction m_CounterInGame_Newaction;
    private readonly InputAction m_CounterInGame_RighClick;
    private readonly InputAction m_CounterInGame_ScrollClick;
    public struct CounterInGameActions
    {
        private @ActionCounter m_Wrapper;
        public CounterInGameActions(@ActionCounter wrapper) { m_Wrapper = wrapper; }
        public InputAction @Newaction => m_Wrapper.m_CounterInGame_Newaction;
        public InputAction @RighClick => m_Wrapper.m_CounterInGame_RighClick;
        public InputAction @ScrollClick => m_Wrapper.m_CounterInGame_ScrollClick;
        public InputActionMap Get() { return m_Wrapper.m_CounterInGame; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CounterInGameActions set) { return set.Get(); }
        public void SetCallbacks(ICounterInGameActions instance)
        {
            if (m_Wrapper.m_CounterInGameActionsCallbackInterface != null)
            {
                @Newaction.started -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnNewaction;
                @Newaction.performed -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnNewaction;
                @Newaction.canceled -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnNewaction;
                @RighClick.started -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnRighClick;
                @RighClick.performed -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnRighClick;
                @RighClick.canceled -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnRighClick;
                @ScrollClick.started -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnScrollClick;
                @ScrollClick.performed -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnScrollClick;
                @ScrollClick.canceled -= m_Wrapper.m_CounterInGameActionsCallbackInterface.OnScrollClick;
            }
            m_Wrapper.m_CounterInGameActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Newaction.started += instance.OnNewaction;
                @Newaction.performed += instance.OnNewaction;
                @Newaction.canceled += instance.OnNewaction;
                @RighClick.started += instance.OnRighClick;
                @RighClick.performed += instance.OnRighClick;
                @RighClick.canceled += instance.OnRighClick;
                @ScrollClick.started += instance.OnScrollClick;
                @ScrollClick.performed += instance.OnScrollClick;
                @ScrollClick.canceled += instance.OnScrollClick;
            }
        }
    }
    public CounterInGameActions @CounterInGame => new CounterInGameActions(this);
    public interface ICounterInGameActions
    {
        void OnNewaction(InputAction.CallbackContext context);
        void OnRighClick(InputAction.CallbackContext context);
        void OnScrollClick(InputAction.CallbackContext context);
    }
}
