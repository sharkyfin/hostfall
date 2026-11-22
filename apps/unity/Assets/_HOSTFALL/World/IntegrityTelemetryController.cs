using System;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public sealed class IntegrityTelemetryController : MonoBehaviour
    {
        [SerializeField] private GameObject[] markers;
        private IntegrityAuditSession session;
        public bool Active => session?.Complete == true;
        public int VisibleMarkers { get; private set; }

        public void Configure(GameObject[] authoredMarkers) => markers = authoredMarkers;

        public void Initialize(IntegrityAuditSession model)
        {
            if (markers == null || markers.Length != IntegrityAuditSession.StageCount)
                throw new InvalidOperationException("Missing authored telemetry markers");
            session = model;
            Refresh();
        }

        public void Refresh()
        {
            bool visible = Active;
            foreach (var marker in markers)
                marker.SetActive(visible);
            VisibleMarkers = visible ? markers.Length : 0;
        }
    }
}
