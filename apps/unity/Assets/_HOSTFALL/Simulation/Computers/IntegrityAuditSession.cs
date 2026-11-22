using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum IntegrityPhase { Ready, Audit, OutOfProbes, Complete }

    [Serializable]
    public sealed class IntegrityProbeData
    {
        public int first, last;
        public bool mismatch;
    }

    [Serializable]
    public sealed class IntegrityAuditData
    {
        public IntegrityPhase phase;
        public int stage, first, last = IntegrityAuditSession.PageCount - 1;
        public bool extendRange;
        public IntegrityProbeData[] probes = Array.Empty<IntegrityProbeData>();
    }

    public sealed class IntegrityAuditSession
    {
        public const int PageCount = 32, StageCount = 3;
        private static readonly int[] badPages = { 6, 19, 27 };
        private static readonly int[] limits = { 6, 5, 5 };
        private readonly List<IntegrityProbeData> reports = new();
        private uint candidates = uint.MaxValue;
        private bool extendRange;

        public IntegrityPhase Phase { get; private set; }
        public int Stage { get; private set; }
        public int First { get; private set; }
        public int Last { get; private set; } = PageCount - 1;
        public bool Complete => Phase == IntegrityPhase.Complete;
        public bool CanIsolate => Phase == IntegrityPhase.Audit && (candidates & (candidates - 1)) == 0;
        public int Remaining => limits[Math.Min(Stage, StageCount - 1)] - reports.Count;
        public IReadOnlyList<IntegrityProbeData> Reports => reports;
        public event Action Completed;
        public event Action Changed;

        public bool IsCandidate(int page) => page >= 0 && page < PageCount && (candidates & (1u << page)) != 0;

        public bool Start()
        {
            if (Phase is not (IntegrityPhase.Ready or IntegrityPhase.OutOfProbes)) return false;
            ClearBank();
            Phase = IntegrityPhase.Audit;
            Changed?.Invoke();
            return true;
        }

        public void SelectPage(int page)
        {
            if (Phase != IntegrityPhase.Audit || page < 0 || page >= PageCount) return;
            if (!extendRange)
                First = Last = page;
            else
            {
                int origin = First;
                First = Math.Min(origin, page);
                Last = Math.Max(origin, page);
            }
            extendRange = !extendRange;
            Changed?.Invoke();
        }

        public bool Probe()
        {
            if (Phase != IntegrityPhase.Audit || CanIsolate || Remaining <= 0) return false;
            Record(First, Last);
            if (Remaining == 0 && !CanIsolate) Phase = IntegrityPhase.OutOfProbes;
            extendRange = false;
            Changed?.Invoke();
            return true;
        }

        private void Record(int first, int last)
        {
            bool mismatch = badPages[Stage] >= first && badPages[Stage] <= last;
            uint range = (uint)((1ul << (last - first + 1)) - 1) << first;
            candidates &= mismatch ? range : ~range;
            reports.Add(new IntegrityProbeData { first = first, last = last, mismatch = mismatch });
        }

        public bool Isolate()
        {
            if (!CanIsolate) return false;
            Stage++;
            ClearBank();
            Phase = Stage == StageCount ? IntegrityPhase.Complete : IntegrityPhase.Ready;
            if (Complete) Completed?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private void ClearBank()
        {
            candidates = uint.MaxValue;
            reports.Clear();
            First = 0;
            Last = PageCount - 1;
            extendRange = false;
        }

        public IntegrityAuditData Capture() => new()
        {
            phase = Phase, stage = Stage, first = First, last = Last, extendRange = extendRange,
            probes = reports.Select(report => new IntegrityProbeData { first = report.first, last = report.last, mismatch = report.mismatch }).ToArray()
        };

        public static IntegrityAuditSession Restore(IntegrityAuditData saved)
        {
            SaveValidation.Require(saved != null && Enum.IsDefined(typeof(IntegrityPhase), saved.phase) &&
                saved.stage >= 0 && saved.stage <= StageCount && (saved.stage == StageCount) == (saved.phase == IntegrityPhase.Complete) &&
                InRange(saved.first, saved.last) && saved.probes != null &&
                saved.probes.Length <= limits[Math.Min(saved.stage, StageCount - 1)] &&
                (!saved.extendRange || saved.phase == IntegrityPhase.Audit && saved.first == saved.last),
                "Invalid audit restore");
            bool active = saved.phase is IntegrityPhase.Audit or IntegrityPhase.OutOfProbes;
            SaveValidation.Require(active || saved.probes.Length == 0 && saved.first == 0 && saved.last == PageCount - 1,
                "Inactive audit holds a range");
            var session = new IntegrityAuditSession
            {
                Stage = saved.stage, Phase = IntegrityPhase.Audit, First = saved.first, Last = saved.last, extendRange = saved.extendRange
            };
            foreach (var report in saved.probes)
            {
                SaveValidation.Require(report != null && !session.CanIsolate && InRange(report.first, report.last), "Invalid diagnostic probe");
                session.Record(report.first, report.last);
                SaveValidation.Require(session.reports[^1].mismatch == report.mismatch, "Audit result differs from firmware");
            }
            SaveValidation.Require(!active || (saved.phase == IntegrityPhase.OutOfProbes) == (session.Remaining == 0 && !session.CanIsolate),
                "Audit budget does not match report");
            session.Phase = saved.phase;
            return session;
        }

        private static bool InRange(int first, int last) => first >= 0 && last >= first && last < PageCount;
    }
}
