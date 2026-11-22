using Hostfall.Simulation.Computers;
using UnityEngine;
namespace Hostfall.UI
{
    public sealed partial class ComputerRuntimeUI
    {
        private AudioSource systemSpeaker;
        private static readonly AudioClip[] systemTones=new AudioClip[16];
        private static AudioClip fieldRecording;

        private void UseSystemSpeaker()
        {
            if(systemSpeaker==null)
            {
                var speaker=new GameObject("Shared physical computer speaker");speaker.transform.SetParent(transform,false);
                systemSpeaker=speaker.AddComponent<AudioSource>();systemSpeaker.spatialBlend=1;systemSpeaker.minDistance=1;systemSpeaker.maxDistance=12;systemSpeaker.rolloffMode=AudioRolloffMode.Linear;systemSpeaker.playOnAwake=false;
            }
            systemSpeaker.transform.position=current.Surface.Centre;
        }

        private void PlayFieldRecording()
        {
            if (fieldRecording == null) fieldRecording = Resources.Load<AudioClip>("Investigation/Voice0317");
            if (fieldRecording == null) return;
            UseSystemSpeaker();
            systemSpeaker.clip = fieldRecording;
            systemSpeaker.Play();
        }

        private void StopFieldRecording()
        {
            if (systemSpeaker == null || systemSpeaker.clip == null) return;
            systemSpeaker.Stop();
            systemSpeaker.clip = null;
        }

        private void PlaySystemSound(bool opening)
        {
            var definition=ComputerApplications.Find(current?.StationId);if(definition==null)return;
            UseSystemSpeaker();
            int theme=Mathf.Clamp((int)definition.Style,0,7),index=theme*2+(opening?0:1);
            if(systemTones[index]==null)
            {
                const int rate=22050;const float duration=.085f;var samples=new float[(int)(rate*duration)];float baseFrequency=540+70*theme,phase=0;
                for(int i=0;i<samples.Length;i++)
                {
                    float t=i/(float)rate,envelope=Mathf.Sin(Mathf.PI*t/duration);envelope*=envelope;
                    float note=t<duration*.5f?baseFrequency:baseFrequency*(opening?1.25f:.75f);
                    phase+=2*Mathf.PI*note/rate;samples[i]=Mathf.Sin(phase)*envelope*.14f;
                }
                var clip=AudioClip.Create("Original computer window tone / "+definition.Style+" / "+opening,samples.Length,1,rate,false);clip.SetData(samples,0);systemTones[index]=clip;
            }
            systemSpeaker.PlayOneShot(systemTones[index],.23f);
        }
    }
}
