// HOSTFALL scene audit (non-destructive)
// Install: Assets/Editor/HostfallSceneAudit.cs
// Unity 6000.6.x, Unity Editor only. It does not modify scene objects.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class HostfallSceneAudit
{
    private const float ApproximatePositionStep = 0.005f;
    private const float ApproximateScaleStep = 0.005f;
    private const float ApproximateRotationStep = 0.001f;

    [MenuItem("Tools/HOSTFALL/Analyze Active Scene")]
    public static void AnalyzeActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
        {
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_HOSTFALL/Content/Scenes/NorthlineFacility.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        List<GameObject> objects = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in descendants)
                objects.Add(transform.gameObject);
        }

        var report = new StringBuilder(128 * 1024);
        int warnings = 0;
        int renderers = 0;
        int colliders = 0;
        int agents = 0;
        report.AppendLine("HOSTFALL — NON-DESTRUCTIVE SCENE AUDIT");
        report.AppendLine("Scene: " + scene.path);
        report.AppendLine("Date UTC: " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        report.AppendLine("Objects (including inactive): " + objects.Count);
        report.AppendLine("NOTE: Warnings are heuristics. An overlap can be an intentional seam/decal.");
        report.AppendLine("NOTE: Run PlayMode tests and visual inspection before deleting/changing objects.");
        report.AppendLine();

        var duplicateMeshes = new Dictionary<string, List<GameObject>>(StringComparer.Ordinal);
        var foundIds = new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase);
        foreach (GameObject gameObject in objects)
        {
            Renderer[] localRenderers = gameObject.GetComponents<Renderer>();
            renderers += localRenderers.Length;
            colliders += gameObject.GetComponents<Collider>().Length;

            MeshFilter filter = gameObject.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && gameObject.GetComponent<MeshRenderer>() != null)
            {
                string key = (filter.sharedMesh.name + "_" + filter.sharedMesh.vertexCount)
                    + ":" + QuantizedVector(gameObject.transform.position, ApproximatePositionStep)
                    + ":" + QuantizedQuaternion(gameObject.transform.rotation, ApproximateRotationStep)
                    + ":" + QuantizedVector(gameObject.transform.lossyScale, ApproximateScaleStep);
                if (!duplicateMeshes.TryGetValue(key, out List<GameObject> matching))
                {
                    matching = new List<GameObject>();
                    duplicateMeshes.Add(key, matching);
                }
                matching.Add(gameObject);
            }

            foreach (Renderer renderer in localRenderers)
            {
                if (!LooksStructural(gameObject.name)) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
                    {
                        Warning(report, ref warnings, "STRUCTURAL_TRANSPARENT",
                            FullPath(gameObject) + " | " + material.name + " | shader " +
                            (material.shader == null ? "<null>" : material.shader.name));
                    }
                }
            }

            if (LooksStructural(gameObject.name) &&
                gameObject.GetComponent<MeshFilter>() != null &&
                gameObject.GetComponent<Collider>() == null &&
                gameObject.GetComponentInParent<Collider>() == null &&
                gameObject.GetComponentInChildren<Collider>(true) == null)
            {
                Warning(report, ref warnings, "STRUCTURAL_NO_COLLIDER",
                    FullPath(gameObject) + " | may rely on a separate proxy collider: review");
            }

            if (LooksDoor(gameObject.name) && gameObject.GetComponent<MeshRenderer>() != null &&
                gameObject.GetComponent<Collider>() == null &&
                gameObject.GetComponentInParent<Collider>() == null &&
                gameObject.GetComponentInChildren<Collider>(true) == null)
            {
                Warning(report, ref warnings, "DOOR_RENDERER_NO_COLLIDER",
                    FullPath(gameObject) + " | check physical blocker on door hierarchy");
            }

            NavMeshAgent agent = gameObject.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agents++;
                InspectAgent(gameObject, agent, report, ref warnings);
            }

            if (gameObject.name.Contains("(Clone)"))
                Warning(report, ref warnings, "CLONE_IN_EDIT_SCENE", FullPath(gameObject));

            // IDs may be embedded in names for high-level diagnostics; real ID validation
            // must use the project's Terminal/Door/Spawner components when available.
            string id = FindKnownTerminalId(gameObject.name);
            if (id != null)
            {
                if (!foundIds.TryGetValue(id, out List<GameObject> idObjects))
                {
                    idObjects = new List<GameObject>();
                    foundIds.Add(id, idObjects);
                }
                idObjects.Add(gameObject);
            }
        }

        foreach (KeyValuePair<string, List<GameObject>> pair in duplicateMeshes)
        {
            if (pair.Value.Count < 2) continue;
            var paths = new StringBuilder();
            foreach (GameObject obj in pair.Value)
                paths.Append("\n    ").Append(FullPath(obj));
            Warning(report, ref warnings, "POTENTIAL_EXACT_MESH_DUPLICATE",
                "count=" + pair.Value.Count + paths);
        }

        foreach (KeyValuePair<string, List<GameObject>> pair in foundIds)
        {
            if (pair.Value.Count < 2) continue;
            // Multiple visible mesh parts may legitimately include the same terminal ID.
            // This is an inspection hint, not a guaranteed duplicate Terminal instance.
            var paths = new StringBuilder();
            foreach (GameObject obj in pair.Value)
                paths.Append("\n    ").Append(FullPath(obj));
            Warning(report, ref warnings, "REPEATED_NAME_TOKEN_" + pair.Key,
                "count=" + pair.Value.Count + paths);
        }

        report.AppendLine();
        report.AppendLine("SUMMARY");
        report.AppendLine("Renderers: " + renderers);
        report.AppendLine("Colliders: " + colliders);
        report.AppendLine("NavMeshAgents: " + agents);
        report.AppendLine("Warnings (not confirmed defects): " + warnings);
        report.AppendLine("Manual checks still required: z-fighting, transparent sorting,");
        report.AppendLine("doors/path connectivity, physics collision matrix, agent root motion,");
        report.AppendLine("runtime spawners, world-state IDs, performance and PlayMode.");

        string projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath));
        string directory = Path.Combine(projectRoot, "docs", "audit");
        Directory.CreateDirectory(directory);
        string filename = "scene_audit_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".txt";
        string path = Path.Combine(directory, filename);
        File.WriteAllText(path, report.ToString(), Encoding.UTF8);
        Debug.Log("HOSTFALL AUDIT: " + warnings + " warnings. Saved: " + path);
        if (!Application.isBatchMode) EditorUtility.RevealInFinder(path);
    }

    private static void InspectAgent(GameObject go, NavMeshAgent agent, StringBuilder report, ref int warnings)
    {
        Renderer[] visuals = go.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Renderer visual in visuals)
        {
            if (!hasBounds)
            {
                bounds = visual.bounds;
                hasBounds = true;
            }
            else bounds.Encapsulate(visual.bounds);
        }
        if (!hasBounds) return;

        float extent = Mathf.Max(bounds.extents.x, bounds.extents.z);
        if (extent > agent.radius * 1.5f + 0.12f)
        {
            Warning(report, ref warnings, "AGENT_SMALL_RELATIVE_TO_VISUALS",
                FullPath(go) + " | radius=" + agent.radius.ToString("0.000", CultureInfo.InvariantCulture) +
                " | world-space extent=" + extent.ToString("0.000", CultureInfo.InvariantCulture) +
                " | this is a heuristic; inspect moving body/collider separately");
        }
        if (go.GetComponentsInChildren<Collider>(true).Length == 0)
            Warning(report, ref warnings, "AGENT_NO_PHYSICS_COLLIDER", FullPath(go));
        if (agent.radius <= 0.01f || agent.height <= 0.01f)
            Warning(report, ref warnings, "AGENT_INVALID_DIMENSIONS", FullPath(go));
    }

    private static string FindKnownTerminalId(string name)
    {
        string[] tokens = {"WS-07", "EDGE-01", "DATA-01", "PWR-01", "QNT-01", "CORE-01", "RBT-01", "ARM-01"};
        foreach (string token in tokens)
            if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                return token;
        return null;
    }

    private static string QuantizedVector(Vector3 v, float step)
    {
        return Quantize(v.x, step) + "," + Quantize(v.y, step) + "," + Quantize(v.z, step);
    }

    private static string QuantizedQuaternion(Quaternion q, float step)
    {
        // q and -q are the same rotation; choose canonical sign.
        if (q.w < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
        return Quantize(q.x, step) + "," + Quantize(q.y, step) + "," +
               Quantize(q.z, step) + "," + Quantize(q.w, step);
    }

    private static int Quantize(float value, float step)
    {
        return Mathf.RoundToInt(value / step);
    }

    private static bool LooksStructural(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("wall") || n.Contains("floor") || n.Contains("ceiling") ||
               n.Contains("bulkhead") || n.Contains("перегород") || n.Contains("стена") ||
               n.Contains("пол_") || n.Contains("потолок");
    }

    private static bool LooksDoor(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("door") || n.Contains("дверь") || n.Contains("шлюз");
    }

    private static string FullPath(GameObject obj)
    {
        var path = new StringBuilder(obj.name);
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path.Insert(0, parent.name + "/");
            parent = parent.parent;
        }
        return path.ToString();
    }

    private static void Warning(StringBuilder report, ref int count, string kind, string details)
    {
        count++;
        report.Append("[WARN ").Append(count).Append("] ").Append(kind).Append(": ")
              .AppendLine(details);
    }
}
#endif
