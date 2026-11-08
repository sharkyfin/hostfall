using System;
using UnityEngine;

namespace Hostfall.Core
{
    public enum ControlMode { Exploring, Terminal, Paused, Field, MainMenu, Introduction }
    public sealed class ControlState : MonoBehaviour
    {
        public event Action<ControlMode> Changed;
        public ControlMode Mode { get; private set; }
        // Headless runs have no native window; virtual Input System devices still need to exercise gameplay.
        public bool CanExplore => Mode == ControlMode.Exploring && (Application.isFocused || Application.isBatchMode);
        public void SetMode(ControlMode mode)
        {
            Mode = mode;
            Time.timeScale = mode == ControlMode.Exploring ? 1 : 0;
            ApplyCursor(); Changed?.Invoke(mode);
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && (Mode == ControlMode.Exploring||Mode==ControlMode.Terminal) && !Application.isBatchMode) SetMode(ControlMode.Paused);
            else ApplyCursor();
        }
        private void ApplyCursor()
        {
            bool locked = Application.isFocused && Mode == ControlMode.Exploring;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        private void Update()
        {
            if (Mode == ControlMode.Exploring && Application.isFocused)
            {
                if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        private void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
