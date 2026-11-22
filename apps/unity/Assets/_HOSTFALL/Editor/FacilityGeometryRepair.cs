using System.Linq;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hostfall.Editor
{
    // Targeted, repeatable repairs. Never rebuild the authored scene to apply these.
    public static class FacilityGeometryRepair
    {
        public static void ApplyAndSave()
        {
            EditorSceneManager.OpenScene(FacilityBuilder.ScenePath);
            ApplyToLoadedScene();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        public static void ApplyToLoadedScene()
        {
            // Wall targets were authored in world axes, but scaled before a quarter-turn.
            foreach(var wall in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="WallAstra_Straight"||t.name=="WallAstra_Straight_Divided"))
            {
                if(Mathf.Abs(Vector3.Dot(wall.right,Vector3.forward))<.99f)continue;
                var b=wall.GetComponent<Collider>().bounds;
                wall.localScale=Vector3.Scale(wall.localScale,new Vector3(b.size.x/b.size.z,1,b.size.z/b.size.x));
                wall.name+=" / aligned";
            }
            var soc=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="SOC / Operations Center");
            foreach(var wall in soc.Cast<Transform>().Where(t=>t.name=="Wall liner"||t.name=="Wall skirting").ToArray())
                CutWall(wall,false,-1.75f,2.5f,3.3f);
            foreach(var door in Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None))
            {
                var jambs=door.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("Jamb")).ToArray();
                if(jambs.Length<2) continue;
                var delta=jambs[0].localPosition-jambs[1].localPosition;
                var leaf=(Transform)new SerializedObject(door).FindProperty("leaf").objectReferenceValue;
                if(leaf!=null && Mathf.Abs(delta.z)>Mathf.Abs(delta.x)) leaf.localRotation=Quaternion.Euler(0,90,0);
            }
            Cut("S08 Gallery South Wall",true,62,3,3.5f);
            Cut("S08 Pods South Wall",true,79,3,3.5f);
            Cut("S08 Office-Chassis Divider",false,56,3,3.5f);
            // Two halves met at the exact middle of the supposedly open vault gate.
            Cut("S08 Vault North Wall L",true,71,3.2f,3.5f);
            Cut("S08 Vault North Wall R",true,71,3.2f,3.5f);
            Cut("S09 Dispatch East Wall",false,112,3,3.5f);
            Cut("S09 Dispatch West Wall",false,108,3.2f,3.2f);
            Cut("Sector05 North Monolithic Wall",true,13,3.2f,3.2f);
            Cut("S07 Corridor West Wall",false,-1.75f,2.6f,3.1f);
            Cut("S07 Corridor East Wall",false,18.5f,3,3.1f);
            Cut("S07 Atrium South Wall",true,27,3,3.1f);
            Cut("S09 Ring East Wall",false,98,3,3.5f);
            Cut("Sector06 North Monolithic Wall",true,30,3.6f,3.6f);
            var cloudGate=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).First(d=>d.DoorId=="door.sec09.cloud");
            cloudGate.transform.position=new Vector3(30,0,95);
            foreach(var rail in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.parent!=null && t.parent.name=="Sector 04 - Quantum Nexus" && t.name.StartsWith("Platform_Rails") && Mathf.Abs(Mathf.Abs(t.position.x)-2.05f)<.02f))
            {
                var b=rail.GetComponent<Collider>().bounds;
                float length=rail.position.z<40||rail.position.z>60?2:6.8f;
                rail.localScale=Vector3.Scale(rail.localScale,new Vector3(length/b.size.z,1,.08f/b.size.x));
            }
            Cut("S10 Arena South Wall",true,30,4.4f,4.4f);
            var gate=Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None).First(d=>d.DoorId=="door.sec10.super");
            gate.transform.position=new Vector3(30,0,126);
            var parent=gate.transform.parent;
            BoxOnce("Logistics Return Floor N",new Vector3(13,-.1f,102.5f),new Vector3(3.6f,.2f,16),parent);
            BoxOnce("Logistics Return Ceiling N",new Vector3(13,3.4f,102.5f),new Vector3(3.6f,.2f,16),parent);
            BoxOnce("Logistics Return West Wall",new Vector3(11,1.6f,102.5f),new Vector3(.4f,3.2f,16),parent);
            BoxOnce("Logistics Return East Wall",new Vector3(15,1.6f,100.6f),new Vector3(.4f,3.2f,11.2f),parent);
            BoxOnce("Logistics Return Floor E",new Vector3(15.6f,-.1f,108),new Vector3(5.6f,.2f,3.6f),parent);
            BoxOnce("Logistics Return Ceiling E",new Vector3(15.6f,3.4f,108),new Vector3(5.6f,.2f,3.6f),parent);
            BoxOnce("Logistics Return North Wall",new Vector3(14.5f,1.6f,110),new Vector3(7.4f,3.2f,.4f),parent);
            BoxOnce("Logistics Return South Wall",new Vector3(16.5f,1.6f,106),new Vector3(3.4f,3.2f,.4f),parent);
            BoxOnce("SOC Shortcut Threshold Floor",new Vector3(8.5f,-.1f,-2.5f),new Vector3(7,.2f,1.4f),parent);
            BoxOnce("SOC Shortcut South End",new Vector3(8.5f,1.7f,-3.2f),new Vector3(7.8f,3.4f,.4f),parent);
            BoxOnce("SOC-Citadel Link Floor",new Vector3(20.5f,-.1f,18.5f),new Vector3(17,.2f,4),parent);
            BoxOnce("SOC-Citadel Link Ceiling",new Vector3(20.5f,3.4f,18.5f),new Vector3(17,.2f,4),parent);
            BoxOnce("SOC-Citadel Link South Wall",new Vector3(20.5f,1.6f,16.3f),new Vector3(17,3.2f,.4f),parent);
            BoxOnce("SOC-Citadel Link North Wall",new Vector3(18.65f,1.6f,20.7f),new Vector3(13.3f,3.2f,.4f),parent);
            BoxOnce("SOC-Citadel Link East End",new Vector3(29.2f,1.6f,18.5f),new Vector3(.4f,3.2f,4.8f),parent);
            BoxOnce("S06-S08 Airlock Floor",new Vector3(53.25f,-.1f,75),new Vector3(3.5f,.2f,6),parent);
            BoxOnce("S06-S08 Airlock South Wall",new Vector3(53.25f,1.75f,71.8f),new Vector3(3.5f,3.5f,.4f),parent);
            BoxOnce("S06-S08 Airlock North Wall",new Vector3(53.25f,1.75f,78.2f),new Vector3(3.5f,3.5f,.4f),parent);
            BoxOnce("S06-S08 Airlock Ceiling",new Vector3(53.25f,3.6f,75),new Vector3(3.5f,.2f,6.8f),parent);
            BoxOnce("S09-S10 Transition Floor",new Vector3(30,-.1f,126.5f),new Vector3(5,.2f,3.5f),parent);
            BoxOnce("S09-S10 Transition West Wall",new Vector3(27.3f,2.2f,126.5f),new Vector3(.4f,4.4f,3.5f),parent);
            BoxOnce("S09-S10 Transition East Wall",new Vector3(32.7f,2.2f,126.5f),new Vector3(.4f,4.4f,3.5f),parent);
            BoxOnce("S09-S10 Transition Ceiling",new Vector3(30,4.5f,126.5f),new Vector3(5.8f,.2f,3.5f),parent);
            ComputeVaultRepair.Apply();
            WorldCirculationRepair.Apply();
            EnvironmentPass.Apply();
        }
        private static void Cut(string name,bool alongX,float center,float width,float height)
        {
            var wall=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
            if(wall==null) return;
            CutWall(wall,alongX,center,width,height);
        }
        private static void CutWall(Transform wall,bool alongX,float center,float width,float height)
        {
            string name=wall.name;
            var b=wall.GetComponent<Collider>().bounds;
            float lo=alongX?b.min.x:b.min.z, hi=alongX?b.max.x:b.max.z;
            float a=Mathf.Clamp(center-width/2,lo,hi),z=Mathf.Clamp(center+width/2,lo,hi);
            if(a>=z) return;
            Part(" A",lo,a,b.min.y,b.max.y); Part(" B",z,hi,b.min.y,b.max.y); Part(" Header",a,z,height,b.max.y);
            void Part(string suffix,float from,float to,float bottom,float top)
            {
                if(to-from<.001f||top-bottom<.001f)return;
                var g=Object.Instantiate(wall.gameObject,wall.parent);g.name=name+suffix;
                g.transform.rotation=Quaternion.identity;
                g.transform.position=new Vector3(alongX?(from+to)/2:b.center.x,(bottom+top)/2,alongX?b.center.z:(from+to)/2);
                g.transform.localScale=new Vector3(alongX?to-from:b.size.x,top-bottom,alongX?b.size.z:to-from);
            }
            Object.DestroyImmediate(wall.gameObject);
        }
        private static void BoxOnce(string name,Vector3 pos,Vector3 size,Transform parent)
        {
            if(parent.Find(name)!=null)return;
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(FacilityBuilder.Content+"/Materials/Dark Metal.mat");
        }
    }
}
