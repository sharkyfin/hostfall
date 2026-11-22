using System.Linq;
using Hostfall.Rendering;
using Hostfall.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hostfall.Tests
{
    public sealed class EnvironmentTests
    {
        [Test] public void PhysicalTextUsesDepthAndEveryChairIsSolid()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_HOSTFALL/Content/Scenes/NorthlineFacility.unity",OpenSceneMode.Additive);
            try
            {
                var objects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
                var texts=objects.Where(t=>t.GetComponent<TextMesh>()!=null).ToArray(); Assert.That(texts.Length,Is.GreaterThan(10));
                foreach(var label in texts)
                {
                    var depth=label.GetComponent<WorldTextDepth>(); Assert.That(depth,Is.Not.Null,label.name);
                    var property=new SerializedObject(depth).FindProperty("depthShader"); Assert.That(property.objectReferenceValue,Is.Not.Null);
                    var material=new Material((Shader)property.objectReferenceValue);
                    Assert.That(material.GetFloat("_ZTest"),Is.EqualTo((float)CompareFunction.LessEqual)); Object.DestroyImmediate(material);
                }
                var chairs=objects.Where(t=>t.name=="Task chair").ToArray(); Assert.That(chairs.Length,Is.EqualTo(2));
                Assert.That(chairs.All(t=>t.parent.name=="Terminal / ws07"||t.parent.name=="Unassigned console"),Is.True,
                    "EDGE/CORE must remain standing posts with a clear patrol aisle");
                foreach(var chair in chairs)
                {
                    Assert.That(chair.GetComponent<Collider>(),Is.Not.Null); Assert.That(chair.GetComponent<Collider>().isTrigger,Is.False);
                    foreach(var renderer in chair.GetComponentsInChildren<Renderer>())
                        Assert.That(renderer.sharedMaterial.GetFloat("_Surface"),Is.Zero);
                }
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
        [Test] public void ModularPaletteContainsRealReusableMeshes()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<FacilityModuleCatalog>("Assets/_HOSTFALL/Content/Settings/FacilityModules.asset");
            Assert.That(catalog,Is.Not.Null); Assert.That(catalog.modules.Length,Is.GreaterThanOrEqualTo(12));
            foreach(var item in catalog.modules) { Assert.That(item.prefab,Is.Not.Null); Assert.That(item.prefab.GetComponentsInChildren<MeshFilter>().Length,Is.GreaterThan(0)); }
        }
        [Test] public void EngineeringPropsHaveCompleteMaterialsAndDoNotObstructTheMainRoute()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_HOSTFALL/Content/Scenes/NorthlineFacility.unity",OpenSceneMode.Additive);
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
                foreach(string name in new[] { "Shift notebook", "Maintenance cart", "Cooling valve" })
                {
                    var props=all.Where(t=>t.name==name).ToArray();Assert.That(props,Is.Not.Empty,name);
                    foreach(var prop in props)
                    {
                    var visible=prop.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray(); Assert.That(visible.Length,Is.GreaterThan(0));
                    foreach(var renderer in visible) foreach(var material in renderer.sharedMaterials)
                    {
                        Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
                        foreach(string slot in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
                            Assert.That(material.GetTexture(slot),Is.Not.Null,name+" "+slot);
                        Assert.That(material.GetFloat("_Surface"),Is.Zero);
                    }
                }
                }
                foreach(var prop in all.Where(t=>t.name=="Maintenance cart"))
                {
                var cart=prop.GetComponent<BoxCollider>();
                Assert.That(cart,Is.Not.Null); Assert.That(cart.isTrigger,Is.False);
                Assert.That(cart.bounds.size.y,Is.EqualTo(1.05f).Within(.02f));
                Assert.That(cart.bounds.Intersects(new Bounds(new Vector3(0,1,1),new Vector3(2.5f,2,12))),Is.False);
                }
                var original=all.Single(t=>t.name=="Maintenance cart"&&t.root.name!="NORTHLINE / Service Bays").GetComponent<BoxCollider>();
                Assert.That(original.bounds.max.x,Is.LessThan(4.9f));
            }
            finally { EditorSceneManager.CloseScene(scene,true); }
        }
    }
}
