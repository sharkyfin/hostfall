using Hostfall.Player;
using Hostfall.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hostfall.Tests
{
    public sealed class GamePreferencesTests
    {
        private sealed class Window : IGameWindow
        {
            public bool NativeFullscreen;
            public int Changes;
            public bool Fullscreen { get=>NativeFullscreen; set { NativeFullscreen=value; Changes++; } }
        }
        [Test] public void SceneReloadAndOtherSettingsPreserveNativeWindowMode()
        {
            var go=new GameObject("Preferences test player");
            float volume=AudioListener.volume;
            int vsync=QualitySettings.vSyncCount;
            try
            {
                var motor=go.AddComponent<PlayerMotor>();
                var window=new Window { NativeFullscreen=true };
                var preferences=new GamePreferences(motor,window);
                preferences.SetVolume(.4f);
                preferences.SetSensitivity(1.2f);
                preferences.SetVSync(false);
                preferences.ChooseLanguage("cpp");
                preferences.Reset();
                var reloaded=new GamePreferences(motor,window);
                Assert.That(reloaded.Fullscreen,Is.True);
                Assert.That(window.Changes,Is.Zero,"Scene loading and unrelated preferences must not change native window mode");
                window.NativeFullscreen=false; // OS window button, outside the game settings.
                Assert.That(reloaded.Fullscreen,Is.False,"Settings must reflect changes made through the OS");
                Assert.That(new GamePreferences(motor,window).Fullscreen,Is.False);
                Assert.That(window.Changes,Is.Zero);
                reloaded.SetFullscreen(true);
                Assert.That(window.Changes,Is.EqualTo(1));
                reloaded.SetFullscreen(true);
                Assert.That(window.Changes,Is.EqualTo(1),"Repeated application must not restart a native fullscreen transition");
            }
            finally { Object.DestroyImmediate(go); AudioListener.volume=volume; QualitySettings.vSyncCount=vsync; }
        }
    }
}
