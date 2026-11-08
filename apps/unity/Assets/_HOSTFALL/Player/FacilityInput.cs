using Hostfall.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hostfall.Player
{
    public sealed class FacilityInput : MonoBehaviour
    {
        private InputActionMap gameplay;
        private InputActionMap system;
        private ControlState state;
        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Dash { get; private set; }
        public InputAction Pulse { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction SwitchCamera { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Escape { get; private set; }
        public InputAction Journal { get; private set; }
        public InputAction Archive { get; private set; }
        public InputAction Save { get; private set; }
        public InputAction Load { get; private set; }
        public void Initialize(ControlState controls)
        {
            state = controls; gameplay = new InputActionMap("Exploration"); system = new InputActionMap("System");
            Move = gameplay.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");
            Look = gameplay.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            Sprint = gameplay.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            Jump = gameplay.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            Dash = gameplay.AddAction("Dash", InputActionType.Button, "<Keyboard>/q");
            Pulse = gameplay.AddAction("Pulse", InputActionType.Button, "<Keyboard>/f");
            Fire = gameplay.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            Fire.AddBinding("<Gamepad>/rightTrigger");
            SwitchCamera = gameplay.AddAction("Camera", InputActionType.Button, "<Keyboard>/v");
            Interact = gameplay.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            Escape = system.AddAction("Escape", InputActionType.Button, "<Keyboard>/escape");
            Journal = system.AddAction("Journal", InputActionType.Button, "<Keyboard>/j");
            Archive = system.AddAction("Archive", InputActionType.Button, "<Keyboard>/k");
            Save = system.AddAction("Save", InputActionType.Button, "<Keyboard>/f5");
            Load = system.AddAction("Load", InputActionType.Button, "<Keyboard>/f9");
            state.Changed += OnModeChanged; system.Enable(); OnModeChanged(state.Mode);
        }
        private void OnModeChanged(ControlMode mode)
        {
            if (mode == ControlMode.Exploring) gameplay.Enable(); else gameplay.Disable();
        }
        private void OnDestroy()
        {
            if (state != null) state.Changed -= OnModeChanged;
            gameplay?.Dispose(); system?.Dispose();
        }
    }
}
