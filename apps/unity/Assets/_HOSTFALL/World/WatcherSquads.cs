using System;
using System.Collections.Generic;
using System.Linq;
using Hostfall.Core;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using UnityEngine;

namespace Hostfall.World
{
    public enum WatcherRole { Local, Pursuit, Flank, Overwatch, Ambush }

    [DefaultExecutionOrder(-100)]
    public sealed class WatcherSquads : MonoBehaviour
    {
        private sealed class Squad
        {
            public readonly List<WatcherController> Members = new();
            public Vector3 Position;
            public float Remaining, UntilAssignment;
            public readonly WatcherController[] Assigned = new WatcherController[4];
        }

        private readonly Dictionary<WatcherController, Squad> membership = new();
        private readonly Dictionary<int, Squad> squads = new();
        private readonly AttackPermitPool permits = new(2);
        private ControlState controls;
        private ComputerCampaign campaign;
        private bool CommandAvailable => campaign.NetworkCommandOnline && campaign.NetworkSecurity.OutageRemaining <= 0;

        public void Initialize(ControlState state, ComputerCampaign model, WatcherController[] actors)
        {
            controls = state;
            campaign = model;
            foreach (var actor in actors.OrderBy(actor => actor.Id, StringComparer.Ordinal))
            {
                int group = Region(actor.PatrolRoute.first);
                if (!squads.TryGetValue(group, out var squad))
                    squads.Add(group, squad = new Squad());
                squad.Members.Add(actor);
                membership.Add(actor, squad);
                actor.BindSquad(this);
            }
        }

        private static int Region(Vector3 point)
        {
            if (point.z < 17 && point.x > -7) return 0;
            if (point.x < -8 && point.z < 38) return 1;
            if (point.z < 38 && point.x < 14) return 2;
            if (point.x < 14 && point.z < 63) return 3;
            if (point.x < 15) return 4;
            if (point.x < 53 && point.z > 65) return 5;
            return point.x < 53 ? 6 : 7;
        }

        private void Update()
        {
            if (controls == null || !controls.WorldRuns) return;
            float dt = Time.deltaTime;
            permits.Advance(dt);
            foreach (var squad in squads.Values)
            {
                squad.Remaining = CommandAvailable ? Mathf.Max(0, squad.Remaining - dt) : 0;
                squad.UntilAssignment -= dt;
                if (squad.Remaining <= 0)
                {
                    Array.Clear(squad.Assigned, 0, squad.Assigned.Length);
                    continue;
                }
                if (squad.UntilAssignment > 0) continue;
                squad.UntilAssignment = .38f;
                Assign(squad);
            }
        }

        private static void Assign(Squad squad)
        {
            Array.Clear(squad.Assigned, 0, squad.Assigned.Length);
            for (int slot = 0; slot < squad.Assigned.Length; slot++)
            {
                float best = 14 * 14;
                foreach (var member in squad.Members)
                {
                    if (!member.CanCoordinate || Array.IndexOf(squad.Assigned, member) >= 0) continue;
                    var offset = member.transform.position - squad.Position;
                    if (Mathf.Abs(offset.y) > 1.8f || offset.sqrMagnitude >= best) continue;
                    best = offset.sqrMagnitude;
                    squad.Assigned[slot] = member;
                }
            }
        }

        internal void ReportSight(WatcherController source, Vector3 position)
        {
            if (!CommandAvailable || !source.CanCoordinate || !membership.TryGetValue(source, out var squad))
                return;
            squad.Position = position;
            squad.Remaining = 3.4f;
        }

        internal bool TryAssignment(WatcherController actor, out WatcherRole role, out Vector3 lastSeen)
        {
            role = WatcherRole.Local;
            lastSeen = default;
            if (!CommandAvailable || !actor.CanCoordinate || !membership.TryGetValue(actor, out var squad) ||
                squad.Remaining <= 0)
                return false;
            int index = Array.IndexOf(squad.Assigned, actor);
            if (index < 0) return false;
            role = (WatcherRole)(index + 1);
            lastSeen = squad.Position;
            return true;
        }

        internal bool Acquire(WatcherController actor) => permits.Acquire(actor.Id, 1.45);
        internal void Release(WatcherController actor) => permits.Release(actor.Id);
    }
}
