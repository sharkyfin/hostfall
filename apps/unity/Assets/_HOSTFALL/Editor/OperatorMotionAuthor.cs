using System;
using System.IO;
using Hostfall.Player;
using UnityEditor;
using UnityEngine;
namespace Hostfall.Editor
{
    public static class OperatorMotionAuthor
    {
        [MenuItem("HOSTFALL/Tools/Build Motion Review")]
        public static void Build()
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FacilityBuilder.ScenePath},locationPathName="Builds/macOS/HOSTFALL-Motion.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
            Directory.CreateDirectory("../../.artifacts/motion-combat-20261115");
            File.WriteAllText("../../.artifacts/motion-combat-20261115/desktop-result.json",JsonUtility.ToJson(new BuildReceipt{result=report.summary.result.ToString(),errors=report.summary.totalErrors,bytes=report.summary.totalSize}));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Motion build failed");
        }
        [Serializable]private class BuildReceipt{public string result;public int errors;public ulong bytes;}
        [Serializable]private class Source{public string[] joints;public Clip[] clips;}
        [Serializable]private class Clip{public string name;public float fps;public Frame[] frames;}
        [Serializable]private class Frame{public Vector3[] points;}
        [MenuItem("HOSTFALL/Tools/Import Type-IV Motion")]
        public static void Apply()
        {
            const string folder="Assets/_HOSTFALL/Resources/Motion";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var source=JsonUtility.FromJson<Source>(File.ReadAllText("Assets/_HOSTFALL/Art/QuaterniusMotion/TypeIVMotionSamples.json"));
            var library=AssetDatabase.LoadAssetAtPath<OperatorMotionLibrary>(folder+"/TypeIVMotionLibrary.asset");
            if(library==null){library=ScriptableObject.CreateInstance<OperatorMotionLibrary>();AssetDatabase.CreateAsset(library,folder+"/TypeIVMotionLibrary.asset");}
            library.joints=source.joints;library.clips=new MotionEntry[source.clips.Length];
            for(int n=0;n<source.clips.Length;n++)
            {
                var input=source.clips[n];string path=folder+"/"+input.name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.ClearCurves();clip.legacy=true;clip.frameRate=input.fps;
                clip.wrapMode=input.name.EndsWith("Loop")?WrapMode.Loop:WrapMode.ClampForever;
                for(int joint=0;joint<source.joints.Length;joint++)for(int axis=0;axis<3;axis++)
                {
                    var keys=new Keyframe[input.frames.Length];for(int f=0;f<keys.Length;f++)keys[f]=new Keyframe(f/input.fps,input.frames[f].points[joint][axis]);
                    var curve=new AnimationCurve(keys);for(int k=0;k<keys.Length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.Linear);}
                    clip.SetCurve(source.joints[joint],typeof(Transform),"localPosition."+"xyz"[axis],curve);
                }
                EditorUtility.SetDirty(clip);library.clips[n]=new MotionEntry{name=input.name,clip=clip};
            }
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();Debug.Log("HOSTFALL Type-IV: 12 CC0 actions imported and retargeted");
        }
    }
}
