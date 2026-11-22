using UnityEngine;

namespace Hostfall.World
{
    public sealed class RelayHorizon : MonoBehaviour
    {
        [SerializeField] private Transform[] distantClouds;
        [SerializeField] private Transform[] shutters;
        [SerializeField] private Light flash;
        private Vector3[] cloudSizes, shutterPositions;

        public void Configure(Transform[] clouds, Transform[] windowShutters, Light eventLight)
        {
            distantClouds = clouds;
            shutters = windowShutters;
            flash = eventLight;
        }

        public void Initialize()
        {
            cloudSizes = new Vector3[distantClouds.Length];
            shutterPositions = new Vector3[shutters.Length];
            for (int i = 0; i < distantClouds.Length; i++) cloudSizes[i] = distantClouds[i].localScale;
            for (int i = 0; i < shutters.Length; i++) shutterPositions[i] = shutters[i].localPosition;
        }

        public void Present(float time, float intensity)
        {
            float opening = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 1.5f, time));
            for (int i = 0; i < shutters.Length; i++)
                shutters[i].localPosition = shutterPositions[i] + Vector3.right * ((i == 0 ? -1 : 1) * 8.3f * opening);

            float brightest = 0;
            for (int i = 0; i < distantClouds.Length; i++)
            {
                float after = time - 1.5f - i * 1.02f;
                bool active = after >= 0;
                var cloud = distantClouds[i];
                if (cloud.gameObject.activeSelf != active) cloud.gameObject.SetActive(active);
                if (!active) continue;
                float height = Mathf.SmoothStep(.04f, 1, Mathf.Clamp01(after * .43f));
                cloud.localScale = Vector3.Scale(cloudSizes[i], new Vector3(.16f + height * .84f, height, .16f + height * .84f));
                brightest = Mathf.Max(brightest, Mathf.Exp(-after * 4.2f));
            }
            flash.enabled = brightest > .025f && intensity > 0;
            flash.intensity = brightest * Mathf.Lerp(0, 9, intensity);
        }
    }
}
