using UnityEngine;

namespace Hostfall.Rendering
{
    [RequireComponent(typeof(TextMesh),typeof(MeshRenderer))]
    public sealed class WorldTextDepth : MonoBehaviour
    {
        [SerializeField] private Shader depthShader;
        private Material material;
        private TextMesh label;
        public void Configure(Shader shader) { depthShader=shader; }
        private void OnEnable()
        {
            label=GetComponent<TextMesh>();
            if (label.font==null || depthShader==null) return;
            material=new Material(depthShader) { name="Depth-tested text / "+name, mainTexture=label.font.material.mainTexture };
            GetComponent<MeshRenderer>().sharedMaterial=material;
            Font.textureRebuilt+=Rebuilt;
        }
        private void Rebuilt(Font font) { if (material!=null && font==label.font) material.mainTexture=font.material.mainTexture; }
        private void LateUpdate() { if(material!=null && material.mainTexture!=label.font.material.mainTexture) material.mainTexture=label.font.material.mainTexture; }
        private void OnDisable()
        { Font.textureRebuilt-=Rebuilt; if(material!=null) Destroy(material); material=null; }
    }
}
