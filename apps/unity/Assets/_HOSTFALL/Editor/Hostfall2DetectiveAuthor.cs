using System;
using System.IO;
using System.Linq;
using Hostfall.Simulation.Computers;
using Hostfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Author Neuroarchive Investigation")]
        public static void AuthorNeuroarchiveInvestigation()
        {
            WorkstationWorkshop.RequireCopy();
            EditorSceneManager.OpenScene(PreviewScene);
            if (Object.FindAnyObjectByType<NeuroInvestigationController>() != null)
                return;

            File.Copy(PreviewScene, Path.Combine(Output, "scene-before-detective.unity"), true);
            var sector = FindTransform("Sector 08 - Neuroarchive");
            var record = CreateArchiveRecording();
            var monitorMaterial = ArchivePlaybackMaterial(record);
            var monitorFaces = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => r.name.StartsWith("CCTV Screen ") && r.transform.position.x > 42 && r.transform.position.x < 54)
                .OrderBy(r => r.transform.position.x).ToArray();
            if (monitorFaces.Length != 5)
                throw new InvalidOperationException("Expected five screens in the physical observation room");

            var metal = MaterialAt("Dark Metal");
            var amber = MaterialAt("Indicator Amber");
            var safe = new GameObject("Case safe / physical memory-vault key").transform;
            safe.SetParent(sector, false);
            safe.position = new Vector3(66.3f, 0, 49.1f);
            // Shelf depth leaves the front open, with the handle and key sharing the visible interaction surface.
            Box("Raised safe stand", safe, new Vector3(0, .38f, 0), new Vector3(1.02f, .76f, .72f), metal, true);
            Box("Safe back", safe, new Vector3(0, 1.12f, .31f), new Vector3(1.02f, .72f, .09f), metal, true);
            Box("Safe roof", safe, new Vector3(0, 1.51f, 0), new Vector3(1.02f, .08f, .72f), metal, true);
            foreach (int side in new[] { -1, 1 })
                Box("Safe solid side", safe, new Vector3(side * .468f, 1.12f, 0), new Vector3(.08f, .72f, .72f), metal, true);

            var hinge = new GameObject("Safe door hinge").transform;
            hinge.SetParent(safe, false);
            hinge.localPosition = new Vector3(-.48f, 1.12f, -.37f);
            Box("Locked safe hatch", hinge, new Vector3(.48f, 0, 0), new Vector3(.96f, .716f, .07f), metal, true);
            Box("Safe handle", hinge, new Vector3(.8f, -.14f, -.055f), new Vector3(.14f, .15f, .06f), amber, true);
            var key = new GameObject("Archive signed key / unique pickup");
            key.transform.SetParent(safe, false);
            key.transform.localPosition = new Vector3(0, .87f, -.10f);
            Box("Case key chassis", key.transform, Vector3.zero, new Vector3(.32f, .11f, .22f), metal, true);
            Box("Key copper contact", key.transform, new Vector3(0, .055f, -.054f), new Vector3(.26f, .018f, .055f), amber, false);
            AddText("КЛЮЧ АРХИВА", safe, new Vector3(0, .61f, -.368f), Quaternion.identity, .035f);
            key.SetActive(false);

            var divider = FindTransform("S08 Office-Chassis Divider A");
            CutOpening(divider, Vector3.forward, 46.7f, 2.3f, 3.1f);
            var passage = new GameObject("Neuroarchive / vault circulation frame").transform;
            passage.SetParent(sector, false);
            passage.SetPositionAndRotation(new Vector3(70, 0, 46.7f), Quaternion.Euler(0, 90, 0));
            Box("Vault portal west trim", passage, new Vector3(-1.1f, 1.55f, 0), new Vector3(.08f, 3.1f, .5f), metal, true);
            Box("Vault portal east trim", passage, new Vector3(1.1f, 1.55f, 0), new Vector3(.08f, 3.1f, .5f), metal, true);
            Box("Vault portal lintel trim", passage, new Vector3(0, 3.04f, 0), new Vector3(2.28f, .12f, .5f), metal, true);
            AddText("ХРАНИЛИЩЕ ПАМЯТИ", passage, new Vector3(0, 3.27f, -.28f), Quaternion.identity, .057f);

            var captionRoot = new GameObject("CCTV archive subtitle").transform;
            captionRoot.SetParent(monitorFaces[2].transform.parent, false);
            captionRoot.position = new Vector3(48, 3.13f, 49.58f);
            AddText("", captionRoot, Vector3.zero, Quaternion.identity, .069f);
            var caption = captionRoot.GetComponentInChildren<TextMesh>();
            safe.gameObject.AddComponent<NeuroInvestigationController>().Configure(hinge, key, monitorFaces, monitorMaterial, caption);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            WorkstationWorkshop.Capture("hf2-case-safe", new Vector3(65.4f, 1.7f, 46.6f), new Vector3(66.3f, 1.15f, 49.1f));
            File.Copy(Path.Combine(WorkstationWorkshop.Output, "hf2-case-safe.png"), Path.Combine(Output, "hf2-case-safe.png"), true);
        }

        private static Material ArchivePlaybackMaterial(Texture2D recording)
        {
            const string path = "Assets/_HOSTFALL/Content/Materials/Neuro Archive Playback.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("HOSTFALL/Archive Monitor");
                if (shader == null)
                    throw new InvalidOperationException("Import the archive monitor shader before authoring");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_ArchiveTex", recording);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Texture2D CreateArchiveRecording()
        {
            // Offline footage from the authored environment. Temporary lights mark
            // seal, quarantine and remote signature in the fixed security camera shot.
            const int width = 320, height = 192, columns = 4, rows = 3;
            const string path = "Assets/_HOSTFALL/Resources/Investigation/ArchiveS08.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var cameraObject = new GameObject("Authoring CCTV camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(49.35f, 2.9f, 40.05f);
            camera.transform.LookAt(new Vector3(54, 1.5f, 43.03f));
            camera.fieldOfView = 59;
            camera.nearClipPlane = .07f;
            var beaconObject = new GameObject("Authoring quarantine beacon");
            var beacon = beaconObject.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.transform.position = new Vector3(52.95f, 2.6f, 42.92f);
            beacon.range = 6;
            var dark = MaterialAt("Dark Metal");
            var indicator = MaterialAt("Indicator Amber");
            var relay = Box("Archive frame relay", beaconObject.transform, new Vector3(.3f, -.8f, 1.88f),
                new Vector3(.25f, .5f, .49f), dark, false);
            var glow = Box("Recording relay indicator", relay.transform, new Vector3(-.51f, .12f, -.12f),
                new Vector3(.09f, .23f, .35f), indicator, false);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var atlas = new Texture2D(columns * width, rows * height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                for (int frame = 0; frame < DetectiveSession.RecordingFrames; frame++)
                {
                    beacon.color = frame < 4 ? new Color(.28f, .5f, .74f) : new Color(.93f, .21f, .1f);
                    beacon.intensity = frame < 4 ? 1.55f : (frame % 2 == 0 ? 3.4f : 1.5f);
                    glow.SetActive(frame >= 8 && frame % 2 == 0);
                    camera.Render();
                    RenderTexture.active = target;
                    atlas.ReadPixels(new Rect(0, 0, width, height), frame % columns * width,
                        (rows - 1 - frame / columns) * height, false);
                }
                atlas.Apply(false);
                File.WriteAllBytes(path, atlas.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(atlas);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(beaconObject);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
