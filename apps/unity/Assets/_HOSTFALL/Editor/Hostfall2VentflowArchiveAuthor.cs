using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hostfall.Editor
{
    public static partial class Hostfall2Workbench
    {
        [MenuItem("HOSTFALL/2.0/Export Ventflow Handover Photograph")]
        public static void ExportVentflowHandoverPhotograph()
        {
            WorkstationWorkshop.RequireCopy();
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new System.InvalidOperationException("Save or discard other work in the verification scene first");
            if (EditorSceneManager.GetActiveScene().path != PreviewScene)
                EditorSceneManager.OpenScene(PreviewScene);

            const string captureName = "ventflow-handover-source";
            WorkstationWorkshop.Capture(captureName, new Vector3(2.75f, 2.35f, -1.95f), new Vector3(-1.9f, 1.1f, -3.9f));
            var source = new Texture2D(2, 2);
            var output = new Texture2D(896, 560, TextureFormat.RGB24, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(Path.Combine(WorkstationWorkshop.Output, captureName + ".png"))))
                    throw new System.InvalidOperationException("Camera capture could not be read");
                var pixels = new Color32[output.width * output.height];
                var noise = new System.Random(308);
                for (int y = 0; y < output.height; y++)
                {
                    float v = (float)y / (output.height - 1);
                    bool drifted = y >= 34 && y < 57 || y >= 102 && y < 111;
                    int drift = drifted ? 46 : 0;
                    for (int x = 0; x < output.width; x++)
                    {
                        float u = (float)((x + drift) % output.width) / (output.width - 1);
                        Color sample = source.GetPixelBilinear(u, v);
                        float grain = (float)noise.NextDouble() * .03f;
                        float luma = Mathf.Pow(sample.grayscale, .72f) + grain;
                        if (y < 31 && (x / 92 + y / 8) % 4 == 0)
                            luma = .035f + grain;
                        pixels[y * output.width + x] = new Color(luma * .86f, luma * .96f, luma, 1);
                    }
                }
                output.SetPixels32(pixels);
                output.Apply();
                const string assetPath = "Assets/_HOSTFALL/Resources/Investigation/VentflowHandover.png";
                File.WriteAllBytes(assetPath, output.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
            finally
            {
                Object.DestroyImmediate(output);
                Object.DestroyImmediate(source);
                EditorSceneManager.OpenScene(PreviewScene);
            }
        }
    }
}
