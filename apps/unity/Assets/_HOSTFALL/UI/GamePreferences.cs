using Hostfall.Player;
using Hostfall.Simulation;
using UnityEngine;

namespace Hostfall.UI
{
    public sealed class GamePreferences
    {
        private readonly bool persistent;
        private readonly PlayerMotor player;
        public string NewGameLanguage { get; private set; }
        public float Sensitivity { get; private set; }
        public float Volume { get; private set; }
        public bool Fullscreen { get; private set; }
        public bool VSync { get; private set; }
        public GamePreferences(PlayerMotor motor)
        {
            player=motor; persistent=!Application.isBatchMode;
            NewGameLanguage=persistent?PlayerPrefs.GetString("hostfall.language","python"):"python";
            if(NewGameLanguage!="cpp"&&NewGameLanguage!="python") NewGameLanguage="python";
            Sensitivity=persistent?Mathf.Clamp(PlayerPrefs.GetFloat("hostfall.sensitivity",1),.4f,2):1;
            Volume=persistent?Mathf.Clamp01(PlayerPrefs.GetFloat("hostfall.volume",1)):1;
            Fullscreen=persistent?PlayerPrefs.GetInt("hostfall.fullscreen",Screen.fullScreen?1:0)==1:Screen.fullScreen;
            VSync=persistent?PlayerPrefs.GetInt("hostfall.vsync",1)==1:true;
            Apply();
        }
        public void ChooseLanguage(string value)
        { NewGameLanguage=FieldSession.NormalizeLanguage(value); Save(); }
        public void SetSensitivity(float value) { Sensitivity=Mathf.Clamp(value,.4f,2); Apply(); Save(); }
        public void SetVolume(float value) { Volume=Mathf.Clamp01(value); Apply(); Save(); }
        public void SetFullscreen(bool value) { Fullscreen=value; Apply(); Save(); }
        public void SetVSync(bool value) { VSync=value; Apply(); Save(); }
        public void Reset() { Sensitivity=1; Volume=1; VSync=true; Apply(); Save(); }
        private void Apply()
        {
            player.LookSensitivity=Sensitivity;
            AudioListener.volume=Volume;
            if(!persistent) return;
            QualitySettings.vSyncCount=VSync?1:0;
            if(Screen.fullScreen!=Fullscreen) Screen.fullScreen=Fullscreen;
        }
        private void Save()
        {
            if(!persistent) return;
            PlayerPrefs.SetString("hostfall.language",NewGameLanguage); PlayerPrefs.SetFloat("hostfall.sensitivity",Sensitivity);
            PlayerPrefs.SetFloat("hostfall.volume",Volume);
            PlayerPrefs.SetInt("hostfall.fullscreen",Fullscreen?1:0); PlayerPrefs.SetInt("hostfall.vsync",VSync?1:0); PlayerPrefs.Save();
        }
    }
}
