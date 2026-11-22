using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hostfall.Editor
{
    internal static class StairWalkSurface
    {
        // Source Quaternius stairs rise towards local -Z. Encode the slope in vertices:
        // rotating a box beneath a nonuniformly scaled root shears its collision plane.
        internal static void Configure(GameObject stairs,Vector3 size)
        {
            foreach(var c in stairs.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            var old=stairs.transform.Find("RampCollider");if(old!=null)Object.DestroyImmediate(old.gameObject);
            string path=FacilityBuilder.Content+"/Meshes/StairSurface-"+stairs.name.Split('/')[0].Trim()+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
                mesh=new Mesh {name="Continuous stair surface / "+stairs.name};
                float x=size.x/2,z=size.z/2,h=size.y,t=.10f;
                mesh.vertices=new[]{new Vector3(-x,h,-z),new Vector3(x,h,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(-x,h-t,-z),new Vector3(x,h-t,-z),new Vector3(-x,-t,z),new Vector3(x,-t,z)};
                mesh.triangles=new[]{0,2,1,1,2,3,4,5,6,5,7,6,0,4,2,4,6,2,1,3,5,5,3,7,0,1,4,4,1,5,2,6,3,6,7,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,path);
            }
            stairs.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
    }
}
