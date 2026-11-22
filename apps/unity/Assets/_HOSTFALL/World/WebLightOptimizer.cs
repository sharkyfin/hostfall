using System.Collections.Generic;
using UnityEngine;

namespace Hostfall.World
{
    // High-performance light distance culler for WebGL players.
    // In WebGL, 480+ dynamic point/spot lights create major single-threaded CPU and GPU stalls.
    // This component keeps only lights within relevant proximity active.
    public sealed class WebLightOptimizer : MonoBehaviour
    {
        private struct ManagedLight
        {
            public Light light;
            public Vector3 position;
        }

        private readonly List<ManagedLight> managedLights = new(128);
        private Transform playerTransform;
        private float timer;
        private const float CullDistanceSqr = 34f * 34f;
        private const float RestoreDistanceSqr = 30f * 30f;

        private void Start()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            enabled = false;
            return;
#else
            var player = Object.FindAnyObjectByType<Hostfall.Player.PlayerMotor>();
            if (player != null) playerTransform = player.transform;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) continue;
                managedLights.Add(new ManagedLight { light = light, position = light.transform.position });
            }
            UpdateCulling(true);
#endif
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < 0.25f) return;
            timer = 0;
            if (playerTransform == null)
            {
                var player = Object.FindAnyObjectByType<Hostfall.Player.PlayerMotor>();
                if (player != null) playerTransform = player.transform;
                else return;
            }
            UpdateCulling(false);
        }

        private void UpdateCulling(bool force)
        {
            if (playerTransform == null) return;
            Vector3 p = playerTransform.position;
            for (int i = 0; i < managedLights.Count; i++)
            {
                var entry = managedLights[i];
                if (entry.light == null) continue;
                float distSqr = (entry.position - p).sqrMagnitude;
                if (entry.light.enabled && distSqr > CullDistanceSqr)
                    entry.light.enabled = false;
                else if (!entry.light.enabled && distSqr < RestoreDistanceSqr)
                    entry.light.enabled = true;
            }
        }
    }
}
