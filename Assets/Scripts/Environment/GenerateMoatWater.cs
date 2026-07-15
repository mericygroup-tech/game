using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Generates segmented, static moat water for the Co Loa map.
/// The generator reads the dry moat submesh from the map, estimates circular bands,
/// then places a small WaterSegment prefab around each ring.
/// </summary>
[DisallowMultipleComponent]
public sealed class GenerateMoatWater : MonoBehaviour
{
    private const string DefaultWaterRootName = "Water";
    private const string DefaultSegmentPrefabPath = "Assets/Prefabs/Environment/WaterSegment.prefab";
    private const string DefaultWaterMaterialPath = "Assets/Materials/Environment/Water_Moat_Static.mat";

    [Header("Scene References")]
    [SerializeField] private Transform mapRoot;
    [SerializeField] private Transform waterRoot;
    [SerializeField] private GameObject waterSegmentPrefab;
    [SerializeField] private Material waterMaterial;

    [Header("Generation")]
    [SerializeField] private string waterRootName = DefaultWaterRootName;
    [SerializeField] private bool useDetectedWaterHeight = true;
    [SerializeField] private float waterHeight = -2.75f;
    [SerializeField] private float waterSurfaceOffset = 0.33f;
    [SerializeField] private float waterWidth = 0f;
    [SerializeField, Range(0.75f, 1f)] private float segmentLengthFill = 0.94f;
    [SerializeField, Min(0.01f)] private float segmentThickness = 0.04f;

    [Header("Rings")]
    [SerializeField] private List<RingSettings> rings = new List<RingSettings>
    {
        new RingSettings("Ring_01", 32),
        new RingSettings("Ring_02", 32),
        new RingSettings("Ring_03", 28),
    };

    /// <summary>
    /// Deletes old generated water and builds a fresh Environment/Water hierarchy.
    /// </summary>
    public void RegenerateWater()
    {
        if (mapRoot == null)
            mapRoot = FindCoLoaMapRoot();

        if (mapRoot == null)
        {
            Debug.LogError("GenerateMoatWater: Cannot find the Co Loa map root in the active scene.", this);
            return;
        }

#if UNITY_EDITOR
        EnsureAssets();
#endif

        waterRoot = ResolveWaterRoot(true);
        ClearGeneratedWater();

        List<MeasuredMoatRing> measuredRings = DetectMoatRings(mapRoot);
        if (measuredRings.Count == 0)
        {
            Debug.LogError("GenerateMoatWater: Could not find a dry moat submesh on the Co Loa map.", this);
            return;
        }

        measuredRings.Sort((a, b) => a.InnerRadius.CompareTo(b.InnerRadius));
        int ringCount = Mathf.Min(measuredRings.Count, rings.Count);
        for (int i = 0; i < ringCount; i++)
        {
            RingSettings settings = rings[i];
            if (settings == null || !settings.enabled)
                continue;

            GenerateRing(settings, measuredRings[i], i);
        }

#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
    }

    /// <summary>
    /// Removes only generated water objects. Existing map/gameplay objects are left untouched.
    /// </summary>
    public void ClearGeneratedWater()
    {
        Transform root = ResolveWaterRoot(false);
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name.StartsWith("Ring_", System.StringComparison.Ordinal))
                DestroyGeneratedObject(child.gameObject);
        }

