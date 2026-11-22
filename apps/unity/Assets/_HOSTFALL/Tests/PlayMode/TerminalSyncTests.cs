using System.Collections;
using System.Linq;
using Hostfall.Core;
using Hostfall.Devices;
using Hostfall.Simulation;
using Hostfall.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace Hostfall.Tests
{
    public sealed class TerminalSyncTests
    {
        [UnityTest] public IEnumerator AuthoredTypingSharesPhysicalDisplayAndPausesOutsideItsView()
        {
            yield return SceneManager.LoadSceneAsync("NorthlineFacility"); yield return null;
            var world=Object.FindAnyObjectByType<FacilityRoot>().Simulation;
            var ui=Object.FindAnyObjectByType<FacilityUI>(); var root=Object.FindAnyObjectByType<Hostfall.UI.FacilityUI>().GetComponent<UIDocument>().rootVisualElement;
            var controls=Object.FindAnyObjectByType<ControlState>();
            var screen=Object.FindObjectsByType<ComputerTerminal>(FindObjectsSortMode.None).Single(t=>t.Id==DeviceId.Ws07).GetComponent<ComputerScreen>();
            Assert.That(root.Q<TextField>("command"),Is.Null); Assert.That(root.Q<Button>("execute"),Is.Null);
            ui.OpenTerminal(DeviceId.Ws07);
            var memory=world[DeviceId.Ws07].Terminal;
            float typingDeadline = Time.realtimeSinceStartup + 3;
            while (memory.Draft.Length == 0 && Time.realtimeSinceStartup < typingDeadline)
                yield return null;
            Assert.That(memory.Draft.Length, Is.GreaterThan(0), "The focused screen starts typing within the observation window");
            Assert.That(ui.TerminalView.Q<Label>("automatic-command").text,Is.EqualTo(memory.Draft));
            Assert.That(screen.PromptText,Is.EqualTo(TerminalDisplay.Read(world,DeviceId.Ws07).Prompt+" "+memory.Draft));
            controls.SetMode(ControlMode.Paused); string draft=memory.Draft; int count=memory.History.Count;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(memory.Draft,Is.EqualTo(draft)); Assert.That(memory.History.Count,Is.EqualTo(count));
            MenuFlowTests.FinishRoutine(world,DeviceId.Ws07);
            ui.OpenTerminal(DeviceId.Ws07); yield return null;
            var display=TerminalDisplay.Read(world,DeviceId.Ws07);
            Assert.That(string.Join("\n\n",ui.TerminalView.Q<ScrollView>("output").Query<Label>().ToList().Select(l=>l.text)),Is.EqualTo(display.Transcript));
            Assert.That(screen.DisplayedText,Is.EqualTo(display.Tail(62,15)));
            int completedCount=memory.History.Count;
            controls.SetMode(ControlMode.Exploring); ui.OpenTerminal(DeviceId.Ws07); yield return null;
            Assert.That(memory.History.Count,Is.EqualTo(completedCount),"Reopening a finished presentation cannot reapply rewards");
            var restored=FacilitySimulation.Restore(world.Capture());
            Assert.That(restored[DeviceId.Ws07].Terminal.History,Is.EqualTo(memory.History));
            Assert.That(restored.PulseUnlocked,Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
