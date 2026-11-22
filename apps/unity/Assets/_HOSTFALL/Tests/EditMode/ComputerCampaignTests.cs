using System;
using System.Collections.Generic;
using System.Linq;
using Hostfall.Simulation;
using Hostfall.Simulation.Computers;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class ComputerCampaignTests
    {
        [Test]
        public void HomeStartsBlankAndRestoreRemovesOnlyTheObsoleteBanner()
        {
            var campaign = new ComputerCampaign();
            Assert.That(campaign.Home.Terminal.Lines, Is.Empty);
            campaign.Home.Terminal.AddLine("HOME / локальный сеанс оператора\nАрхив доступен. Справочник — вкладка «Знания».", "muted");
            campaign.Shell.Execute("pwd");
            campaign.Shell.SetDraft("ls");
            var restored = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(restored.Home.Terminal.Lines.Select(line => line.text),
                Is.EqualTo(new[] { "operator@home:~$ pwd", "/home/operator" }));
            Assert.That(restored.Home.Terminal.History, Is.EqualTo(new[] { "pwd" }));
            Assert.That(restored.Home.Terminal.Draft, Is.EqualTo("ls"));
        }

        [Test]
        public void HomeShellNavigatesSearchesAndReadsOnlyItsVirtualFiles()
        {
            var campaign = new ComputerCampaign(); var shell = campaign.Shell;
            Assert.That(campaign.Home.Id, Is.EqualTo(DeviceId.Home01));
            Assert.That(shell.Execute("pwd").Text, Is.EqualTo("/home/operator"));
            Assert.That(shell.Execute("ls").Text, Does.Not.Contain(".profile"));
            Assert.That(shell.Execute("ls -la").Text, Does.Contain(".profile").And.Contain("dr-xr-xr-x"));
            Assert.That(shell.Execute("cd archive").ExitCode, Is.Zero);
            Assert.That(shell.Execute("grep -in 'связь' evacuation.txt").Text, Does.Contain("3:"));
            Assert.That(shell.Execute("find ~ -type f -name '*.log'").Text, Is.EqualTo("/home/operator/archive/recovery_journal.log"));
            Assert.That(shell.Execute("cd ../../../../../../").ExitCode, Is.Zero);
            Assert.That(shell.Execute("pwd").Text, Is.EqualTo("/"));
            Assert.That(shell.Execute("cat /etc/passwd").ExitCode, Is.EqualTo(1));
            foreach (string command in new[] { "echo hacked", "rm ~/readme.txt", "cat a; pwd", "cat $(whoami)", "cat a | pwd", "cat a > b", "help\npwd", "cat 'broken" })
                Assert.That(shell.Execute(command).ExitCode, Is.EqualTo(1), command);
            Assert.That(shell.Execute("cat ~/readme.txt").ExitCode, Is.Zero);
            Assert.That(HomeGuide.Chapters.Count, Is.EqualTo(5));
            Assert.That(HomeGuide.Search("grep").Any(), Is.True);
        }

        [Test]
        public void HomeArchiveAcceptsEarnedRecordsAndRestoresAnOldKnowledgeSelection()
        {
            var campaign = new ComputerCampaign();
            Assert.That(campaign.GuideAvailable("network"), Is.False);
            Assert.That(campaign.TryReadArchive(ComputerCampaign.IncidentPath, out _), Is.False);
            Assert.That(campaign.HasUnreadArchive, Is.True);
            Assert.That(campaign.TryReadArchive(ComputerCampaign.EvacuationPath, out string record), Is.True);
            Assert.That(record, Does.Contain("VENTFLOW"));
            Assert.That(campaign.HasUnreadArchive, Is.False);
            var old = campaign.Capture();
            old.guide = "paths";
            var restored = ComputerCampaign.Restore(old);
            Assert.That(restored.SelectedGuide, Is.EqualTo("start"));
            Assert.That(restored.HasRead(ComputerCampaign.EvacuationPath), Is.True);
        }

        [Test]
        public void HomeShellCompletionHistoryPagerAndDraftSurviveRestore()
        {
            var campaign = new ComputerCampaign(); var shell = campaign.Shell;
            campaign.Home.WriteFile("/home/operator/archive/shift note.txt", string.Join("\n", Enumerable.Range(1, 60)));
            Assert.That(shell.Complete("he").Text, Is.EqualTo("help"));
            Assert.That(shell.Complete("cat 'archive/shift n").Text, Is.EqualTo("cat 'archive/shift note.txt'"));
            string completed = shell.Complete("cat archive/shift").Text;
            Assert.That(shell.Execute(completed).ExitCode, Is.Zero);
            shell.Execute("less 'archive/shift note.txt'"); shell.Page(1);
            shell.SetDraft("grep -n");
            var restored = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(restored.Shell.PagerText, Is.EqualTo(shell.PagerText));
            Assert.That(restored.Shell.PagerLine, Is.EqualTo(18));
            Assert.That(restored.Home.Terminal.Draft, Is.EqualTo("grep -n"));
            Assert.That(restored.Shell.BrowseHistory(-1, "draft"), Does.StartWith("less"));
            Assert.That(restored.Shell.BrowseHistory(1, ""), Is.EqualTo("draft"));
            restored.Shell.ClosePager(); restored.Shell.Execute("clear");
            Assert.That(restored.Home.Terminal.Lines, Is.Empty);
            Assert.That(restored.Home.Terminal.History, Is.Not.Empty);
        }

        [Test]
        public void SnakeRejectsReversalAndSecondTurnWithinOneGridStep()
        {
            var snake = new SnakeSession(17); snake.Start();
            Assert.That(snake.Turn(SnakeDirection.Left), Is.False);
            Assert.That(snake.Turn(SnakeDirection.Up), Is.True);
            Assert.That(snake.Turn(SnakeDirection.Left), Is.False);
            snake.Advance(snake.StepSeconds / 2);
            var restored = SnakeSession.Restore(snake.Capture());
            snake.Advance(snake.StepSeconds / 2); restored.Advance(restored.StepSeconds / 2);
            Assert.That(restored.Body, Is.EqualTo(snake.Body));
            Assert.That(restored.Direction, Is.EqualTo(SnakeDirection.Up));
            Assert.That(restored.Turn(SnakeDirection.Left), Is.True);
        }

        [Test]
        public void SnakePartialRestoreReplaysExactlyAndFailuresKeepEarnedStage()
        {
            var original = new ComputerCampaign(42); original.BeginSnake();
            for (int i = 0; i < 9; i++) PlayTick(original.Snake);
            original.Snake.Advance(original.Snake.StepSeconds * .37);
            var restored = ComputerCampaign.Restore(original.Capture());
            for (int i = 0; i < 120; i++)
            {
                PlayTick(original.Snake); PlayTick(restored.Snake);
                var a = original.Snake.Capture(); var b = restored.Snake.Capture();
                Assert.That(b.body, Is.EqualTo(a.body)); Assert.That(b.random, Is.EqualTo(a.random));
                Assert.That(b.food, Is.EqualTo(a.food)); Assert.That(b.score, Is.EqualTo(a.score));
                Assert.That(b.phase, Is.EqualTo(a.phase)); Assert.That(b.elapsed, Is.EqualTo(a.elapsed));
            }
            original = new ComputerCampaign(9); original.BeginSnake();
            while (original.Snake.Stage < 1) PlayTick(original.Snake);
            // Intentionally do not steer until colliding with a wall.
            for (int i = 0; i < 40 && original.Snake.Phase == SnakePhase.Running; i++) original.Snake.Advance(original.Snake.StepSeconds);
            Assert.That(original.Snake.Phase, Is.EqualTo(SnakePhase.Failed));
            int earned = original.Snake.Stage; original.BeginSnake();
            Assert.That(original.Snake.Stage, Is.EqualTo(earned)); Assert.That(original.Snake.Score, Is.EqualTo(earned * 4));
            Assert.That(original.VentSealed, Is.True);
            ComputerCampaign.Restore(original.Capture());
        }

        [Test]
        public void AllThreeStagesPublishRealDesiredActionsAndOneHomeDocumentExactlyOnce()
        {
            var campaign = new ComputerCampaign(123); var events = new List<WorldActionData>();
            campaign.WorldAction += events.Add;
            Assert.That(campaign.Home.TryRead(ComputerCampaign.IncidentPath, out _), Is.False);
            campaign.BeginSnake(); Complete(campaign.Snake);
            Assert.That(events.Count, Is.EqualTo(4));
            Assert.That(events.Count(a => a.kind == WorldActionKind.SealVent), Is.EqualTo(1));
            Assert.That(events.Any(a => a.kind == WorldActionKind.UnlockDoor && a.target == "door.corridor.server"), Is.True);
            Assert.That(campaign.Messages.Count, Is.EqualTo(2));
            Assert.That(campaign.Home.TryRead(ComputerCampaign.IncidentPath, out var incident), Is.True);
            Assert.That(incident, Does.Contain("SUPER"));
            campaign.Shell.Execute("cat " + ComputerCampaign.IncidentPath);
            var restored = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(restored.HasRead(ComputerCampaign.IncidentPath), Is.True);
            Assert.That(restored.Messages.Last().read, Is.True);
            restored.BeginSnake(); restored.Snake.Advance(100);
            Assert.That(restored.Messages.Count, Is.EqualTo(2)); Assert.That(restored.Actions.Count, Is.EqualTo(4));
            Assert.That(campaign.Home.TryRead("/home/operator/readme.txt", out var readme), Is.True);
            Assert.That(readme, Does.Not.Contain("TYPE-IV"));
        }

        [Test]
        public void VentWarningRunsAfterLeavingGameAndSealingCannotEraseReleasedEnemy()
        {
            var campaign = new ComputerCampaign(7); campaign.BeginSnake();
            for (int i = 0; i < 26; i++) campaign.AdvanceWorld(.25);
            Assert.That(campaign.ThreatPhase, Is.EqualTo(VentThreatPhase.Released));
            Assert.That(campaign.Snake.Score, Is.Zero, "World clock must not play the game");
            Complete(campaign.Snake);
            Assert.That(campaign.VentSealed, Is.True);
            Assert.That(campaign.ThreatPhase, Is.EqualTo(VentThreatPhase.Released));
            Assert.That(ComputerCampaign.Restore(campaign.Capture()).ThreatPhase, Is.EqualTo(VentThreatPhase.Released));
        }

        [Test]
        public void MalformedSavesCannotForgeRewardsOrBreakAppGeometry()
        {
            var campaign = new ComputerCampaign();
            var snapshot = campaign.Capture(); snapshot.actions = new[] { new WorldActionData { receipt = "fake", kind = WorldActionKind.UnlockDoor, target = "door.soc.corridor" } };
            Assert.Throws<ArgumentException>(() => ComputerCampaign.Restore(snapshot));
            var snake = campaign.Snake.Capture(); snake.body[1] = snake.body[0];
            Assert.Throws<ArgumentException>(() => SnakeSession.Restore(snake));
            snake = campaign.Snake.Capture(); snake.elapsed = double.NaN;
            Assert.Throws<ArgumentException>(() => SnakeSession.Restore(snake));
            snapshot = campaign.Capture(); snapshot.home.id = DeviceId.Ws07;
            Assert.Throws<ArgumentException>(() => ComputerCampaign.Restore(snapshot));
            Assert.Throws<ArgumentException>(() => campaign.SaveDesktop(new ComputerDesktopData
            {
                station = ComputerCampaign.SnakeStation, focusedApp = "snake",
                windows = new[] { new DesktopWindowData { app = "snake", x = float.NaN } }
            }));
        }

        [Test]
        public void DesktopWindowSnapshotDoesNotLeakMutableStateToViews()
        {
            var campaign = new ComputerCampaign();
            campaign.SaveDesktop(new ComputerDesktopData
            {
                station = ComputerCampaign.SnakeStation, focusedApp = "snake",
                windows = new[] { new DesktopWindowData { app = "snake", x = 200 } }
            });
            var view = campaign.Desktop(ComputerCampaign.SnakeStation); view.windows[0].x = 900;
            Assert.That(campaign.Desktop(ComputerCampaign.SnakeStation).windows[0].x, Is.EqualTo(200));
            var restored = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(restored.Desktop(ComputerCampaign.SnakeStation).focusedApp, Is.EqualTo("snake"));
        }

        [Test]
        public void AuthoredSnakeBoardsCanBeCompletedAcrossDeterministicSeeds()
        {
            for (uint seed = 1; seed <= 24; seed++)
            {
                var snake = new SnakeSession(seed); snake.Start(); Complete(snake);
                Assert.That(snake.Score, Is.EqualTo(12));
                Assert.That(SnakeSession.Restore(snake.Capture()).Phase, Is.EqualTo(SnakePhase.Complete));
            }
        }

        [Test]
        public void InvestigationAndServiceUtilitiesAdvanceTheRouteWithoutAdvancingRetiredGames()
        {
            var campaign = new ComputerCampaign();
            string camera = ComputerCampaign.NetworkStation, archive = ComputerCampaign.DetectiveStation;
            Assert.That(campaign.ReadCaseEvidence(camera, 0), Is.False);
            Complete(campaign.Snake);
            Assert.That(campaign.SubmitCase(camera, 1, 3), Is.EqualTo(FieldCaseResult.CompareSources));
            Assert.That(campaign.ReadCaseEvidence(camera, 0), Is.True);
            campaign = ComputerCampaign.Restore(campaign.Capture());
            Assert.That(campaign.ReadCaseEvidence(camera, 1), Is.True);
            Assert.That(campaign.SubmitCase(camera, 0, 3), Is.EqualTo(FieldCaseResult.Incorrect));
            Assert.That(campaign.NetworkSecurity.HasPending, Is.True);
            Assert.That(campaign.SubmitCase(camera, 1, 3), Is.EqualTo(FieldCaseResult.Confirmed));
            Assert.That(campaign.NetworkSecurity.HasPending || campaign.NetworkCommandOnline, Is.False);
            Assert.That(campaign.Network.Stage, Is.Zero);

            campaign.ExecuteFieldUtility(ComputerCampaign.SignalStation, FieldUtilityId.PowerLight);
            Assert.That(campaign.Circuit.Energized, Is.False);
            campaign.ExecuteFieldUtility(ComputerCampaign.CircuitStation, FieldUtilityId.PowerGuard);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Turret), Is.True);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Lighting), Is.False);
            campaign.AdvanceWorld(.2);
            campaign.ExecuteFieldUtility(ComputerCampaign.CircuitStation, FieldUtilityId.PowerLight);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Lighting), Is.True);
            Assert.That(campaign.Circuit.Powered(CircuitLoad.Shortcut), Is.True);
            Assert.That(ComputerCampaign.Restore(campaign.Capture()).Circuit.Powered(CircuitLoad.Lighting), Is.True);
            campaign.ExecuteFieldUtility(ComputerCampaign.SignalStation, FieldUtilityId.PhaseBypass);
            campaign.ExecuteFieldUtility(ComputerCampaign.PayloadStation, FieldUtilityId.CargoTransfer);
            campaign.ExecuteFieldUtility(ComputerCampaign.MinefieldStation, FieldUtilityId.FloorService);
            Assert.That(campaign.CurrentStage, Is.EqualTo(CampaignStage.Detective));
            Assert.That(campaign.Signal.Stage + campaign.Payload.Stage + campaign.Minefield.Stage, Is.Zero);
            Assert.That(campaign.ReadCaseEvidence(archive, 0) && campaign.ReadCaseEvidence(archive, 1), Is.True);
            Assert.That(campaign.SubmitCase(archive, 2, 3), Is.EqualTo(FieldCaseResult.Confirmed));
            Assert.That(campaign.CurrentStage, Is.EqualTo(CampaignStage.Safe));
            Assert.That(campaign.TakeArchiveKey(), Is.True);
            Assert.That(campaign.TakeArchiveKey(), Is.False);

            var emptyBay = new ContainmentSensors(SentryZone.Offline, false, false, false);
            campaign.Containment.Observe(emptyBay, .1);
            campaign.ExecuteFieldUtility(ComputerCampaign.ContainmentStation, FieldUtilityId.QuarantineSeal);
            for (int tick = 0; tick < 25; tick++) campaign.Containment.Observe(emptyBay, .1);
            campaign.ExecuteFieldUtility(ComputerCampaign.ContainmentStation, FieldUtilityId.QuarantineSeal);
            Assert.That(campaign.Containment.Complete, Is.True);
            Assert.That(campaign.AuthorizeRecovery(), Is.False);
            for (int record = 0; record < ComputerCampaign.RecoveryRecords.Count; record++)
                Assert.That(campaign.TryReadRecoveryRecord(record, out _), Is.True);
            Assert.That(campaign.AuthorizeRecovery(), Is.True);
            Assert.That(campaign.AuthorizeRecovery(), Is.False);
            var saved = campaign.Capture();
            var restored = ComputerCampaign.Restore(saved);
            Assert.That(restored.CurrentStage, Is.EqualTo(CampaignStage.Transit));
            Assert.That(restored.Actions.Count(a => a.target == ComputerCampaign.RecoveryTransitTarget), Is.EqualTo(1));
            saved.fieldRoute.cameraRead = 0;
            Assert.Throws<ArgumentException>(() => ComputerCampaign.Restore(saved));
        }

        internal static void Complete(SnakeSession snake)
        {
            for (int i = 0; i < 2500 && snake.Phase != SnakePhase.Complete; i++) PlayTick(snake);
            Assert.That(snake.Phase, Is.EqualTo(SnakePhase.Complete), "Real steering must be able to finish");
        }
        private static void PlayTick(SnakeSession snake)
        {
            if (snake.Phase == SnakePhase.Complete) return;
            if (snake.Phase != SnakePhase.Running) snake.Start();
            int head = snake.Body[0], target = snake.Food;
            var queue = new Queue<int>(); var parent = new Dictionary<int, int> { [head] = -1 }; queue.Enqueue(head);
            while (queue.Count > 0 && !parent.ContainsKey(target))
            {
                int cell = queue.Dequeue();
                foreach (int next in new[] { cell + 1, cell + SnakeSession.Width, cell - 1, cell - SnakeSession.Width })
                {
                    if (next < 0 || next >= SnakeSession.Width * SnakeSession.Height ||
                        Math.Abs(next % SnakeSession.Width - cell % SnakeSession.Width) + Math.Abs(next / SnakeSession.Width - cell / SnakeSession.Width) != 1 ||
                        parent.ContainsKey(next) || snake.IsObstacle(next) || snake.Body.Contains(next)) continue;
                    parent[next] = cell; queue.Enqueue(next);
                }
            }
            if (parent.ContainsKey(target))
            {
                int next = target; while (parent[next] != head && parent[next] >= 0) next = parent[next];
                snake.Turn(next == head + 1 ? SnakeDirection.Right : next == head - 1 ? SnakeDirection.Left : next > head ? SnakeDirection.Down : SnakeDirection.Up);
            }
            snake.Advance(snake.StepSeconds);
        }
    }
}
