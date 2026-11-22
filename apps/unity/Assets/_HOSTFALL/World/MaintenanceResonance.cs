using Hostfall.Core;
using UnityEngine;

namespace Hostfall.World
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MaintenanceResonance : MonoBehaviour
    {
        private ControlState controls;
        private AudioSource pump;

        private void Start()
        {
            controls = FindAnyObjectByType<ControlState>();
            pump = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (controls == null) return;
            if (controls.WorldRuns)
            {
                if (!pump.isPlaying) pump.Play();
            }
            else if (pump.isPlaying) pump.Pause();
        }
    }
}
