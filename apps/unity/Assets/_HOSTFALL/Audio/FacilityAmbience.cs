using UnityEngine;

namespace Hostfall.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FacilityAmbience : MonoBehaviour
    {
        private AudioClip hum;
        private void Awake()
        {
            const int rate = 22050;
            var samples = new float[rate * 2];
            // Integer frequencies over two seconds make the generated loop seamless.
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)rate;
                samples[i] = .1f * Mathf.Sin(2 * Mathf.PI * 50 * time)
                    + .025f * Mathf.Sin(2 * Mathf.PI * 101 * time)
                    + .012f * Mathf.Sin(2 * Mathf.PI * 173 * time);
            }
            hum = AudioClip.Create("NORTHLINE / ventilation", samples.Length, 1, rate, false); hum.SetData(samples, 0);
            var source = GetComponent<AudioSource>(); source.clip = hum; source.loop = true; source.volume = .3f; source.spatialBlend = 0;
            source.Play();
        }
        private void OnDestroy() { if (hum != null) Destroy(hum); }
    }
}
