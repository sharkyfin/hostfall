using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hostfall.Editor
{
    public static class ActionRouteUpgrade
    {
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene(FacilityBuilder.ScenePath,OpenSceneMode.Single);
            AddPlatforms(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("HOSTFALL: action route saved without rebuilding authored scene");
        }
        public static void AddPlatforms(Scene scene)
        {
            foreach(var existing in scene.GetRootGameObjects()) if(existing.name=="Action Route / Service Steps") return;
            var root=new GameObject("Action Route / Service Steps"); SceneManager.MoveGameObjectToScene(root,scene);
            var paint=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Service sage paint.mat");
            var trim=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Indicator Amber.mat");
            var positions=new[] { new Vector3(-1.85f,.2f,8.35f),new Vector3(-1.85f,.4f,9.7f),new Vector3(-1.85f,.6f,11.05f) };
            for(int i=0;i<positions.Length;i++)
            {
                var height=.4f*(i+1);
                var step=GameObject.CreatePrimitive(PrimitiveType.Cube); step.name="Service step "+(i+1);
                step.transform.SetParent(root.transform); step.transform.position=positions[i]; step.transform.localScale=new Vector3(.95f,height,1);
                step.GetComponent<MeshFilter>().sharedMesh=FacilityArt.BeveledBox(); step.GetComponent<Renderer>().sharedMaterial=paint;
                var edge=GameObject.CreatePrimitive(PrimitiveType.Cube); edge.name="Visible edge";
                edge.transform.SetParent(root.transform); edge.transform.position=positions[i]+new Vector3(0,height/2+.006f,-.45f); edge.transform.localScale=new Vector3(.85f,.012f,.06f);
                Object.DestroyImmediate(edge.GetComponent<Collider>()); edge.GetComponent<Renderer>().sharedMaterial=trim;
            }
        }
    }
}
