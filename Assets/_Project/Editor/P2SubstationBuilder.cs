using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.ProBuilder;
using Unity.AI.Navigation;
using HeroShooter.Systems;

/// <summary>Builds the playable ProBuilder greybox from docs/level_design/layout.json.</summary>
public static class P2SubstationBuilder
{
    [Serializable] sealed class LayoutData
    {
        public string name;
        public string scene;
        public Footprint footprint;
        public List<RectData> solids;
        public List<RectData> pads;
        public List<RectData> decks;
        public List<ConnectorData> connectors;
        public List<EnemyData> enemies;
        public SpawnData spawn;
    }
    [Serializable] sealed class Footprint { public float w, d; }
    [Serializable] sealed class RectData
    {
        public string name, kind;
        public float x0, z0, x1, z1, h;
    }
    [Serializable] sealed class ConnectorData
    {
        public string name, kind;
        public float x0, z0, x1, z1, rise, steps, riser, tread;
        public float[] low, high;
    }
    [Serializable] sealed class EnemyData
    {
        public string prefab, on;
        public float x, y, z;
    }
    [Serializable] sealed class SpawnData { public float x, z, yaw; }

    const string SourceScene = "Assets/_Project/Scenes/20_Map_Arena.unity";
    const string TargetScene = "Assets/_Project/Scenes/22_Map_Substation.unity";
    const string NavDataPath = "Assets/_Project/Scenes/22_Map_Substation/NavMesh.asset";
    const string LayoutPath = "docs/level_design/layout.json";
    const int StructureLayer = 3;

