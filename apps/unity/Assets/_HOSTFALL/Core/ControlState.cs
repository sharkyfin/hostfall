using System;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hostfall.Core
{
    public enum ControlMode { Exploring, Terminal, Paused, Field, MainMenu, Introduction, Map, Cinematic, Computer }
    public sealed class ControlState : MonoBehaviour
    {
        public event Action<ControlMode> Changed;
        public ControlMode Mode { get; private set; }
        public bool RidingTransit { get; private set; }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void Hostfall_SetPointerCapture(int wanted);
        [DllImport("__Internal")] private static extern int Hostfall_HasPointerCapture();
        private bool captured;

        public bool CanLookWithMouse => captured || Mouse.current?.rightButton.isPressed == true;
#else
        public bool CanLookWithMouse => true;
#endif
        public void SetTransitPassenger(bool aboard) => RidingTransit = aboard;
        // Headless runs have no native window; virtual Input System devices still need to exercise gameplay.
        public bool CanExplore => Mode == ControlMode.Exploring && !RidingTransit && (Application.isFocused || Application.isBatchMode || Hostfall.World.PerformanceReplay.Active);
        public bool WorldRuns => (Mode == ControlMode.Exploring || Mode == ControlMode.Computer) && (Application.isFocused || Application.isBatchMode || Hostfall.World.PerformanceReplay.Active);
        public void SetMode(ControlMode mode)
        {
            Mode = mode;
            Time.timeScale = mode == ControlMode.Exploring || mode == ControlMode.Computer ? 1 : 0;
            ApplyCursor(); Changed?.Invoke(mode);
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && (Mode == ControlMode.Exploring||Mode==ControlMode.Terminal||Mode==ControlMode.Computer) && !Application.isBatchMode && !Hostfall.World.PerformanceReplay.Active) SetMode(ControlMode.Paused);
            else ApplyCursor();
        }
        private void ApplyCursor()
        {
            bool locked = Application.isFocused && Mode == ControlMode.Exploring;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!locked) captured = false;
            Hostfall_SetPointerCapture(locked ? 1 : 0);
#else
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        private void LateUpdate()
        {
            if (Mode != ControlMode.Exploring) return;
            bool wasCaptured = captured;
            captured = Hostfall_HasPointerCapture() == 1;
            // Browsers can consume Escape while releasing a captured pointer.
            if (wasCaptured && !captured) SetMode(ControlMode.Paused);
        }
#else
        private void Update()
        {
            if (Mode == ControlMode.Exploring && Application.isFocused &&
                (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)) ApplyCursor();
        }
#endif
        private void OnDestroy()
        {
            Time.timeScale = 1;
#if UNITY_WEBGL && !UNITY_EDITOR
            Hostfall_SetPointerCapture(0);
#else
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#endif
        }
    }
}
