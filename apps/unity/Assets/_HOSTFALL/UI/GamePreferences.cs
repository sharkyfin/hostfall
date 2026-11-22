using Hostfall.Player;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.UI
{
    public interface IGameWindow
    {
        bool Fullscreen { get; set; }
    }
    public sealed class NativeGameWindow : IGameWindow
    {
        public bool Fullscreen
        {
            get => Screen.fullScreen;
            set { if (!Application.isEditor && !Application.isBatchMode && Screen.fullScreen != value) {if(value){Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);}else Screen.fullScreen=false;} }
        }
    }
    public sealed class GamePreferences
    {
        private readonly bool persistent;
        private readonly PlayerMotor player;
        private readonly IGameWindow window;
        public string NewGameLanguage { get; private set; }
        public float Sensitivity { get; private set; }
        public float Volume { get; private set; }
        public float MotionIntensity {get;private set;}=.5f;
        public float ScareIntensity { get; private set; } = 1;
        public bool Fullscreen => window.Fullscreen;
        public bool VSync { get; private set; }
        public GamePreferences(PlayerMotor motor, IGameWindow gameWindow = null)
        {
            player=motor; window=gameWindow??new NativeGameWindow(); persistent=!Application.isBatchMode;
            NewGameLanguage=persistent?PlayerPrefs.GetString("hostfall.language","python"):"python";
            if(NewGameLanguage!="cpp"&&NewGameLanguage!="python") NewGameLanguage="python";
#if UNITY_WEBGL && !UNITY_EDITOR
            Sensitivity=persistent?Mathf.Clamp(PlayerPrefs.GetFloat("hostfall.sensitivity",1.25f),.4f,2):1.25f;
            Volume=persistent?Mathf.Clamp01(PlayerPrefs.GetFloat("hostfall.volume",1)):1;
            VSync=persistent?PlayerPrefs.GetInt("hostfall.vsync",0)==1:false;
            MotionIntensity=persistent?Mathf.Clamp01(PlayerPrefs.GetFloat("hostfall.motion",0f)):0f;
#else
            Sensitivity=persistent?Mathf.Clamp(PlayerPrefs.GetFloat("hostfall.sensitivity",1),.4f,2):1;
            Volume=persistent?Mathf.Clamp01(PlayerPrefs.GetFloat("hostfall.volume",1)):1;
            VSync=persistent?PlayerPrefs.GetInt("hostfall.vsync",1)==1:true;
            MotionIntensity=persistent?Mathf.Clamp01(PlayerPrefs.GetFloat("hostfall.motion",.5f)):.5f;
#endif
            ScareIntensity = persistent ? Mathf.Clamp01(PlayerPrefs.GetFloat("hostfall.scare", 1)) : 1;
            Apply();
        }
        public void ChooseLanguage(string value)
        { NewGameLanguage=FieldSession.NormalizeLanguage(value); Save(); }
        public void SetSensitivity(float value) { Sensitivity=Mathf.Clamp(value,.4f,2); Apply(); Save(); }
        public void SetMotion(float value){MotionIntensity=Mathf.Clamp01(value);Apply();Save();}
        public void SetScareIntensity(float value) { ScareIntensity = Mathf.Clamp01(value); Apply(); Save(); }
        public void SetVolume(float value) { Volume=Mathf.Clamp01(value); Apply(); Save(); }
        // The OS/Unity own the window state. Loading a scene or changing another
        // preference must never reapply an old window mode or size.
        public void SetFullscreen(bool value) { if(window.Fullscreen!=value) window.Fullscreen=value; }
        public void SetVSync(bool value) { VSync=value; Apply(); Save(); }
#if UNITY_WEBGL && !UNITY_EDITOR
        public void Reset() { Sensitivity=1.25f; Volume=1; VSync=false;MotionIntensity=0f; ScareIntensity=1; Apply(); Save(); }
#else
        public void Reset() { Sensitivity=1; Volume=1; VSync=true;MotionIntensity=.5f; ScareIntensity=1; Apply(); Save(); }
#endif
        private void Apply()
        {
            player.LookSensitivity=Sensitivity;player.MotionIntensity=MotionIntensity;
            player.ScareIntensity = ScareIntensity;
            AudioListener.volume=Volume;
            if(!persistent) return;
            QualitySettings.vSyncCount=VSync?1:0;
        }
        private void Save()
        {
            if(!persistent) return;
            PlayerPrefs.SetString("hostfall.language",NewGameLanguage); PlayerPrefs.SetFloat("hostfall.sensitivity",Sensitivity);
            PlayerPrefs.SetFloat("hostfall.volume",Volume);
            PlayerPrefs.SetFloat("hostfall.scare", ScareIntensity);
            PlayerPrefs.SetFloat("hostfall.motion",MotionIntensity);PlayerPrefs.SetInt("hostfall.vsync",VSync?1:0); PlayerPrefs.Save();
        }
    }
}
