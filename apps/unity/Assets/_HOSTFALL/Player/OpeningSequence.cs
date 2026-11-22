using System;
using Hostfall.CameraSystem;
using Hostfall.Core;
using UnityEngine;
using UnityEngine.UIElements;
namespace Hostfall.Player
{
    public sealed class OpeningSequence : MonoBehaviour
    {
        private PlayerCameraRig rig;private ControlState controls;private PlayerMotor player;private VisualElement overlay,shade;private Label subtitle;private AudioSource voice;private Action complete;
        private float elapsed,duration;private bool pending,playing,interactive;private Vector3 start;private Quaternion view;
        public bool Playing=>playing;
        public void Initialize(PlayerCameraRig cameras,ControlState state,PlayerMotor motor,VisualElement root,bool fresh,Action finished,bool newStory=false)
        {
            rig=cameras;controls=state;player=motor;pending=fresh;complete=finished;interactive=newStory;overlay=root.Q("awakening");shade=root.Q("awakening-shade");subtitle=root.Q<Label>("awakening-subtitle");root.Q<Button>("skip-awakening").clicked+=Skip;
        }
        public bool TryBegin()
        {
            if(!pending)return false;pending=false;playing=true;elapsed=0;start=player.transform.position;view=player.ViewRotation;
            var clip=Resources.Load<AudioClip>(interactive?"Story/AwakeningTransfer":"Story/Awakening");voice=gameObject.AddComponent<AudioSource>();voice.clip=clip;voice.spatialBlend=0;voice.volume=.85f;voice.ignoreListenerPause=true;duration=Mathf.Max(10,clip!=null?clip.length+2:10);
            controls.SetMode(ControlMode.Cinematic);overlay.style.display=DisplayStyle.Flex;if(clip!=null)voice.Play();Tick(0);return true;
        }
        private void Update()
        {
            if(!playing)return;
            var kb=UnityEngine.InputSystem.Keyboard.current;
            if(kb!=null&&(kb.escapeKey.wasPressedThisFrame||kb.spaceKey.wasPressedThisFrame||kb.eKey.wasPressedThisFrame)){Skip();return;}
            Tick(Time.unscaledDeltaTime);
        }
        public void Tick(float delta)
        {
            if(!playing)return;elapsed+=delta;float t=elapsed/duration;
            // First reveal the actual body from behind, then settle into its real eye position.
            var behind=start+view*new Vector3(1.1f,1.2f,-1.8f);var target=start+Vector3.up*1.25f;
            float approach=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.85f,t));var eye=player.Eye;
            var position=Vector3.Lerp(behind,eye,approach);var rotation=Quaternion.Slerp(Quaternion.LookRotation(target-behind),view,approach);
            rig.SetCinematicPose(position,rotation,Mathf.Lerp(50,65,approach),approach<.9f);
            shade.style.opacity=t<.12f?1-t/.12f:0;
            subtitle.text = interactive
                ? (elapsed < 2.8f ? "Я успел перенести сознание." : elapsed < 5.1f ? "Значит, это теперь моё тело." : "Доберусь до SUPER. И отключу его.")
                : (elapsed < 1.5f ? "Что со мной происходит?.." : elapsed < 4.2f ? "Где все? Я помню только аварийный сигнал." : "Нужно найти выход.");
            if(elapsed>=duration)Skip();
        }
        public void Skip()
        {
            if(!playing)return;playing=false;voice?.Stop();overlay.style.display=DisplayStyle.None;rig.ClearCinematicPose();complete?.Invoke();
        }
        private void OnDestroy(){if(voice!=null)voice.Stop();}
    }
}
