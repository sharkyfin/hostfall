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
            foreach(var existing in scene.GetRootGameObjects())
            {
                if(existing.name=="Action Route / Service Steps" || existing.name=="Action Route / Maintenance Deck")
                    UnityEngine.Object.DestroyImmediate(existing);
            }

            var root=new GameObject("Action Route / Service Steps");
            SceneManager.MoveGameObjectToScene(root,scene);

            var paint=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Service sage paint.mat");
            var trim=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Indicator Amber.mat");
            var dark=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Dark Metal.mat");
            var metal=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Industrial Metal.mat");

            // 3 Canonical Service Steps (Preserved with exactly 3 BoxColliders for ActionRuntimeTests)
            var positions=new[] { new Vector3(-1.85f,.2f,8.35f), new Vector3(-1.85f,.4f,9.7f), new Vector3(-1.85f,.6f,11.05f) };
            for(int i=0;i<positions.Length;i++)
            {
                var height=.4f*(i+1);
                var step=GameObject.CreatePrimitive(PrimitiveType.Cube); step.name="Service step "+(i+1);
                step.transform.SetParent(root.transform); step.transform.position=positions[i]; step.transform.localScale=new Vector3(.95f,height,1);
                step.GetComponent<MeshFilter>().sharedMesh=FacilityArt.BeveledBox(); step.GetComponent<Renderer>().sharedMaterial=paint;

                var edge=GameObject.CreatePrimitive(PrimitiveType.Cube); edge.name="Visible edge";
                edge.transform.SetParent(root.transform); edge.transform.position=positions[i]+new Vector3(0,height/2+.006f,-.45f); edge.transform.localScale=new Vector3(.85f,.012f,.06f);
                Object.DestroyImmediate(edge.GetComponent<Collider>()); edge.GetComponent<Renderer>().sharedMaterial=trim;

                // Side safety stringers/railings (visual only, no colliders)
                var rail=GameObject.CreatePrimitive(PrimitiveType.Cube); rail.name="Step stringer "+(i+1);
                rail.transform.SetParent(root.transform); rail.transform.position=positions[i]+new Vector3(.48f,height/2+.35f,0); rail.transform.localScale=new Vector3(.04f,.65f,.96f);
                Object.DestroyImmediate(rail.GetComponent<Collider>()); rail.GetComponent<Renderer>().sharedMaterial=dark;
            }

            // Dedicated Elevated Maintenance Deck connecting Step 3 along the west server racks
            var deckRoot=new GameObject("Action Route / Maintenance Deck");
            SceneManager.MoveGameObjectToScene(deckRoot,scene);

            // Elevated walkway slab at Y=1.2m connecting from Step 3 (Z=11.55) to Z=14.4
            float deckLen=2.85f;
            float deckZ=11.55f+deckLen/2f; // Z=12.975
            var deck=GameObject.CreatePrimitive(PrimitiveType.Cube); deck.name="Server Maintenance Walkway Deck";
            deck.transform.SetParent(deckRoot.transform); deck.transform.position=new Vector3(-1.85f, 1.14f, deckZ); deck.transform.localScale=new Vector3(.95f, .12f, deckLen);
            deck.GetComponent<Renderer>().sharedMaterial=metal;

            // Amber safety edge along the aisle side
            var deckEdge=GameObject.CreatePrimitive(PrimitiveType.Cube); deckEdge.name="Walkway Amber Edge";
            deckEdge.transform.SetParent(deckRoot.transform); deckEdge.transform.position=new Vector3(-1.38f, 1.206f, deckZ); deckEdge.transform.localScale=new Vector3(.04f, .014f, deckLen);
            Object.DestroyImmediate(deckEdge.GetComponent<Collider>()); deckEdge.GetComponent<Renderer>().sharedMaterial=trim;

            // Sturdy structural vertical steel columns connecting deck underside to floor (Y=0)
            float[] supportZ=new[] { 11.6f, 12.95f, 14.3f };
            for(int s=0;s<supportZ.Length;s++)
            {
                var pillar=GameObject.CreatePrimitive(PrimitiveType.Cube); pillar.name="Walkway Support Pillar "+s;
                pillar.transform.SetParent(deckRoot.transform); pillar.transform.position=new Vector3(-1.85f, .54f, supportZ[s]); pillar.transform.localScale=new Vector3(.14f, 1.08f, .14f);
                pillar.GetComponent<Renderer>().sharedMaterial=dark;
            }

            // Safety handrail along the walkway outer edge facing the aisle (Y=1.2 to Y=2.0)
            var handrail=GameObject.CreatePrimitive(PrimitiveType.Cube); handrail.name="Walkway Safety Handrail";
            handrail.transform.SetParent(deckRoot.transform); handrail.transform.position=new Vector3(-1.38f, 1.85f, deckZ); handrail.transform.localScale=new Vector3(.05f, .04f, deckLen);
            handrail.GetComponent<Renderer>().sharedMaterial=paint;

            for(int p=0;p<=4;p++)
            {
                float postZ=11.6f + p*(deckLen-.1f)/4f;
                var post=GameObject.CreatePrimitive(PrimitiveType.Cube); post.name="Handrail Post "+p;
                post.transform.SetParent(deckRoot.transform); post.transform.position=new Vector3(-1.38f, 1.52f, postZ); post.transform.localScale=new Vector3(.04f, .65f, .04f);
                Object.DestroyImmediate(post.GetComponent<Collider>()); post.GetComponent<Renderer>().sharedMaterial=dark;
            }

            // Descent steps at far end (Z=14.4) back down to floor level near the Vault Gate
            var descentStep=GameObject.CreatePrimitive(PrimitiveType.Cube); descentStep.name="Walkway Descent Step";
            descentStep.transform.SetParent(deckRoot.transform); descentStep.transform.position=new Vector3(-1.85f, .6f, 14.7f); descentStep.transform.localScale=new Vector3(.95f, .6f, .55f);
            descentStep.GetComponent<Renderer>().sharedMaterial=paint;
        }
    }
}
