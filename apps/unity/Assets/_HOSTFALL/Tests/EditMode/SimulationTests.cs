using System.Linq;
using Hostfall.Simulation;
using NUnit.Framework;

namespace Hostfall.Tests
{
    public sealed class SimulationTests
    {
        [Test]
        public void OldAuthoredWalkthroughMigratesWithoutReplacingPlayerNotes()
        {
            const string path="/home/operator/ticket.txt";
            var model=new FacilitySimulation(MissionDefinition.NightShift);
            model[DeviceId.Ws07].WriteFile(path,"WS-07 / INC-0317\nНачните с ps.\nИзучите: cat /var/log/scheduler.log\nОтключите автозапуск: systemctl disable cache-sync\nШтатно остановите службу: systemctl stop cache-sync\nkill завершает процесс, но Restart=always запускает новый.\nЗатем проверьте ps на CORE-01 в серверной.\n");
            var restored=FacilitySimulation.Restore(model.Capture());
            Assert.That(restored[DeviceId.Ws07].TryRead(path,out var note),Is.True);
            Assert.That(note,Does.Not.Contain("systemctl").And.Not.Contain("cache-sync"));
            model[DeviceId.Ws07].WriteFile(path,"player notes");
            restored=FacilitySimulation.Restore(model.Capture());
            restored[DeviceId.Ws07].TryRead(path,out note);
            Assert.That(note,Is.EqualTo("player notes"));
        }
        [Test]
        public void ReferenceExplainsSyntaxWithoutDisclosingTheTarget()
        {
            var model=new FacilitySimulation(MissionDefinition.NightShift);
            var shell=new TerminalSession(model,DeviceId.Ws07);
            var help=shell.Execute("help").Text;
            Assert.That(help,Does.Contain("kill <PID>"));
            Assert.That(help,Does.Contain("systemctl stop <служба>"));
            Assert.That(help,Does.Not.Contain("cache-sync").And.Not.Contain("431").And.Not.Contain("core01").And.Not.Contain("Начните"));
            Assert.That(shell.Execute("systemctl status missing").Text,Does.Not.Contain("cache-sync"));
            Assert.That(model.Objective,Does.Not.Contain("cache-sync"));
            shell.Execute("ps");
            Assert.That(model.Objective,Does.Not.Contain("cache-sync"));
            Assert.That(shell.Execute("systemctl disable cache-sync").Text,Does.Not.Contain("systemctl stop"));
            Assert.That(shell.Execute("systemctl stop cache-sync").Text,Does.Not.Contain("systemctl disable"));
        }
        [Test]
        public void InvestigationCanIdentifyTheRelayFromWorldEvidenceAndVerifyIsolation()
        {
            var world=new FacilitySimulation(MissionDefinition.NightShift); var shell=new TerminalSession(world,DeviceId.Ws07);
            Assert.That(shell.Execute("help").Text,Does.Not.Contain("cache-sync"));
            Assert.That(shell.Execute("cat ticket.txt").Text,Does.Not.Contain("cache-sync").And.Not.Contain("systemctl"));
            var processes=shell.Execute("ps").Text;
            var units=shell.Execute("ls /etc/systemd/system").Text.Split('\n');
            var relay=units.Single(name=>name.EndsWith(".service",System.StringComparison.Ordinal));
            var definition=shell.Execute("cat /etc/systemd/system/"+relay).Text;
            Assert.That(definition,Does.Contain("watcher task relay").And.Contain("Restart=always"));
            var executable=definition.Split('\n').Single(line=>line.StartsWith("ExecStart=",System.StringComparison.Ordinal)).Substring(10);
            Assert.That(processes,Does.Contain(executable));
            var service=relay.Substring(0,relay.Length-".service".Length);
            Assert.That(shell.Execute("cat /var/log/scheduler.log").Text,Does.Contain(service+" -> edge01"));
            shell.Execute("systemctl stop "+service);
            Assert.That(world.ServerAccess,Is.False,"Stopping alone must not silently cancel the restart rule");
            shell.Execute("systemctl disable "+service);
            Assert.That(world.ServerAccess,Is.True);
            Assert.That(shell.Execute("ps").Text,Does.Not.Contain(executable));
            Assert.That(world.Mission.CompletedObjectives,Is.EqualTo(2));
        }
        [Test]
        public void LegacyGreetingLosesTheAutomaticHelpBannerWithoutRemovingTheHelpCommand()
        {
            var model=new FacilitySimulation(MissionDefinition.NightShift); var data=model.Capture();
            var ws=data.computers.Single(c=>c.id==DeviceId.Ws07);
            ws.terminal=new TerminalMemoryData { host=DeviceId.Ws07, initialized=true, lines=new[] { new TerminalLineData { text="Сеанс WS07\nhelp — команды с объяснениями",style="muted" } } };
            var restored=FacilitySimulation.Restore(data);
            Assert.That(TerminalDisplay.Read(restored,DeviceId.Ws07).Transcript,Is.EqualTo("Сеанс WS07"));
            var help=new TerminalSession(restored,DeviceId.Ws07).Execute("help");
            Assert.That(help.ExitCode,Is.Zero); Assert.That(help.Text,Does.Contain("Команды"));
        }
        [Test]
        public void ProcessInspectionAdvancesOnlyOnWorkstation()
        {
            var simulation = new FacilitySimulation(MissionDefinition.NightShift);
            new TerminalSession(simulation, DeviceId.Edge01).Execute("ps");
            Assert.That(simulation.ServerAccess, Is.False);
            var terminal = new TerminalSession(simulation, DeviceId.Ws07);
            Assert.That(terminal.Execute("ps").Text, Does.Contain("431"));
            Assert.That(simulation.Mission.CompletedObjectives, Is.EqualTo(1));
            Assert.That(simulation.ServerAccess, Is.False, "Inspection identifies the problem; it does not release the robot gate.");
        }
        [Test]
        public void FilesAndDirectoriesAreHostLocalAndPathsAreCanonical()
        {
            var simulation = new FacilitySimulation(MissionDefinition.NightShift);
            simulation[DeviceId.Ws07].WriteFile("/tmp/private", "local data");
            var terminal = new TerminalSession(simulation, DeviceId.Ws07);
            Assert.That(terminal.Execute("cd /tmp").ExitCode, Is.Zero);
            Assert.That(terminal.Execute("cat './private'").Text, Is.EqualTo("local data"));
            Assert.That(terminal.Execute("ls").Text, Does.Contain("private"));
            terminal.Execute("cd ../../../../");
            Assert.That(terminal.Execute("pwd").Text, Is.EqualTo("/"));
            terminal.Execute("ssh edge01");
            Assert.That(terminal.Execute("cat /tmp/private").ExitCode, Is.EqualTo(1));
            terminal.Execute("ssh ws07");
            Assert.That(terminal.Execute("pwd").Text, Is.EqualTo("/"));
        }
        [Test]
        public void ServiceRestartsAfterKillButStopsOnlyThroughTheManager()
        {
            var simulation = new FacilitySimulation(MissionDefinition.NightShift); var terminal = new TerminalSession(simulation, DeviceId.Ws07);
            Assert.That(terminal.Execute("kill 431").Text, Does.Contain("pid=432"));
            Assert.That(simulation.SourceIsolated, Is.False);
            terminal.Execute("systemctl disable cache-sync");
            Assert.That(simulation.SourceIsolated, Is.False, "Disabling autostart does not stop or change the running service restart policy");
            terminal.Execute("kill 432");
            Assert.That(simulation.SourceIsolated, Is.False);
            Assert.That(terminal.Execute("ps").Text, Does.Contain("433"));
            terminal.Execute("systemctl stop cache-sync");
            Assert.That(simulation.SourceIsolated, Is.True);
            Assert.That(simulation.ServerAccess, Is.True);
            Assert.That(terminal.Execute("ps").Text, Does.Not.Contain("cache-sync"));
            Assert.That(terminal.Execute("cat /var/log/scheduler.log").Text, Does.Contain("432"));
        }
        [Test]
        public void CompleteIncidentRequiresIsolationAndCoreInspection()
        {
            var simulation = new FacilitySimulation(MissionDefinition.NightShift); var terminal = new TerminalSession(simulation, DeviceId.Ws07);
            var core = new TerminalSession(simulation, DeviceId.Core01);
            core.Execute("ps"); Assert.That(simulation.Mission.IsComplete, Is.False);
            terminal.Execute("ps"); terminal.Execute("systemctl disable cache-sync"); terminal.Execute("systemctl stop cache-sync");
            Assert.That(simulation.Mission.CompletedObjectives, Is.EqualTo(2));
            core.Execute("ps"); Assert.That(simulation.Mission.IsComplete, Is.True);
            Assert.That(core.Execute("cat /var/log/incident.log").Text, Does.Contain("подтверждено оператором"));
        }
        [Test]
        public void PowerLossBreaksRemoteAccessButPreservesLocalAccess()
        {
            var simulation = new FacilitySimulation(MissionDefinition.NightShift); var remote = new TerminalSession(simulation, DeviceId.Ws07);
            remote.Execute("ssh core01"); simulation.SetPower(DeviceId.Edge01, false);
            Assert.That(remote.Execute("ps").ExitCode, Is.EqualTo(1));
            Assert.That(new TerminalSession(simulation, DeviceId.Core01).Execute("hostname").ExitCode, Is.Zero);
            simulation.SetPower(DeviceId.Edge01, true);
            Assert.That(remote.Execute("hostname").Text, Does.Contain("core01"));
        }
        [TestCase("cat 'broken")]
        [TestCase("ps | cat")]
        [TestCase("pwd extra")]
        [TestCase("ssh nowhere")]
        [TestCase("kill 1")]
        [TestCase("kill -1")]
        [TestCase("systemctl disable unknown")]
        [TestCase("cd /missing")]
        public void InvalidCommandsDoNotAdvanceMission(string command)
        {
            var simulation = new FacilitySimulation(MissionDefinition.NightShift); var terminal = new TerminalSession(simulation, DeviceId.Ws07);
            Assert.That(terminal.Execute(command).ExitCode, Is.EqualTo(1));
            Assert.That(simulation.Mission.CompletedObjectives, Is.Zero);
        }
        [Test]
        public void MissionDefinitionCanChangeObjectivesWithoutChangingCommandHandlers()
        {
            var definition = new MissionDefinition("Control audit completed",
                new ObjectiveDefinition(ObjectiveCondition.InspectProcesses, DeviceId.Edge01, "Inspect EDGE"),
                new ObjectiveDefinition(ObjectiveCondition.InspectProcesses, DeviceId.Core01, "Inspect CORE"));
            var simulation = new FacilitySimulation(definition);
            new TerminalSession(simulation, DeviceId.Ws07).Execute("ps");
            Assert.That(simulation.Mission.CompletedObjectives, Is.Zero);
            new TerminalSession(simulation, DeviceId.Edge01).Execute("ps");
            Assert.That(simulation.Objective, Is.EqualTo("Inspect CORE"));
            new TerminalSession(simulation, DeviceId.Core01).Execute("ps");
            Assert.That(simulation.Mission.IsComplete, Is.True);
            Assert.That(simulation.Objective, Is.EqualTo("Control audit completed"));
        }
        [Test]
        public void StoppingWithoutDisablingAutostartDoesNotCompleteIsolation()
        {
            var world = new FacilitySimulation(MissionDefinition.NightShift); var terminal = new TerminalSession(world, DeviceId.Ws07);
            terminal.Execute("ps"); terminal.Execute("systemctl stop cache-sync");
            Assert.That(world.SourceIsolated, Is.False);
            terminal.Execute("systemctl disable cache-sync");
            Assert.That(world.SourceIsolated, Is.True);
            Assert.That(world.Mission.CompletedObjectives, Is.EqualTo(2));
        }
        [Test]
        public void WatcherUsesTheSamePowerAndIsolationRulesAsRemoteDevices()
        {
            var brain = new WatcherState();
            brain.Observe(false, true, false); Assert.That(brain.Mode, Is.EqualTo(WatcherMode.Patrol));
            brain.Observe(false, true, true); Assert.That(brain.Mode, Is.EqualTo(WatcherMode.Alert));
            brain.Observe(false, false, true); Assert.That(brain.Mode, Is.EqualTo(WatcherMode.Offline));
            brain.Observe(true, true, true); Assert.That(brain.Mode, Is.EqualTo(WatcherMode.Maintenance));
        }
        [Test]
        public void WorldSnapshotRestoresFilesPowerCwdProcessesAndServiceRestartSequence()
        {
            var world = new FacilitySimulation(MissionDefinition.NightShift); var shell = new TerminalSession(world, DeviceId.Ws07);
            shell.Execute("ps"); shell.Execute("kill 431"); shell.Execute("cd /var/log");
            world[DeviceId.Core01].WriteFile("/tmp/player/note.txt", "свой файл");
            world.SetPower(DeviceId.Edge01, false); world.RecordWatcherContact();
            var restored = FacilitySimulation.Restore(world.Capture());
            Assert.That(restored.Mission.CompletedObjectives, Is.EqualTo(1));
            Assert.That(restored.WatcherContacts, Is.EqualTo(1));
            Assert.That(restored[DeviceId.Edge01].Powered, Is.False);
            Assert.That(restored[DeviceId.Ws07].WorkingDirectory, Is.EqualTo("/var/log"));
            Assert.That(restored[DeviceId.Core01].TryRead("/tmp/player/note.txt", out string note), Is.True);
            Assert.That(note, Is.EqualTo("свой файл"));
            Assert.That(new TerminalSession(restored, DeviceId.Ws07).Execute("kill 432").Text, Does.Contain("pid=433"));
            shell = new TerminalSession(restored, DeviceId.Ws07);
            shell.Execute("systemctl disable cache-sync"); shell.Execute("systemctl stop cache-sync");
            restored = FacilitySimulation.Restore(restored.Capture());
            Assert.That(restored.SourceIsolated, Is.True); Assert.That(restored.Mission.CompletedObjectives, Is.EqualTo(2));
            Assert.That(restored[DeviceId.Ws07].LastCommand, Is.EqualTo("systemctl stop cache-sync"));
            new TerminalSession(restored, DeviceId.Core01).Execute("ps");
            Assert.That(FacilitySimulation.Restore(restored.Capture()).Mission.IsComplete, Is.True);
        }
        [Test]
        public void WorldRestoreRejectsConflictingDevicesPathsAndMissionState()
        {
            var data = new FacilitySimulation(MissionDefinition.NightShift).Capture(); data.computers[1].id = data.computers[0].id;
            Assert.Throws<System.ArgumentException>(() => FacilitySimulation.Restore(data));
            data = new FacilitySimulation(MissionDefinition.NightShift).Capture(); data.computers[0].files[0].path = "/../../escape";
            Assert.Throws<System.ArgumentException>(() => FacilitySimulation.Restore(data));
            data = new FacilitySimulation(MissionDefinition.NightShift).Capture(); data.completedObjectives = 2;
            Assert.Throws<System.ArgumentException>(() => FacilitySimulation.Restore(data));
            data = new FacilitySimulation(MissionDefinition.NightShift).Capture(); data.nextPid = 431;
            Assert.Throws<System.ArgumentException>(() => FacilitySimulation.Restore(data));
        }
        [Test]
        public void TerminalMemorySurvivesWorldRestoreWithoutMixingComputers()
        {
            var world=new FacilitySimulation(MissionDefinition.NightShift); var memory=world[DeviceId.Ws07].Terminal;
            memory.Initialized=true; memory.Host=DeviceId.Core01; memory.Draft="cat /var/log/"; memory.Scroll=125;
            memory.AddCommand("ssh core01"); memory.AddLine("connected to core01","result");
            var restored=FacilitySimulation.Restore(world.Capture()); var saved=restored[DeviceId.Ws07].Terminal;
            Assert.That(saved.Host,Is.EqualTo(DeviceId.Core01)); Assert.That(saved.Draft,Is.EqualTo("cat /var/log/"));
            Assert.That(saved.Scroll,Is.EqualTo(125)); Assert.That(saved.History,Does.Contain("ssh core01"));
            Assert.That(saved.Lines.Single().text,Is.EqualTo("connected to core01"));
            Assert.That(restored[DeviceId.Edge01].Terminal.Lines,Is.Empty);
            saved.Clear(); restored=FacilitySimulation.Restore(restored.Capture());
            Assert.That(restored[DeviceId.Ws07].Terminal.Initialized,Is.True); Assert.That(restored[DeviceId.Ws07].Terminal.Lines,Is.Empty);
            Assert.That(restored[DeviceId.Ws07].Terminal.History,Is.Not.Empty);
        }
        [Test]
        public void ResumeRemoteSessionAndExitWorksEvenIfTheLinkIsLost()
        {
            var world=new FacilitySimulation(MissionDefinition.NightShift); var shell=new TerminalSession(world,DeviceId.Ws07); shell.Resume(DeviceId.Core01);
            world.SetPower(DeviceId.Edge01,false); Assert.That(shell.Execute("ps").ExitCode,Is.EqualTo(1));
            Assert.That(shell.Execute("exit").ExitCode,Is.Zero); Assert.That(shell.Host,Is.EqualTo(DeviceId.Ws07));
            Assert.That(shell.Execute("ps").ExitCode,Is.Zero);
        }
        [Test]
        public void CommandsOwnTheTranscriptAndBothPresentationsShareClearAndRemoteState()
        {
            var world=new FacilitySimulation(MissionDefinition.NightShift); var shell=new TerminalSession(world,DeviceId.Ws07);
            Assert.That(TerminalDisplay.Read(world,DeviceId.Ws07).Transcript,Does.Contain("431"));
            Assert.That(world.Mission.CompletedObjectives,Is.Zero,"Drawing a process list must not execute ps");
            shell.Execute("pwd"); shell.Execute("ssh core01"); shell.Execute("cat /var/log/super.log");
            var display=TerminalDisplay.Read(world,DeviceId.Ws07);
            Assert.That(display.Header,Does.Contain("CORE01")); Assert.That(display.Transcript,Does.Contain("выбран лидер"));
            Assert.That(world[DeviceId.Ws07].Terminal.History.Count,Is.EqualTo(3));
            Assert.That(new TerminalSession(world,DeviceId.Ws07).Host,Is.EqualTo(DeviceId.Core01));
            shell.Execute("clear"); Assert.That(display.Transcript,Is.Empty); Assert.That(display.Tail(52,12),Is.Empty);
            var restored=FacilitySimulation.Restore(world.Capture());
            Assert.That(TerminalDisplay.Read(restored,DeviceId.Ws07).Transcript,Is.Empty);
            Assert.That(TerminalDisplay.Read(restored,DeviceId.Ws07).Header,Does.Contain("CORE01"));
        }
        [Test]
        public void StateChangesAreObservableAndSessionResetIsIsolated()
        {
            var simulation = new FacilitySimulation(MissionDefinition.NightShift); int events = 0; simulation.StateChanged += () => events++;
            simulation.SetPower(DeviceId.Edge01, false); simulation.SetPower(DeviceId.Edge01, false);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(new FacilitySimulation(MissionDefinition.NightShift)[DeviceId.Edge01].Powered, Is.True);
        }
    }
}
