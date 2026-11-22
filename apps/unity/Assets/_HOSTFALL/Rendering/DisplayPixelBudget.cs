using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hostfall.Rendering
{
    // Native output keeps text sharp. Only the expensive 3D surface uses the Full HD budget.
    public sealed class DisplayPixelBudget : MonoBehaviour
    {
        public const int PixelBudget=1920*1080;
        private int width,height;
        private UniversalRenderPipelineAsset pipeline;
        private float previous;
        public static float ScaleFor(int w,int h)=>Mathf.Min(1,(float)System.Math.Sqrt(PixelBudget/(Mathf.Max(1,w)*(double)Mathf.Max(1,h))));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if(Application.isEditor)return;
            var go=new GameObject("Display pixel budget");DontDestroyOnLoad(go);go.AddComponent<DisplayPixelBudget>();
        }
        private System.Collections.IEnumerator Start()
        {
            // Let Unity finish the native window's asynchronous initial mode change.
            yield return null;
            // One startup migration from the old letterboxed profile, never on scene load.
            if(!Application.isBatchMode&&PlayerPrefs.GetInt("hostfall.display-budget-version",0)<2)
            {
                if(Screen.fullScreen)
                {Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);}
                PlayerPrefs.SetInt("hostfall.display-budget-version",2);PlayerPrefs.Save();
            }
        }
        private void OnEnable()
        {
            pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline!=null)previous=pipeline.renderScale;
            RenderPipelineManager.beginContextRendering+=BeforeRender;
        }
        private void BeforeRender(ScriptableRenderContext context,System.Collections.Generic.List<Camera> cameras)
        {
            if(pipeline==null){pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(pipeline==null)return;previous=pipeline.renderScale;}
            if(width==Screen.width&&height==Screen.height)return;
            width=Screen.width;height=Screen.height;pipeline.renderScale=ScaleFor(width,height);
        }
        private void OnDisable()
        {RenderPipelineManager.beginContextRendering-=BeforeRender;if(pipeline!=null)pipeline.renderScale=previous;}
    }
}
