using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hostfall.Editor
{
    // Project opt-in only. Does not load/save scenes or run in test/build copies.
    [InitializeOnLoad]
    public static class HostfallEditorTools
    {
        static HostfallEditorTools()
        {
            if(Application.isBatchMode||Application.dataPath.Contains("/.artifacts/"))return;
            string root=Path.GetFullPath(Application.dataPath+"/../../..");
            if(!File.Exists(root+"/.artifacts/tooling/enable-editor-tools"))return;
            EditorApplication.delayCall+=Connect;
        }
        [MenuItem("HOSTFALL/Tools/Connect Local Editor Bridge")]
        public static async void Connect()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Connect;return;}
            string root=Path.GetFullPath(Application.dataPath+"/../../..");
            var locator=Type.GetType("MCPForUnity.Editor.Services.MCPServiceLocator, MCPForUnity.Editor");
            if(locator==null){Debug.LogWarning("HOSTFALL tools: MCP for Unity is not installed.");return;}
            try
            {
                EditorPrefs.SetBool("MCPForUnity.UseHttpTransport",true);
                EditorPrefs.SetString("MCPForUnity.HttpTransportScope","local");
                EditorPrefs.SetString("MCPForUnity.HttpUrl","http://127.0.0.1:8080");
                EditorPrefs.SetBool("MCPForUnity.TelemetryDisabled",true);
                EditorPrefs.SetString("MCPForUnity.UvxPath","/Users/mac/.local/bin/uvx");
                object bridge=locator.GetProperty("Bridge",BindingFlags.Public|BindingFlags.Static).GetValue(null);
                var task=(Task<bool>)bridge.GetType().GetMethod("StartAsync").Invoke(bridge,null);
                bool connected=await task;
                Directory.CreateDirectory(root+"/.artifacts/tooling");
                File.WriteAllText(root+"/.artifacts/tooling/editor-bridge-status.json",JsonUtility.ToJson(new Status
                {connected=connected,unityVersion=Application.unityVersion,scene=EditorSceneManager.GetActiveScene().path,sceneDirty=EditorSceneManager.GetActiveScene().isDirty},true));
                Debug.Log("HOSTFALL local editor bridge: "+connected);
            }
            catch(Exception e){Debug.LogWarning("HOSTFALL local editor bridge: "+e.GetBaseException().Message);}
        }
        [Serializable] private class Status { public bool connected,sceneDirty;public string unityVersion,scene; }
    }
}
