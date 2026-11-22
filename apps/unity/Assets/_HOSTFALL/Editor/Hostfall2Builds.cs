using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Build macOS Candidate")]
        public static void BuildMacCandidate()
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64;
            BuildCandidate(BuildTarget.StandaloneOSX, NamedBuildTarget.Standalone, "Builds/macOS/HOSTFALL-2-Candidate.app", false);
        }

        [MenuItem("HOSTFALL/2.0/Build macOS Profile")]
        public static void BuildMacProfile()
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64;
            BuildCandidate(BuildTarget.StandaloneOSX, NamedBuildTarget.Standalone, "Builds/macOS/HOSTFALL-2-Profile.app", true);
        }

        public static void BuildWebCandidate()
        {
            BuildCandidate(BuildTarget.WebGL, NamedBuildTarget.WebGL, "Builds/Web/HOSTFALL-2-Candidate", false);
        }

        private static void BuildCandidate(BuildTarget target, NamedBuildTarget namedTarget, string destination, bool profile)
        {
            WorkstationWorkshop.RequireCopy();
            string previousIdentifier = PlayerSettings.GetApplicationIdentifier(namedTarget);
            string previousProduct = PlayerSettings.productName;
            bool previousFallback = PlayerSettings.WebGL.decompressionFallback;
            bool useDefaultApis = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL);
            var oldApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
            try
            {
                PlayerSettings.productName = profile ? "HOSTFALL Workbench Profile" : "HOSTFALL 2 Preview";
                PlayerSettings.SetApplicationIdentifier(namedTarget, profile ? "com.northline.hostfall.computerqa" : "com.northline.hostfall.preview");
                if (target == BuildTarget.WebGL)
                {
                    PlayerSettings.WebGL.decompressionFallback = true;
                    PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                    PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.OpenGLES3 });
                }
                var build = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { PreviewScene },
                    target = target,
                    locationPathName = destination,
                    options = profile ? BuildOptions.Development : BuildOptions.None
                });
                string report = $"Result {build.summary.result}\nErrors {build.summary.totalErrors}\nWarnings {build.summary.totalWarnings}\nBytes {build.summary.totalSize}\nSeconds {build.summary.totalTime.TotalSeconds:F2}\nDestination {Path.GetFullPath(destination)}\n";
                File.WriteAllText(Path.Combine(Output, "candidate-" + (profile ? "profile" : target.ToString()) + "-build.txt"), report);
                if (build.summary.result != BuildResult.Succeeded) throw new InvalidOperationException(report);
            }
            finally
            {
                PlayerSettings.productName = previousProduct;
                PlayerSettings.SetApplicationIdentifier(namedTarget, previousIdentifier);
                if (target == BuildTarget.WebGL)
                {
                    PlayerSettings.WebGL.decompressionFallback = previousFallback;
                    PlayerSettings.SetGraphicsAPIs(target, oldApis);
                    PlayerSettings.SetUseDefaultGraphicsAPIs(target, useDefaultApis);
                }
            }
        }
    }
}
