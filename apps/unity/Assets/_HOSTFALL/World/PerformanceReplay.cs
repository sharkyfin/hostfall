using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Hostfall.Core;
using Hostfall.Persistence;
using Hostfall.Player;
using Unity.Profiling;
using UnityEngine;

namespace Hostfall.World
{
    // Explicit diagnostic player only. No replay, file I/O or preference changes in normal play.
    public sealed class PerformanceReplay : MonoBehaviour
    {
        public static bool Active {get;private set;}
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Prepare()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-hostfall-profile");
            if(index<0||index+1>=args.Length)return;
            Active=true;Application.runInBackground=true;
            output=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(output);
            WorldSaveController.FreshStartRequested=true;WorldSaveController.TestStorageDirectory=Path.Combine(output,"private-saves");
            var go=new GameObject("Explicit performance replay");DontDestroyOnLoad(go);go.AddComponent<PerformanceReplay>();
        }
        private IEnumerator Start()
        {
            if(output==null){Destroy(gameObject);yield break;}
            yield return null;yield return null;yield return null;
            File.WriteAllText(Path.Combine(output,"started.json"),"{\"started\":true}");
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hostfall2-input-hold")>=0)
            {
                if(Application.identifier!="com.northline.hostfall.computerqa")throw new InvalidOperationException("Input QA requires isolated bundle");
                var ui=FindAnyObjectByType<Hostfall.UI.FacilityUI>();ui.BeginSession();ui.Opening.Skip();
                FindAnyObjectByType<FacilityRoot>().Field.AcknowledgeIntroduction();FindAnyObjectByType<ControlState>().SetMode(ControlMode.Exploring);
                var station=Array.Find(FindObjectsByType<Hostfall.Devices.ComputerStation>(FindObjectsSortMode.None),s=>s.StationId=="pc.home");
                FindAnyObjectByType<Hostfall.UI.ComputerRuntimeUI>().Open(station);
                yield break;
            }
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hostfall2-loop")>=0)
            {yield return ComputerWorldReview.Run(output);Application.Quit(0);yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hostfall-awakening")>=0)
            {yield return ReviewAwakening();Application.Quit(0);yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hostfall-screens")>=0)
            {yield return ReviewScreens();Application.Quit(0);yield break;}
            var controls=FindAnyObjectByType<ControlState>();var player=FindAnyObjectByType<PlayerMotor>();var health=player.GetComponent<PlayerHealth>();
            player.enabled=false;controls.SetMode(ControlMode.Exploring);
            int vsync=QualitySettings.vSyncCount,rate=Application.targetFrameRate;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            using var cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
            using var render=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread",1);
            using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            using var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);
            var receipt=new Receipt{device=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),width=Screen.width,height=Screen.height,cpuValid=cpu.Valid,renderValid=render.Valid,gcValid=gc.Valid,drawsValid=draws.Valid,trianglesValid=triangles.Valid};
            var positions=new[]{new Vector3(0,.05f,-4),new Vector3(0,.05f,6),new Vector3(0,.05f,25),new Vector3(-16,.05f,26),new Vector3(30,.05f,72),new Vector3(87,.05f,193)};
            var yaws=new[]{0f,0f,180f,270f,180f,180f};var names=new[]{"spawn","corridor","compute","guardian","robotics","finale"};
            if (FindAnyObjectByType<FacilityRoot>().Simulation.InteractiveComputers)
            {
                positions = new[] { new Vector3(-2.6f, .08f, -14.3f), new Vector3(.05f, .08f, 4.04f),
                    new Vector3(0, .05f, 25), new Vector3(-16, .05f, 26), new Vector3(8.5f, 1.05f, 73.5f),
                    new Vector3(10, .05f, 136), new Vector3(10, .05f, 183.35f) };
                yaws = new[] { 180f, 90f, 180f, 270f, 0, 0, 0 };
                names = new[] { "shelter", "service-passage", "compute", "mantis", "logistics", "super", "trust" };
            }
            var timing=new FrameTiming[1];
            for(int view=0;view<positions.Length;view++)
            {
                var state=player.Capture(false);state.position=positions[view];state.yaw=yaws[view];state.pitch=0;state.targetPitch=0;state.hasWeapon=true;player.Restore(state);health.ResetHealth();
                for(int frame=0;frame<135;frame++)
                {
                    if(health.CurrentHealth<70)health.ResetHealth();if(controls.Mode!=ControlMode.Exploring)controls.SetMode(ControlMode.Exploring);
                    FrameTimingManager.CaptureFrameTimings();yield return null;
                    if(frame==30)ScreenCapture.CaptureScreenshot(Path.Combine(output,names[view]+".png"));
                    if(frame<45)continue;
                    bool valid=FrameTimingManager.GetLatestTimings(1,timing)>0;
                    receipt.samples.Add(new Sample{view=names[view],delta=Time.unscaledDeltaTime*1000,cpu=cpu.LastValue/1e6,render=render.LastValue/1e6,gc=gc.LastValue,draws=draws.LastValue,triangles=triangles.LastValue,timingValid=valid,timingCpu=valid?timing[0].cpuFrameTime:0,gpu=valid?timing[0].gpuFrameTime:0});
                }
            }
            QualitySettings.vSyncCount=vsync;Application.targetFrameRate=rate;
            File.WriteAllText(Path.Combine(output,"native-profile.json"),JsonUtility.ToJson(receipt));
            Application.Quit(0);
        }
        private IEnumerator ReviewScreens()
        {
            var ui=FindAnyObjectByType<Hostfall.UI.FacilityUI>();var controls=FindAnyObjectByType<ControlState>();var motor=FindAnyObjectByType<PlayerMotor>();motor.enabled=false;
            var rig=FindAnyObjectByType<Hostfall.CameraSystem.PlayerCameraRig>();var document=ui.GetComponent<UnityEngine.UIElements.UIDocument>();
            var rows=new List<string>();
            foreach(var id in new[]{Hostfall.Simulation.DeviceId.Data01,Hostfall.Simulation.DeviceId.Pwr01,Hostfall.Simulation.DeviceId.Neuro01,Hostfall.Simulation.DeviceId.Cargo01})
            {
                var surface=System.Linq.Enumerable.Single(FindObjectsByType<Hostfall.Devices.WorkstationSurface>(FindObjectsSortMode.None),s=>s.Device==id);
                controls.SetMode(ControlMode.Exploring);var state=motor.Capture(false);state.position=surface.Centre+surface.Normal*1.5f-Vector3.up*1.5f;state.yaw=Quaternion.LookRotation(-surface.Normal).eulerAngles.y;state.pitch=0;state.targetPitch=0;motor.Restore(state);
                yield return new WaitForSecondsRealtime(.6f);
                ui.OpenTerminalScenario(Hostfall.Simulation.TerminalScenarioCatalog.GetScenario(id));
                yield return new WaitForSecondsRealtime(3);
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"screen-"+id+".png"));
                var panel=ui.TerminalView;
                rows.Add(id+" ready="+rig.FocusReady+" output="+Screen.width+"x"+Screen.height+" system="+Display.main.systemWidth+"x"+Display.main.systemHeight+" mode="+Screen.fullScreenMode+" scale="+((UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline).renderScale+" rect="+panel.worldBound);
                yield return new WaitForSecondsRealtime(.2f);
            }
            File.WriteAllLines(Path.Combine(output,"screens.txt"),rows);
        }
        private IEnumerator ReviewAwakening()
        {
            var ui=FindAnyObjectByType<Hostfall.UI.FacilityUI>();var controls=FindAnyObjectByType<ControlState>();var motor=FindAnyObjectByType<PlayerMotor>();
            ui.BeginSession();yield return new WaitForSecondsRealtime(1.7f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"opening-body.png"));
            yield return new WaitForSecondsRealtime(4.2f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"opening-eye.png"));yield return new WaitForSecondsRealtime(.2f);ui.Opening.Skip();
            FindAnyObjectByType<FacilityRoot>().Field.AcknowledgeIntroduction();controls.SetMode(ControlMode.Exploring);motor.enabled=false;
            var rows=new List<string>();var surface=System.Linq.Enumerable.Single(FindObjectsByType<Hostfall.Devices.WorkstationSurface>(FindObjectsSortMode.None),s=>s.Device==Hostfall.Simulation.DeviceId.Data01);
            var state=motor.Capture(false);state.position=surface.Centre+surface.Normal*1.4f-Vector3.up*1.65f;state.yaw=Quaternion.LookRotation(-surface.Normal).eulerAngles.y;state.pitch=0;state.targetPitch=0;motor.Restore(state);
            var scenario=Hostfall.Simulation.TerminalScenarioCatalog.GetScenario(surface.Device);var world=FindAnyObjectByType<FacilityRoot>().Simulation;var session=world.Screens.Present(scenario);for(int i=0;i<5000;i++)session.Tick(.1f);
            yield return new WaitForSecondsRealtime(.8f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"terminal-near.png"));yield return null;rows.Add("near surface="+(ui.PhysicalDisplay.Surface!=null?ui.PhysicalDisplay.Surface.Device.ToString():"none")+" RT="+ui.PhysicalDisplay.Texture.width+"x"+ui.PhysicalDisplay.Texture.height);
            ui.OpenTerminalScenario(scenario);yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"terminal-approach.png"));yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"terminal-focused.png"));yield return null;
            var previous=RenderTexture.active;RenderTexture.active=ui.PhysicalDisplay.Texture;var shot=new Texture2D(ui.PhysicalDisplay.Texture.width,ui.PhysicalDisplay.Texture.height,TextureFormat.RGBA32,false);shot.ReadPixels(new Rect(0,0,shot.width,shot.height),0,0);shot.Apply();File.WriteAllBytes(Path.Combine(output,"terminal-texture.png"),shot.EncodeToPNG());RenderTexture.active=previous;Destroy(shot);
            controls.SetMode(ControlMode.Exploring);yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"terminal-left.png"));
            foreach(var point in new[]{new Vector3(-1.9f,0,-1.4f),new Vector3(1.9f,0,-1.4f),new Vector3(0,0,2.5f)})
            {state=motor.Capture(false);state.position=point;state.yaw=point.z>1?90:0;state.pitch=0;state.targetPitch=0;motor.Restore(state);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"note-"+rows.Count+".png"));yield return null;rows.Add("note "+point);}
            yield return new WaitForSecondsRealtime(.2f);File.WriteAllLines(Path.Combine(output,"review.txt"),rows);
        }
        [Serializable]private class Receipt{public string device,api;public int width,height;public bool cpuValid,renderValid,gcValid,drawsValid,trianglesValid;public List<Sample> samples=new();}
        [Serializable]private class Sample{public string view;public double delta,cpu,render,timingCpu,gpu;public long gc,draws,triangles;public bool timingValid;}
#endif
    }
}
