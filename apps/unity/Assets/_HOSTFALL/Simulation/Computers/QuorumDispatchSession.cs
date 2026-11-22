using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum QuorumIssue { None, Inactive, Witness, TimeWindow, Busy, SharedLimit, Missing, Dependency }

    public sealed class QuorumJob
    {
        public string Title { get; }
        public int Duration { get; }
        public int Earliest { get; }
        public int Deadline { get; }
        public int WitnessMask { get; }
        public int Dependencies { get; }

        internal QuorumJob(string title, int duration, int earliest, int deadline, int witnessMask, int deps = 0)
        {
            Title = title;
            Duration = duration;
            Earliest = earliest;
            Deadline = deadline;
            WitnessMask = witnessMask;
            Dependencies = deps;
        }
    }

    [Serializable]
    public sealed class QuorumDispatchData
    {
        public bool started, complete;
        public int selected;
        public int[] witnesses = Enumerable.Repeat(-1, QuorumDispatchSession.JobCount).ToArray();
        public int[] times = Enumerable.Repeat(-1, QuorumDispatchSession.JobCount).ToArray();
    }

    public sealed class QuorumDispatchSession
    {
        public const int WitnessCount = 3, JobCount = 6, Slots = 7, SimultaneousJobs = 2;
        private static readonly QuorumJob[] tasks =
        {
            new("Опрос реле", 2, 0, 3, 1),
            new("Снимок архива", 2, 1, 4, 2),
            new("Сверка часов", 2, 0, 4, 5),
            new("Подпись реле", 1, 2, 5, 4, 1 | 4),
            new("Подпись архива", 1, 2, 5, 2, 2 | 4),
            new("Подтверждение кворума", 2, 4, 7, 7, 8 | 16)
        };
        private QuorumDispatchData state = new();
        public static IReadOnlyList<QuorumJob> Tasks => tasks;
        public bool Started => state.started;
        public bool Complete => state.complete;
        public int Selected => state.selected;
        public event Action Completed;
        public event Action Changed;

        public bool Start()
        {
            if (Started) return false;
            state.started = true;
            Changed?.Invoke();
            return true;
        }

        public int Witness(int job) => state.witnesses[job];
        public int Time(int job) => state.times[job];

        public void Select(int job)
        {
            if (!Started || Complete || job < 0 || job >= JobCount || job == Selected) return;
            state.selected = job;
            Changed?.Invoke();
        }

        public QuorumIssue Schedule(int witness, int slot)
        {
            if (!Started || Complete) return QuorumIssue.Inactive;
            QuorumIssue issue = CheckPlacement(Selected, witness, slot);
            if (issue != QuorumIssue.None) return issue;
            state.witnesses[Selected] = witness;
            state.times[Selected] = slot;
            Changed?.Invoke();
            return QuorumIssue.None;
        }

        public void Cancel()
        {
            if (!Started || Complete || Witness(Selected) < 0) return;
            state.witnesses[Selected] = -1;
            state.times[Selected] = -1;
            Changed?.Invoke();
        }

        public int JobAt(int witness, int slot)
        {
            for (int job = 0; job < JobCount; job++)
                if (Witness(job) == witness && slot >= Time(job) && slot < Time(job) + tasks[job].Duration)
                    return job;
            return -1;
        }

        public QuorumIssue CheckPlacement(int index, int witness, int slot)
        {
            if (index < 0 || index >= JobCount || witness < 0 || witness >= WitnessCount)
                return QuorumIssue.Witness;
            var job = tasks[index];
            if ((job.WitnessMask & (1 << witness)) == 0) return QuorumIssue.Witness;
            if (slot < job.Earliest || slot > job.Deadline - job.Duration) return QuorumIssue.TimeWindow;
            for (int time = slot; time < slot + job.Duration; time++)
            {
                int occupied = 0;
                for (int lane = 0; lane < WitnessCount; lane++)
                {
                    int other = JobAt(lane, time);
                    if (other < 0 || other == index) continue;
                    if (lane == witness) return QuorumIssue.Busy;
                    occupied++;
                }
                if (occupied >= SimultaneousJobs) return QuorumIssue.SharedLimit;
            }
            return QuorumIssue.None;
        }

        public QuorumIssue Inspect(out int job)
        {
            for (job = 0; job < JobCount; job++)
                if (Witness(job) < 0) return QuorumIssue.Missing;

            for (job = 0; job < JobCount; job++)
            {
                int dependencies = tasks[job].Dependencies;
                for (int previous = 0; previous < job; previous++)
                    if ((dependencies & (1 << previous)) != 0 && Time(previous) + tasks[previous].Duration > Time(job))
                        return QuorumIssue.Dependency;
            }
            job = -1;
            return QuorumIssue.None;
        }

        public QuorumIssue Dispatch(out int job)
        {
            job = -1;
            if (!Started || Complete) return QuorumIssue.Inactive;
            var issue = Inspect(out job);
            if (issue != QuorumIssue.None) return issue;
            state.complete = true;
            Completed?.Invoke();
            Changed?.Invoke();
            return QuorumIssue.None;
        }

        public QuorumDispatchData Capture() => new()
        {
            started = Started, complete = Complete, selected = Selected,
            witnesses = (int[])state.witnesses.Clone(), times = (int[])state.times.Clone()
        };

        public static QuorumDispatchSession Restore(QuorumDispatchData data)
        {
            SaveValidation.Require(data != null && data.witnesses?.Length == JobCount && data.times?.Length == JobCount &&
                data.selected >= 0 && data.selected < JobCount, "Invalid witness schedule");
            var restored = new QuorumDispatchSession();
            for (int job = 0; job < JobCount; job++)
            {
                if (data.witnesses[job] == -1)
                {
                    SaveValidation.Require(data.times[job] == -1 && !data.complete, "Invalid empty witness slot");
                    continue;
                }
                SaveValidation.Require(data.started && restored.CheckPlacement(job, data.witnesses[job], data.times[job]) == QuorumIssue.None,
                    "Invalid witness placement");
                restored.state.witnesses[job] = data.witnesses[job];
                restored.state.times[job] = data.times[job];
            }
            SaveValidation.Require(data.started || data.selected == 0 && !data.complete, "Inactive witness controller");
            SaveValidation.Require(!data.complete || restored.Inspect(out _) == QuorumIssue.None, "Invalid dispatched sequence");
            restored.state.selected = data.selected;
            restored.state.started = data.started;
            restored.state.complete = data.complete;
            return restored;
        }
    }
}
