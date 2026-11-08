using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hostfall.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hostfall.Editor
{
    internal static class ModularKit
    {
        private const string Source="Assets/_HOSTFALL/Art/QuaterniusSciFi";
        private const string Output=FacilityBuilder.Content+"/Prefabs/Kit";
        private static readonly Dictionary<string,Material> materials=new();
        private static readonly Dictionary<string,Vector3> sizes=new();
        internal static void Prepare()
        {
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Source+"/Textures","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=path.Contains("_Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=path.Contains("BaseColor")||path.Contains("Decals")||path.Contains("Emissive");
                importer.maxTextureSize=1024; importer.SaveAndReimport();
            }
            materials.Clear(); sizes.Clear(); var modules=new List<FacilityModule>();
            foreach(string path in Directory.GetFiles(Source+"/Models","*.fbx").OrderBy(p=>p))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path); importer.importAnimation=false; importer.importCameras=false; importer.importLights=false;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var root=new GameObject(Path.GetFileNameWithoutExtension(path));
                var child=UnityEngine.Object.Instantiate(model,root.transform); child.name="Geometry / Quaternius";
                foreach(var renderer in child.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Resolve(m!=null?m.name:"M_Black")).ToArray();
                var renderers=child.GetComponentsInChildren<Renderer>(); var bounds=renderers[0].bounds; foreach(var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                child.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                sizes[root.name]=bounds.size;
                if(root.name.Contains("Crate")||root.name=="Prop_Computer"||root.name.StartsWith("Wall")||root.name.StartsWith("Column"))
                { var collider=root.AddComponent<BoxCollider>(); collider.center=new Vector3(0,bounds.size.y/2,0); collider.size=bounds.size; }
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Output+"/"+root.name+".prefab");
                modules.Add(new FacilityModule { id=root.name,category=root.name.StartsWith("Prop")?"Equipment":"Structure",prefab=prefab,size=bounds.size });
                Debug.Log("HOSTFALL kit "+root.name+" "+bounds.size); UnityEngine.Object.DestroyImmediate(root);
            }
            var catalog=AssetDatabase.LoadAssetAtPath<FacilityModuleCatalog>(FacilityBuilder.Content+"/Settings/FacilityModules.asset");
            if(catalog==null) { catalog=ScriptableObject.CreateInstance<FacilityModuleCatalog>(); AssetDatabase.CreateAsset(catalog,FacilityBuilder.Content+"/Settings/FacilityModules.asset"); }
            catalog.modules=modules.ToArray(); EditorUtility.SetDirty(catalog);
        }
        private static Material Resolve(string name)
        {
            if(materials.TryGetValue(name,out var cached)) return cached;
            string path=FacilityBuilder.Content+"/Materials/Kit "+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.SetFloat("_Surface",0); material.SetFloat("_ZWrite",1); material.SetFloat("_SrcBlend",1); material.SetFloat("_DstBlend",0); material.renderQueue=2000; material.SetOverrideTag("RenderType","Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetFloat("_Smoothness",.35f);
            string trim=name.Contains("Trim_01")?"T_Trim_01":name.Contains("Trim_02")?"T_Trim_02":name.Contains("Trim_03")?"T_Trim_03":"";
            Color color=name.Contains("Black")||name.Contains("Screen")?new Color(.025f,.035f,.04f):new Color(.85f,.87f,.82f);
            if(name.Contains("Dark")) color=new Color(.44f,.5f,.48f);
            if(trim.Length>0)
            {
                var baseMap=Tex(trim+"_BaseColor"); material.SetTexture("_BaseMap",baseMap);
                material.SetTexture("_BumpMap",Tex(trim+"_Normal")); material.SetFloat("_BumpScale",.65f); material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_MetallicGlossMap",Tex(trim+"_MetalSmooth")); material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetTexture("_OcclusionMap",Tex(trim+"_Occlusion")); material.SetFloat("_OcclusionStrength",.7f); material.EnableKeyword("_OCCLUSIONMAP");
                var emission=Tex(trim+"_Emissive");
                if(emission!=null) { material.SetTexture("_EmissionMap",emission); material.SetColor("_EmissionColor",new Color(.08f,.75f,.85f)*1.3f); material.EnableKeyword("_EMISSION"); }
            }
            if(name.Contains("Light")) { color=new Color(.1f,.7f,.8f); material.SetColor("_EmissionColor",color*1.7f); material.EnableKeyword("_EMISSION"); }
            if(name.Contains("Decal") && !name.Contains("Screen"))
            {
                material.SetTexture("_BaseMap",Tex("T_Decals")); material.SetFloat("_AlphaClip",1); material.SetFloat("_Cutoff",.5f);
                material.EnableKeyword("_ALPHATEST_ON"); material.SetOverrideTag("RenderType","TransparentCutout"); material.renderQueue=2450;
            }
            material.SetColor("_BaseColor",color); material.enableInstancing=true; EditorUtility.SetDirty(material); materials.Add(name,material); return material;
        }
        private static Texture2D Tex(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"/Textures/"+name+".png");
        internal static GameObject Place(string name,Transform parent,Vector3 position,Vector3 target,float yaw=0,bool solid=false)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/"+name+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            Vector3 size=sizes[name]; go.transform.localScale=new Vector3(size.x>.001f?target.x/size.x:1,size.y>.001f?target.y/size.y:1,size.z>.001f?target.z/size.z:1);
            if(solid && go.GetComponent<Collider>()==null)
            {
                var shape=go.AddComponent<BoxCollider>(); shape.center=new Vector3(0,size.y/2,0); shape.size=size;
            }
            return go;
        }
        internal static void Room(Transform room,int width,int depth)
        {
            for(int side=-1;side<=1;side+=2) for(int bay=0;bay<depth/4;bay++)
                Place("WallAstra_Straight",room,new Vector3(side*(width/2f+.2f),.02f,-depth/2f+2+bay*4),new Vector3(.6f,3.24f,4),side<0?0:180,true);
            for(int x=0;x<width/2;x++) for(int z=0;z<depth/2;z++)
                Place("Platform_DarkPlates",room,new Vector3(-width/2f+1+x*2,.005f,-depth/2f+1+z*2),new Vector3(1.975f,1,1.975f));
        }
    }
}
