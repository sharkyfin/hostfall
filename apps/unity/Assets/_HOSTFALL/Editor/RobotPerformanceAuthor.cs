using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Hostfall.Editor
{
    public static class RobotPerformanceAuthor
    {
        [MenuItem("HOSTFALL/Tools/Build Robot Profile Baseline")]
        public static void Baseline()=>Build("HOSTFALL-Profile-Before",true);
        [MenuItem("HOSTFALL/Tools/Build Robot Profile After")]
        public static void After()=>Build("HOSTFALL-Profile-After",true);
        [MenuItem("HOSTFALL/Tools/Build Robot Release")]
        public static void Release()=>Build("HOSTFALL-Robots",false,true);
        [MenuItem("HOSTFALL/Tools/Build GPU Resident Profile")]
        public static void Resident()=>Variant("HOSTFALL-Profile-Resident",false);
        [MenuItem("HOSTFALL/Tools/Build GPU Occlusion Profile")]
        public static void Occlusion()=>Variant("HOSTFALL-Profile-Occlusion",true);
        [MenuItem("HOSTFALL/Tools/Select GPU Resident Rendering")]
        public static void SelectResident()=>SelectRendering(false);
        [MenuItem("HOSTFALL/Tools/Select GPU Occlusion Rendering")]
        public static void SelectOcclusion()=>SelectRendering(true);
        private static void SelectRendering(bool occlusion)
        {
            if(!Application.dataPath.Contains("/.artifacts/"))throw new InvalidOperationException("Experimental GPU settings are restricted to the verification copy.");
            var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/_HOSTFALL/Content/Settings/FacilityURP.asset");
            SetMode(pipeline,1,occlusion);RequireVariants();
            EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();
        }
        private static void Variant(string name,bool occlusion)
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/_HOSTFALL/Content/Settings/FacilityURP.asset");
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            int oldStripping=graphics.FindProperty("m_BrgStripping").intValue;
            var serialized=new SerializedObject(pipeline);int mode=serialized.FindProperty("m_GPUResidentDrawerMode").intValue;bool oldOcclusion=serialized.FindProperty("m_GPUResidentDrawerEnableOcclusionCullingInCameras").boolValue;
            try
            {
                SetMode(pipeline,1,occlusion);RequireVariants();
                EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssets();Build(name,true,true);
            }
            finally
            {
                SetMode(pipeline,mode,oldOcclusion);EditorUtility.SetDirty(pipeline);
                graphics.Update();graphics.FindProperty("m_BrgStripping").intValue=oldStripping;graphics.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            }
        }
        private static void RequireVariants()
        {
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            settings.FindProperty("m_BrgStripping").intValue=(int)UnityEditor.Rendering.BatchRendererGroupStrippingMode.KeepAll;
            settings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            if(UnityEditor.Rendering.EditorGraphicsSettings.batchRendererGroupShaderStrippingMode!=UnityEditor.Rendering.BatchRendererGroupStrippingMode.KeepAll)throw new InvalidOperationException("BRG shader variants were not retained");
        }
        private static void SetMode(UnityEngine.Object pipeline,int mode,bool occlusion)
        {
            var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_GPUResidentDrawerMode").intValue=mode;serialized.FindProperty("m_GPUResidentDrawerEnableOcclusionCullingInCameras").boolValue=occlusion;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Build(string name,bool development,bool clean=false)
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture=UnityEditor.Build.OSArchitecture.x64;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FacilityBuilder.ScenePath},locationPathName="Builds/macOS/"+name+".app",target=BuildTarget.StandaloneOSX,options=(development?BuildOptions.Development:BuildOptions.None)|(clean?BuildOptions.CleanBuildCache:BuildOptions.None)});
            const string path="../../.artifacts/robot-performance-20261115";Directory.CreateDirectory(path);
            File.WriteAllText(path+"/"+name+"-build.json",JsonUtility.ToJson(new Receipt{result=report.summary.result.ToString(),errors=report.summary.totalErrors,bytes=report.summary.totalSize}));
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException(name+" failed");
        }
        [Serializable]private class Receipt{public string result;public int errors;public ulong bytes;}
    }
}
