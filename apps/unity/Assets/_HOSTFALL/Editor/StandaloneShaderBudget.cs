using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hostfall.Editor
{
    internal sealed class StandaloneShaderBudget : IPreprocessShaders
    {
        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> variants)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) return;
            // The authored browser scene uses exponential-squared fog throughout.
            var linear = new ShaderKeyword("FOG_LINEAR");
            var exponential = new ShaderKeyword("FOG_EXP");
            for (int index = variants.Count - 1; index >= 0; index--)
            {
                var enabled = variants[index].shaderKeywordSet;
                if (enabled.IsEnabled(linear) || enabled.IsEnabled(exponential)) variants.RemoveAt(index);
            }
        }

    }
}
