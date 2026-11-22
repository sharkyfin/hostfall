using UnityEngine;

namespace Hostfall.Rendering
{
    [RequireComponent(typeof(TextMesh),typeof(MeshRenderer))]
    public sealed class WorldTextDepth : MonoBehaviour
    {
        [SerializeField] private Shader depthShader;
        private Material material;
        private Material original;
        private TextMesh label;
        private MeshRenderer textRenderer;
        public void Configure(Shader shader) { depthShader=shader; }

        public static Material CreateDepthMaterial(TextMesh text, Shader shader)
        {
            var depth = new Material(shader)
            {
                name = "Depth-tested world text",
                mainTexture = text.font.material.mainTexture,
                hideFlags = HideFlags.HideAndDontSave
            };
            depth.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            return depth;
        }

        private void OnEnable()
        {
            label=GetComponent<TextMesh>();
            if (label.font==null || depthShader==null) return;
            textRenderer = GetComponent<MeshRenderer>();
            original = textRenderer.sharedMaterial;
            material = CreateDepthMaterial(label, depthShader);
            textRenderer.sharedMaterial = material;
            Font.textureRebuilt+=Rebuilt;
        }
        private void Rebuilt(Font font) { if (material!=null && font==label.font) material.mainTexture=font.material.mainTexture; }
        // Atlas changes are delivered by Font.textureRebuilt; no per-label frame polling.
        private void OnDisable()
        {
            Font.textureRebuilt -= Rebuilt;
            if (material == null) return;
            if (textRenderer.sharedMaterial == material) textRenderer.sharedMaterial = original;
            Destroy(material);
            material = null;
        }
    }
}
