using System;
using System.Collections.Generic;
using System.Linq;

namespace Hostfall.Simulation.Computers
{
    public enum RecoveryStage { Network, Access, Sequence, Complete }
    public enum RecoveryIssue { None, Inactive, Missing, Link, Credential, Order }
    public enum RecoveryNode { Vent, Relay, Power, Phase, Archive, Ring, Core }
    public enum RecoveryCredential { DrivePermit, ArchiveKey, QuarantineSeal, OperatorSignature }
    public enum RecoveryOperation { Authenticate, SecureBoundary, TakeClockReference, RestoreBus, Commit }

    public readonly struct RecoveryLink
    {
        public readonly RecoveryNode From, To;
        public RecoveryLink(RecoveryNode from, RecoveryNode to) { From = from; To = to; }
    }

    [Serializable]
    public sealed class RecoveryProtocolData
    {
        public bool started;
        public RecoveryStage stage;
        public int links;
        public int[] credentials = Enumerable.Repeat(-1, RecoveryProtocolSession.AccessRows).ToArray();
        public int[] sequence = Array.Empty<int>();
    }

    public sealed class RecoveryProtocolSession
    {
        public const int AccessRows = 4, LinkCount = 11, OperationCount = 5;
        private static readonly RecoveryLink[] choices =
        {
            new(RecoveryNode.Vent, RecoveryNode.Relay),
            new(RecoveryNode.Vent, RecoveryNode.Core),
            new(RecoveryNode.Relay, RecoveryNode.Power),
            new(RecoveryNode.Relay, RecoveryNode.Archive),
            new(RecoveryNode.Power, RecoveryNode.Phase),
            new(RecoveryNode.Power, RecoveryNode.Core),
            new(RecoveryNode.Phase, RecoveryNode.Ring),
            new(RecoveryNode.Archive, RecoveryNode.Ring),
            new(RecoveryNode.Archive, RecoveryNode.Power),
            new(RecoveryNode.Ring, RecoveryNode.Core),
            new(RecoveryNode.Core, RecoveryNode.Relay)
        };
        private const int RequiredLinks = (1 << 0) | (1 << 2) | (1 << 4) | (1 << 6) | (1 << 7) | (1 << 9);
        private static readonly RecoveryCredential[] access =
        {
            RecoveryCredential.DrivePermit, RecoveryCredential.ArchiveKey,
            RecoveryCredential.QuarantineSeal, RecoveryCredential.OperatorSignature
        };
        private static readonly RecoveryOperation[] sequence =
        {
            RecoveryOperation.SecureBoundary, RecoveryOperation.TakeClockReference,
            RecoveryOperation.Authenticate, RecoveryOperation.RestoreBus, RecoveryOperation.Commit
        };
        private readonly int[] credentials = Enumerable.Repeat(-1, AccessRows).ToArray();
        private readonly List<RecoveryOperation> program = new(OperationCount);
        private int links;

        public static IReadOnlyList<RecoveryLink> Links => choices;
        public bool Started { get; private set; }
        public RecoveryStage Stage { get; private set; }
        public bool Complete => Stage == RecoveryStage.Complete;
        public IReadOnlyList<RecoveryOperation> Program => program;
        public event Action Completed;
        public event Action Changed;

        public bool Start()
        {
            if (Started) return false;
            Started = true;
            Changed?.Invoke();
            return true;
        }

        public bool Connected(int index) => index >= 0 && index < LinkCount && (links & (1 << index)) != 0;

        public void ToggleLink(int index)
        {
            if (!Started || Stage != RecoveryStage.Network || index < 0 || index >= LinkCount) return;
            links ^= 1 << index;
            Changed?.Invoke();
        }

        public int Credential(int service) => credentials[service];

        public void SetCredential(int service, RecoveryCredential credential)
        {
            if (!Started || Stage != RecoveryStage.Access || service < 0 || service >= AccessRows ||
                !Enum.IsDefined(typeof(RecoveryCredential), credential)) return;
            credentials[service] = (int)credential;
            Changed?.Invoke();
        }