#if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
    }

    private void GenerateRing(RingSettings settings, MeasuredMoatRing measuredRing, int ringIndex)
    {
        int segmentCount = Mathf.Clamp(settings.segmentCount, 8, 40);
        string ringName = string.IsNullOrWhiteSpace(settings.ringName)
            ? "Ring_" + (ringIndex + 1).ToString("00")
            : settings.ringName.Trim();

        GameObject ringObject = new GameObject(ringName);
        ringObject.transform.SetParent(waterRoot, false);
        ringObject.isStatic = true;

        float width = settings.waterWidth > 0.01f ? settings.waterWidth : waterWidth;
        if (width <= 0.01f)
            width = Mathf.Max(0.5f, measuredRing.OuterRadius - measuredRing.InnerRadius);

        float radius = Mathf.Max(0.5f, (measuredRing.InnerRadius + measuredRing.OuterRadius) * 0.5f);
        float height = useDetectedWaterHeight
            ? measuredRing.MinY + waterSurfaceOffset
            : (Mathf.Abs(settings.waterHeight) > 0.001f ? settings.waterHeight : waterHeight);

        float circumference = 2f * Mathf.PI * radius;
        float segmentLength = (circumference / segmentCount) * segmentLengthFill;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (i + 0.5f) / segmentCount * Mathf.PI * 2f;
            Vector3 radial = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 position = measuredRing.Center + radial * radius;
            position.y = height;

            GameObject segment = InstantiateSegmentPrefab(ringObject.transform);
            segment.name = "WaterSegment_" + (i + 1).ToString("00");
            segment.transform.position = position;

            // Local Z points across the moat width; local X follows the tangent of the ring.
            segment.transform.rotation = Quaternion.LookRotation(radial, Vector3.up);
            segment.transform.localScale = new Vector3(segmentLength, segmentThickness, width);
            segment.isStatic = true;

            RemoveColliders(segment);
            AssignWaterMaterial(segment);
        }
    }

    private GameObject InstantiateSegmentPrefab(Transform parent)
    {
#if UNITY_EDITOR
        if (waterSegmentPrefab != null)
        {
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(waterSegmentPrefab, parent) as GameObject;
            if (prefabInstance != null)
                return prefabInstance;
        }
#endif

        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.transform.SetParent(parent, false);
        RemoveColliders(fallback);
        return fallback;
    }

    private void AssignWaterMaterial(GameObject segment)
    {
        MeshRenderer renderer = segment.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = segment.AddComponent<MeshRenderer>();

        renderer.sharedMaterial = waterMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void RemoveColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            DestroyGeneratedObject(collider);
    }

    private Transform ResolveWaterRoot(bool createIfMissing)
    {
        if (waterRoot != null)
            return waterRoot;

        Transform existingChild = transform.Find(waterRootName);
        if (existingChild != null)
        {
            waterRoot = existingChild;
            return waterRoot;
        }

        if (!createIfMissing)
            return null;

        GameObject root = new GameObject(string.IsNullOrWhiteSpace(waterRootName) ? DefaultWaterRootName : waterRootName);
        root.transform.SetParent(transform, false);
        waterRoot = root.transform;
        return waterRoot;
    }

    private List<MeasuredMoatRing> DetectMoatRings(Transform root)
    {
        List<MeasuredMoatRing> rawRings = CollectMoatSubmeshComponents(root);
        if (rawRings.Count == 0)
            return rawRings;

        Vector3 commonCenter = CalculateWeightedCenter(rawRings);
        foreach (MeasuredMoatRing ring in rawRings)
            ring.CalculateCircularBand(commonCenter, 0.02f, 0.98f);

        rawRings.Sort((a, b) => a.InnerRadius.CompareTo(b.InnerRadius));
        EnforceRingGaps(rawRings, 4f);
        return rawRings;
    }

    private List<MeasuredMoatRing> CollectMoatSubmeshComponents(Transform root)
    {
        List<MeasuredMoatRing> result = new List<MeasuredMoatRing>();
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                if (!IsDryMoatSubmesh(renderer, mesh, materials, subMeshIndex))
                    continue;

                AddConnectedSubmeshRings(renderer.transform, mesh, subMeshIndex, result);
            }
        }

        return result;
    }

    private bool IsDryMoatSubmesh(Renderer renderer, Mesh mesh, Material[] materials, int subMeshIndex)
    {
        if (materials != null && subMeshIndex < materials.Length && IsDryMoatMaterial(materials[subMeshIndex]))
            return true;

        // CoLoa terrain GLB stores dry moat earth as submesh 2 on Terrain_CoLoa_600x600.
        // This fallback still works after material-fix tools replace scene materials.
        string meshName = mesh != null ? mesh.name : string.Empty;
        string rendererName = renderer != null ? renderer.name : string.Empty;
        bool terrainMesh = ContainsIgnoreCase(meshName, "Terrain_CoLoa") || ContainsIgnoreCase(rendererName, "Terrain_CoLoa");
        return terrainMesh && subMeshIndex == 2;
    }

    private static bool IsDryMoatMaterial(Material material)
    {
        if (material == null)
            return false;

        string materialName = material.name;
        return ContainsIgnoreCase(materialName, "Dry_Moat") ||
               (ContainsIgnoreCase(materialName, "Dry") && ContainsIgnoreCase(materialName, "Moat"));
    }

    private static void AddConnectedSubmeshRings(
        Transform meshTransform,
        Mesh mesh,
        int subMeshIndex,
        List<MeasuredMoatRing> result)
    {
        int[] triangles = mesh.GetTriangles(subMeshIndex);
        if (triangles == null || triangles.Length < 3)
            return;

        UnionFind unionFind = new UnionFind(mesh.vertexCount);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];
            unionFind.Add(a);
            unionFind.Add(b);
            unionFind.Add(c);
            unionFind.Union(a, b);
            unionFind.Union(a, c);
        }

        Dictionary<int, MeasuredMoatRing> byRoot = new Dictionary<int, MeasuredMoatRing>();
        Vector3[] vertices = mesh.vertices;
        foreach (int vertexIndex in triangles)
        {
            int root = unionFind.Find(vertexIndex);
            if (!byRoot.TryGetValue(root, out MeasuredMoatRing ring))
            {
                ring = new MeasuredMoatRing();
                byRoot.Add(root, ring);
            }

            ring.AddVertex(meshTransform.TransformPoint(vertices[vertexIndex]));
        }

        foreach (MeasuredMoatRing ring in byRoot.Values)
        {
            if (ring.VertexCount >= 24)
                result.Add(ring);
        }
    }

    private static Vector3 CalculateWeightedCenter(List<MeasuredMoatRing> measuredRings)
    {
        Vector3 weightedCenter = Vector3.zero;
        int totalWeight = 0;
        foreach (MeasuredMoatRing ring in measuredRings)
        {
            weightedCenter += ring.AveragePosition * ring.VertexCount;
            totalWeight += ring.VertexCount;
        }

        return totalWeight > 0 ? weightedCenter / totalWeight : Vector3.zero;
    }

    private static void EnforceRingGaps(List<MeasuredMoatRing> measuredRings, float minimumGap)
    {
        for (int i = 0; i < measuredRings.Count - 1; i++)
        {
            MeasuredMoatRing current = measuredRings[i];
            MeasuredMoatRing next = measuredRings[i + 1];
            if (current.OuterRadius + minimumGap <= next.InnerRadius)
                continue;

            float midpoint = (current.OuterRadius + next.InnerRadius) * 0.5f;
            current.OuterRadius = Mathf.Max(current.InnerRadius + 0.25f, midpoint - minimumGap * 0.5f);
            next.InnerRadius = Mathf.Min(next.OuterRadius - 0.25f, midpoint + minimumGap * 0.5f);
        }
    }

    private static bool ContainsIgnoreCase(string value, string token)
    {
        return !string.IsNullOrEmpty(value) &&
               !string.IsNullOrEmpty(token) &&
               value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Transform FindCoLoaMapRoot()
    {
        GameObject exact = GameObject.Find("coloa_three_ring_terrain_bridges_v02");
        if (exact != null)
            return exact.transform;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in transforms)
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;

            string name = candidate.name.ToLowerInvariant();
            if (name.Contains("coloa") || name.Contains("co_loa"))
                return candidate;
        }

        return null;
    }

    private static void DestroyGeneratedObject(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    [System.Serializable]
    public sealed class RingSettings
    {
        public bool enabled = true;
        public string ringName = "Ring_01";
        [Range(8, 40)] public int segmentCount = 32;
        public float waterHeight = -2.75f;
        public float waterWidth = 0f;

        public RingSettings()
        {
        }

        public RingSettings(string ringName, int segmentCount)
        {
            this.ringName = ringName;
            this.segmentCount = segmentCount;
        }
    }

    private sealed class MeasuredMoatRing
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private Vector3 sum;

        public int VertexCount => vertices.Count;
        public Vector3 AveragePosition => VertexCount > 0 ? sum / VertexCount : Vector3.zero;
        public Vector3 Center { get; private set; }
        public float MinY { get; private set; } = float.PositiveInfinity;
        public float InnerRadius { get; set; }
        public float OuterRadius { get; set; }

        public void AddVertex(Vector3 vertex)
        {
            vertices.Add(vertex);
            sum += vertex;
            MinY = Mathf.Min(MinY, vertex.y);
        }

        public void CalculateCircularBand(Vector3 commonCenter, float innerPercentile, float outerPercentile)
        {
            Center = commonCenter;
            List<float> distances = new List<float>(vertices.Count);
            foreach (Vector3 vertex in vertices)
                distances.Add(DistanceOnXZ(commonCenter, vertex));

            distances.Sort();
            InnerRadius = Percentile(distances, innerPercentile);
            OuterRadius = Percentile(distances, outerPercentile);
        }

        private static float Percentile(List<float> sortedValues, float percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0)
                return 0f;

            int index = Mathf.Clamp(
                Mathf.RoundToInt((sortedValues.Count - 1) * Mathf.Clamp01(percentile)),
                0,
                sortedValues.Count - 1);
            return sortedValues[index];
        }
    }

    private sealed class UnionFind
    {
        private readonly int[] parents;

        public UnionFind(int count)
        {
            parents = new int[Mathf.Max(0, count)];
            for (int i = 0; i < parents.Length; i++)
                parents[i] = -1;
        }

        public void Add(int index)
        {
            if (index >= 0 && index < parents.Length && parents[index] < 0)
                parents[index] = index;
        }

        public int Find(int index)
        {
            Add(index);
            if (index < 0 || index >= parents.Length)
                return -1;

            if (parents[index] == index)
                return index;

            parents[index] = Find(parents[index]);
            return parents[index];
        }

        public void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA >= 0 && rootB >= 0 && rootA != rootB)
                parents[rootB] = rootA;
        }
    }

    private static float DistanceOnXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Generate Co Loa Water")]
    public static void GenerateFromMenu()
    {
        Transform environment = ResolveEnvironmentRoot();
        GenerateMoatWater generator = environment.GetComponent<GenerateMoatWater>();
        if (generator == null)
            generator = environment.gameObject.AddComponent<GenerateMoatWater>();

        generator.mapRoot = FindCoLoaMapRoot();
        generator.EnsureAssets();
        generator.RegenerateWater();

        Selection.activeGameObject = generator.gameObject;
        EditorUtility.SetDirty(generator);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    private void EnsureAssets()
    {
        if (waterMaterial == null)
            waterMaterial = LoadOrCreateWaterMaterial();

        if (waterSegmentPrefab == null)
            waterSegmentPrefab = LoadOrCreateWaterSegmentPrefab(waterMaterial);
    }

    private static Transform ResolveEnvironmentRoot()
    {
        GameObject environment = GameObject.Find("Environment");
        if (environment == null)
            environment = GameObject.Find("Enviroment");

        if (environment == null)
            environment = new GameObject("Environment");

        return environment.transform;
    }

    private static Material LoadOrCreateWaterMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DefaultWaterMaterialPath);
        if (material == null)
        {
            EnsureFolder("Assets/Materials/Environment");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = "Water_Moat_Static" };
            AssetDatabase.CreateAsset(material, DefaultWaterMaterialPath);
        }

        ConfigureWaterMaterial(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureWaterMaterial(Material material)
    {
        if (material == null)
            return;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null)
            material.shader = urpLit;

        Color moatColor = new Color(0.055f, 0.19f, 0.13f, 0.72f);
        SetFloat(material, "_Surface", 1f);
        SetFloat(material, "_Blend", 0f);
        SetFloat(material, "_AlphaClip", 0f);
        SetFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        SetFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        SetFloat(material, "_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        SetFloat(material, "_ZWrite", 0f);
        SetFloat(material, "_Metallic", 0f);
        SetFloat(material, "_Smoothness", 0.88f);
        SetFloat(material, "_Glossiness", 0.88f);
        SetFloat(material, "_SpecularHighlights", 1f);
        SetFloat(material, "_EnvironmentReflections", 1f);
        SetColor(material, "_BaseColor", moatColor);
        SetColor(material, "_Color", moatColor);
        SetColor(material, "_SpecColor", new Color(0.16f, 0.22f, 0.16f, 1f));

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
    }

    private static GameObject LoadOrCreateWaterSegmentPrefab(Material material)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSegmentPrefabPath);
        if (prefab != null)
            return prefab;

        EnsureFolder(Path.GetDirectoryName(DefaultSegmentPrefabPath).Replace('\\', '/'));

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = "WaterSegment";
        RemoveColliders(segment);
        segment.isStatic = true;

        MeshRenderer renderer = segment.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        prefab = PrefabUtility.SaveAsPrefabAsset(segment, DefaultSegmentPrefabPath);
        DestroyImmediate(segment);
        return prefab;
    }

    private static void EnsureFolder(string folder)
    {
        string normalized = folder.Replace('\\', '/').TrimEnd('/');
        if (AssetDatabase.IsValidFolder(normalized))
            return;

        string parent = Path.GetDirectoryName(normalized);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent.Replace('\\', '/'));

        string grandParent = Path.GetDirectoryName(normalized);
        string leaf = Path.GetFileName(normalized);
        if (!string.IsNullOrEmpty(grandParent) && !string.IsNullOrEmpty(leaf))
            AssetDatabase.CreateFolder(grandParent.Replace('\\', '/'), leaf);
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }

    private static void SetColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
            material.SetColor(propertyName, value);
    }

    [CustomEditor(typeof(GenerateMoatWater))]
    private sealed class GenerateMoatWaterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);

            GenerateMoatWater generator = (GenerateMoatWater)target;
            if (GUILayout.Button("Regenerate Water", GUILayout.Height(30f)))
            {
                Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Regenerate Co Loa Water");
                generator.RegenerateWater();
                EditorUtility.SetDirty(generator);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            if (GUILayout.Button("Clear Water", GUILayout.Height(24f)))
            {
                Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Clear Co Loa Water");
                generator.ClearGeneratedWater();
                EditorUtility.SetDirty(generator);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
#endif
}
