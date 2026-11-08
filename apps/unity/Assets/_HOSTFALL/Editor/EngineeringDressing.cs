using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hostfall.Editor
{
    // A deliberately small selection: one notebook, one cart, one cooling assembly.
    internal static class EngineeringDressing
    {
        private const string Source="Assets/_HOSTFALL/Art/PolyHavenEngineering/";
        private const string Output=FacilityBuilder.Content+"/Prefabs/Engineering/";
        private static readonly Dictionary<string,Material> materials=new();
        internal static void Prepare()
        {
            Directory.CreateDirectory(Output); AssetDatabase.Refresh(); materials.Clear();
            foreach(string path in Directory.GetFiles(Source,"*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".jpg")||p.EndsWith(".png")))
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath(path);
                texture.textureType=path.Contains("nor_gl")?TextureImporterType.NormalMap:TextureImporterType.Default;
                texture.sRGBTexture=path.Contains("_diff_"); texture.maxTextureSize=1024; texture.SaveAndReimport();
            }
            Make("office_notepads","office_notepads_yellow_pad","Shift notebook");
            Make("industrial_storage_cart",null,"Maintenance cart");
            Make("modular_industrial_pipes_01","modular_industrial_pipes_01_pipe04","Cooling valve");
        }
        private static void Make(string id,string part,string name)
        {
            string path=Source+id+"/"+id+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path); importer.importAnimation=false; importer.importCameras=false; importer.importLights=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; importer.SaveAndReimport();
            var root=new GameObject(name); var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled=part==null||renderer.name==part;
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Material(id,m!=null?m.name:"")).ToArray();
            }
            var active=model.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            if(active.Length==0) throw new System.InvalidOperationException("Missing engineering mesh: "+part);
            Bounds bounds=active[0].bounds; foreach(var renderer in active.Skip(1)) bounds.Encapsulate(renderer.bounds);
            model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            // Uniform scaling preserves the original silhouette; FBX units are measured here.
            float target=id=="office_notepads"?.26f:id=="industrial_storage_cart"?1.05f:.55f;
            float dimension=id=="office_notepads"?Mathf.Max(bounds.size.x,bounds.size.z):bounds.size.y;
            float scale=target/Mathf.Max(.001f,dimension); root.transform.localScale=Vector3.one*scale;
            if(id=="industrial_storage_cart")
            { var box=root.AddComponent<BoxCollider>(); box.center=new Vector3(0,bounds.size.y/2,0); box.size=bounds.size; }
            PrefabUtility.SaveAsPrefabAsset(root,Output+name+".prefab");
            Debug.Log("HOSTFALL dressing "+name+" original bounds "+bounds.size+" uniform scale "+scale);
            Object.DestroyImmediate(root);
        }
        private static Material Material(string id,string sourceName)
        {
            string prefix=id;
            if(id=="modular_industrial_pipes_01") prefix+="_"+(sourceName.Contains("group02")?"group02":"group01");
            if(materials.TryGetValue(prefix,out var cached)) return cached;
            string output=FacilityBuilder.Content+"/Materials/Engineering "+prefix+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(output);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,output); }
            string textures=Source+id+"/Textures/";
            Texture2D Tex(string suffix)=>AssetDatabase.LoadAssetAtPath<Texture2D>(textures+prefix+suffix);
            material.SetColor("_BaseColor",Color.white); material.SetFloat("_Surface",0); material.SetFloat("_ZWrite",1); material.SetFloat("_AlphaClip",0);
            material.SetFloat("_SrcBlend",1); material.SetFloat("_DstBlend",0); material.renderQueue=2000; material.SetOverrideTag("RenderType","Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetTexture("_BaseMap",Tex("_diff_1k.jpg"));
            material.SetTexture("_BumpMap",Tex("_nor_gl_1k.jpg")); material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale",.65f);
            material.SetTexture("_MetallicGlossMap",Tex("_MetalSmooth.png")); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Smoothness",1);
            material.SetTexture("_OcclusionMap",Tex("_Occlusion.png")); material.EnableKeyword("_OCCLUSIONMAP"); material.SetFloat("_OcclusionStrength",.5f);
            material.enableInstancing=true; EditorUtility.SetDirty(material); materials.Add(prefix,material); return material;
        }
        internal static void Place(string name,Transform parent,Vector3 position,float yaw=0)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Output+name+".prefab"));
            go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        }
    }
}
