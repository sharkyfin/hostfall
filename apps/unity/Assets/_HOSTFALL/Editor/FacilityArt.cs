using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Hostfall.Editor
{
    internal static class FacilityArt
    {
        internal static Mesh BeveledBox()
        {
            const string path = FacilityBuilder.Content + "/Settings/BeveledBox.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            float[] steps = { -.5f, -.44f, .44f, .5f };
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            for (int axis = 0; axis < 3; axis++) foreach (int sign in new[] { -1, 1 })
            {
                Vector3 n = axes[axis] * sign, u = axes[(axis + 1) % 3], v = Vector3.Cross(n, u);
                int offset = vertices.Count;
                for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
                {
                    Vector3 raw = n * .5f + u * steps[x] + v * steps[y];
                    Vector3 core = new(Mathf.Clamp(raw.x, -.44f, .44f), Mathf.Clamp(raw.y, -.44f, .44f), Mathf.Clamp(raw.z, -.44f, .44f));
                    Vector3 normal = (raw - core).normalized;
                    vertices.Add(core + normal * .06f); normals.Add(normal); uv.Add(new Vector2(steps[x] + .5f, steps[y] + .5f));
                }
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                {
                    int a = offset + y * 4 + x, b = a + 1, c = a + 4, d = c + 1;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            mesh = new Mesh { name = "Chamfered industrial housing" }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        internal static void Surface(Material material, string name, float scale)
        {
            var baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(FacilityBuilder.Content + "/Textures/" + name + ".png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(FacilityBuilder.Content + "/Textures/" + name + "-normal.png");
            material.SetTexture("_BaseMap", baseMap); material.SetTextureScale("_BaseMap", new Vector2(scale, scale));
            material.SetTexture("_BumpMap", normal); material.SetFloat("_BumpScale", .35f); material.EnableKeyword("_NORMALMAP"); EditorUtility.SetDirty(material);
        }
    }
}
