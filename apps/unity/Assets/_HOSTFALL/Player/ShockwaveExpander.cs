using System;
using UnityEngine;

namespace Hostfall.Player
{
    public sealed class ShockwaveExpander : MonoBehaviour
    {
        private Transform sphereTransform;
        private Light shockLight;
        private float maxScale;
        private float duration;
        private float elapsed;

        public void Initialize(Transform sphere, Light l, float targetScale, float life)
        {
            sphereTransform = sphere;
            shockLight = l;
            maxScale = targetScale;
            duration = life;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (sphereTransform != null)
            {
                sphereTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, maxScale, Mathf.Sqrt(t));
            }

            if (shockLight != null)
            {
                shockLight.intensity = Mathf.Lerp(8.0f, 0f, t);
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}