    public static string Build()
    {
        if (EditorApplication.isPlaying) return "FAIL: editor is playing";
        Scene active = SceneManager.GetActiveScene();
        if (active.isDirty) return "FAIL: active scene is dirty: " + active.name;
        bool targetExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null;
        if (!targetExists && !AssetDatabase.CopyAsset(SourceScene, TargetScene)) return "FAIL: could not copy source scene";
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        if (!scene.IsValid() || scene.name != "22_Map_Substation") return "FAIL: target scene did not open";
        string jsonPath = Path.Combine(projectRoot, LayoutPath);
        if (!File.Exists(jsonPath)) return "FAIL: layout not found: " + jsonPath;
        LayoutData layout = JsonUtility.FromJson<LayoutData>(File.ReadAllText(jsonPath));
        if (layout == null || layout.solids == null || layout.decks == null || layout.connectors == null)
            return "FAIL: invalid layout data";
        GameObject arena = GameObject.Find("Arena");
        GameObject enemies = GameObject.Find("Enemies");
        GameObject spawn = GameObject.Find("SpawnPoint");
        GameObject navRoot = GameObject.Find("NavMesh");
        if (arena == null || enemies == null || spawn == null || navRoot == null)
            return "FAIL: copied rig missing Arena/Enemies/SpawnPoint/NavMesh";
        if (targetExists && arena.transform.Find("Blockmesh") == null)
            return "FAIL: existing target is not a builder-created Substation scene";

        // Replace only the copied scene's map geometry; keep its match rig and its intra-scene references.
        for (int i = arena.transform.childCount - 1; i >= 0; --i)
            UnityEngine.Object.DestroyImmediate(arena.transform.GetChild(i).gameObject);
        Transform blockmesh = new GameObject("Blockmesh").transform;
        blockmesh.SetParent(arena.transform, false);

        Material block = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Block.mat");
        Material floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Floor.mat");
        if (block == null || floor == null) return "FAIL: M_Block or M_Floor missing";

        MakeCube("Floor", new Vector3(0f, -0.15f, 0f), new Vector3(layout.footprint.w, 0.3f, layout.footprint.d),
                 blockmesh, floor);
        for (int i = 0; i < layout.solids.Count; i++)
        {
            RectData r = layout.solids[i];
            float w = r.x1 - r.x0;
            float d = r.z1 - r.z0;
            MakeCube(r.name, new Vector3((r.x0 + r.x1) * 0.5f, r.h * 0.5f, (r.z0 + r.z1) * 0.5f),
                     new Vector3(w, r.h, d), blockmesh, block);
        }
        for (int i = 0; i < layout.pads.Count; i++)
        {
            RectData r = layout.pads[i];
            float y = r.h > 0.01f ? r.h * 0.5f : -0.025f;
            float h = r.h > 0.01f ? r.h : 0.05f;
            MakeCube(r.name, new Vector3((r.x0 + r.x1) * 0.5f, y, (r.z0 + r.z1) * 0.5f),
                     new Vector3(r.x1 - r.x0, h, r.z1 - r.z0), blockmesh, block);
        }
        for (int i = 0; i < layout.decks.Count; i++)
        {
            RectData r = layout.decks[i];
            const float slab = 0.30f;
            MakeCube(r.name, new Vector3((r.x0 + r.x1) * 0.5f, r.h - slab * 0.5f, (r.z0 + r.z1) * 0.5f),
                     new Vector3(r.x1 - r.x0, slab, r.z1 - r.z0), blockmesh, block);
        }
        for (int i = 0; i < layout.connectors.Count; i++)
        {
            ConnectorData c = layout.connectors[i];
            if (c.kind == "ramp") MakeRamp(c, blockmesh, block);
            else if (c.kind == "stair") MakeStairs(c, blockmesh, block);
        }

        spawn.transform.position = new Vector3(layout.spawn.x, 1.2f, layout.spawn.z);
        spawn.transform.rotation = Quaternion.Euler(0f, layout.spawn.yaw, 0f);
        if (layout.enemies != null)
        {
            for (int i = 0; i < layout.enemies.Count && i < enemies.transform.childCount; i++)
            {
                EnemyData e = layout.enemies[i];
                Transform t = enemies.transform.GetChild(i);
                t.position = new Vector3(e.x, e.y, e.z);
                t.rotation = Quaternion.identity;
            }
        }

        if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/22_Map_Substation"))
            AssetDatabase.CreateFolder("Assets/_Project/Scenes", "22_Map_Substation");
        NavMeshSurface surface = navRoot.GetComponent<NavMeshSurface>();
        if (surface == null) return "FAIL: NavMeshSurface missing";
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = -1;
        surface.agentTypeID = 0;
        surface.navMeshData = null;
        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavDataPath) != null)
            AssetDatabase.DeleteAsset(NavDataPath);
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
        NavMesh.RemoveAllNavMeshData();
        surface.BuildNavMesh();
        NavMeshData baked = surface.navMeshData;
        if (baked == null) return "FAIL: NavMeshSurface produced no data";
        AssetDatabase.CreateAsset(baked, NavDataPath);
        EditorUtility.SetDirty(surface);
        surface.AddData();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, TargetScene)) return "FAIL: save failed";
        AssetDatabase.Refresh();

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        return "OK scene=" + scene.name + " solids=" + layout.solids.Count + " decks=" + layout.decks.Count +
               " connectors=" + layout.connectors.Count + " enemyInstances=" + enemies.transform.childCount +
               " navVertices=" + tri.vertices.Length + " navTriangles=" + (tri.indices.Length / 3) +
               " navAsset=" + NavDataPath + " sceneDirty=" + scene.isDirty;
    }

    static GameObject MakeCube(string name, Vector3 position, Vector3 scale, Transform parent, Material material)
    {
        ProBuilderMesh pb = ShapeGenerator.GenerateCube(PivotLocation.Center, Vector3.one);
        pb.ToMesh();
        pb.Refresh();
        GameObject go = pb.gameObject;
        go.name = name;
        go.layer = StructureLayer;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        MeshCollider collider = go.GetComponent<MeshCollider>();
        if (collider == null) collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic | StaticEditorFlags.OccluderStatic);
        return go;
    }

    static void MakeRamp(ConnectorData c, Transform parent, Material material)
    {
        float dx = c.high[0] - c.low[0];
        float dz = c.high[1] - c.low[1];
        float run = Mathf.Sqrt(dx * dx + dz * dz);
        float length = Mathf.Sqrt(run * run + c.rise * c.rise);
        float yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        float slope = -Mathf.Atan2(c.rise, run) * Mathf.Rad2Deg;
        Vector3 mid = new Vector3((c.low[0] + c.high[0]) * 0.5f,
                                  c.rise * 0.5f,
                                  (c.low[1] + c.high[1]) * 0.5f);
        GameObject ramp = MakeCube(c.name, mid, new Vector3(3f, 0.25f, length), parent, material);
        ramp.transform.rotation = Quaternion.Euler(slope, yaw, 0f);
    }

    static void MakeStairs(ConnectorData c, Transform parent, Material material)
    {
        float dx = c.high[0] - c.low[0];
        float dz = c.high[1] - c.low[1];
        float yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        int steps = Mathf.RoundToInt(c.steps);
        for (int i = 0; i < steps; i++)
        {
            float f = (i + 0.5f) / steps;
            float h = (i + 1) * c.riser;
            Vector3 center = new Vector3(c.low[0] + dx * f, h * 0.5f, c.low[1] + dz * f);
            MakeCube(c.name + "_Step_" + (i + 1).ToString("00"), center,
                     new Vector3(3f, h, c.tread), parent, material).transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
