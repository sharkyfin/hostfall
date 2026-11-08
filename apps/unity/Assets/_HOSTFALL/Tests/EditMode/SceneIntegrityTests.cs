using Hostfall.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Hostfall.Tests
{
    public sealed class SceneIntegrityTests
    {
        [Test]
        public void SavedSceneHasNoMissingScriptsAndHasBoundStartupReferences()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_HOSTFALL/Content/Scenes/NorthlineFacility.unity", OpenSceneMode.Additive);
            try
            {
                FacilityRoot composition = null;
                UIDocument document = null;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
                        if (transform.TryGetComponent<FacilityRoot>(out var candidate)) composition = candidate;
                        if (transform.TryGetComponent<UIDocument>(out var view)) document = view;
                    }
                Assert.That(composition, Is.Not.Null);
                var serialized = new SerializedObject(composition);
                foreach (var name in new[] { "mission", "controls", "input", "player", "rig", "fps", "tps", "brain", "playerVisual", "scanner", "ui", "door", "power" })
                    Assert.That(serialized.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
                var computers = serialized.FindProperty("computers");
                Assert.That(computers.arraySize, Is.EqualTo(3));
                for (int i = 0; i < computers.arraySize; i++) Assert.That(computers.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                Assert.That(document, Is.Not.Null);
                Assert.That(document.visualTreeAsset, Is.Not.Null);
                Assert.That(document.panelSettings, Is.Not.Null);
                Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void VolumeProfilesContainOnlyLoadableComponents()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:VolumeProfile", new[] { "Assets/_HOSTFALL/Content" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                Assert.That(profile, Is.Not.Null, path);
                foreach (var component in profile.components) Assert.That(component, Is.Not.Null, path);
                foreach (var subasset in AssetDatabase.LoadAllAssetsAtPath(path)) Assert.That(subasset, Is.Not.Null, path);
            }
        }
    }
}