        public void Append(RecoveryOperation operation)
        {
            if (!Started || Stage != RecoveryStage.Sequence ||
                !Enum.IsDefined(typeof(RecoveryOperation), operation) || program.Contains(operation)) return;
            program.Add(operation);
            Changed?.Invoke();
        }

        public void Remove(int index)
        {
            if (!Started || Stage != RecoveryStage.Sequence || index < 0 || index >= program.Count) return;
            program.RemoveAt(index);
            Changed?.Invoke();
        }

        public void Move(int index, int delta)
        {
            if (!Started || Stage != RecoveryStage.Sequence || index < 0 || index >= program.Count ||
                (delta != -1 && delta != 1)) return;
            int adjacent = index + delta;
            if (adjacent < 0 || adjacent >= program.Count) return;
            (program[index], program[adjacent]) = (program[adjacent], program[index]);
            Changed?.Invoke();
        }

        public RecoveryIssue Verify()
        {
            if (!Started || Complete) return RecoveryIssue.Inactive;
            RecoveryIssue issue;
            switch (Stage)
            {
                case RecoveryStage.Network:
                    issue = links == 0 ? RecoveryIssue.Missing : links == RequiredLinks ? RecoveryIssue.None : RecoveryIssue.Link;
                    break;
                case RecoveryStage.Access:
                    issue = credentials.Contains(-1) ? RecoveryIssue.Missing :
                        AccessConfirmed(credentials) ? RecoveryIssue.None : RecoveryIssue.Credential;
                    break;
                default:
                    issue = program.Count != OperationCount ? RecoveryIssue.Missing :
                        program.SequenceEqual(sequence) ? RecoveryIssue.None : RecoveryIssue.Order;
                    break;
            }
            if (issue != RecoveryIssue.None) return issue;
            Stage++;
            if (Complete) Completed?.Invoke();
            Changed?.Invoke();
            return RecoveryIssue.None;
        }

        private static bool AccessConfirmed(int[] values)
        {
            for (int service = 0; service < AccessRows; service++)
                if (values[service] != (int)access[service]) return false;
            return true;
        }

        public RecoveryProtocolData Capture() => new()
        {
            started = Started, stage = Stage, links = links, credentials = (int[])credentials.Clone(),
            sequence = program.Select(item => (int)item).ToArray()
        };

        public static RecoveryProtocolSession Restore(RecoveryProtocolData data)
        {
            SaveValidation.Require(data != null && Enum.IsDefined(typeof(RecoveryStage), data.stage) &&
                data.links >= 0 && data.links < (1 << LinkCount) && data.credentials?.Length == AccessRows &&
                data.credentials.All(item => item >= -1 && item < AccessRows) && data.sequence != null &&
                data.sequence.Length <= OperationCount && data.sequence.Distinct().Count() == data.sequence.Length &&
                data.sequence.All(item => item >= 0 && item < OperationCount), "Invalid recovery program");
            SaveValidation.Require(data.started || data.stage == RecoveryStage.Network && data.links == 0, "Inactive recovery protocol");
            SaveValidation.Require(data.stage == RecoveryStage.Network
                ? data.credentials.All(item => item == -1) && data.sequence.Length == 0
                : data.links == RequiredLinks, "Invalid reconstructed network");
            SaveValidation.Require(data.stage < RecoveryStage.Sequence
                ? data.sequence.Length == 0 : AccessConfirmed(data.credentials), "Invalid access matrix");
            var session = new RecoveryProtocolSession { Started = data.started, Stage = data.stage, links = data.links };
            Array.Copy(data.credentials, session.credentials, AccessRows);
            session.program.AddRange(data.sequence.Select(item => (RecoveryOperation)item));
            SaveValidation.Require(!session.Complete || session.program.SequenceEqual(sequence), "Invalid activation sequence");
            return session;
        }
    }
}
