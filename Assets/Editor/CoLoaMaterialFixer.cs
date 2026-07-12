using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CoLoaMaterialFixer
{
    private const string ScenePath = "Assets/Scenes/S03.unity";
    private const string SceneObjectName = "coloa_map_stage03_paths";
    private const string SceneObjectSearchText = "coloa_map_stage03";
    private const string PreferredModelFolder = "Assets/Models/CoLoa";
    private const string AlternateModelFolder = "Assets/Models/Coloa";
    private const string RuntimeFolderName = "Materials_RuntimeFixed";
    private const string LegacyMaterialFolder = "Assets/Materials/CoLoa";

    private enum AssignmentGroup
    {
        Grass,
        Earth,
        Path,
        Stone,
        Roof,
        Wood,
        DarkOpening,
        Bronze,
        Fallback
    }

    private struct RuntimeMaterialSpec
    {
        public readonly string Name;
        public readonly Color32 Color;
        public readonly float Smoothness;
        public readonly AssignmentGroup Group;

        public RuntimeMaterialSpec(string name, byte r, byte g, byte b, float smoothness, AssignmentGroup group)
        {
            Name = name;
            Color = new Color32(r, g, b, 255);
            Smoothness = smoothness;
            Group = group;
        }
    }

    private struct Assignment
    {
        public readonly RuntimeMaterialSpec Spec;
        public readonly bool IsFallback;

        public Assignment(RuntimeMaterialSpec spec, bool isFallback)
        {
            Spec = spec;
            IsFallback = isFallback;
        }
    }

    private static readonly RuntimeMaterialSpec Grass = new RuntimeMaterialSpec("Runtime_Grass_Green", 68, 112, 63, 0.22f, AssignmentGroup.Grass);
    private static readonly RuntimeMaterialSpec EarthDark = new RuntimeMaterialSpec("Runtime_Earth_Dark", 92, 72, 51, 0.2f, AssignmentGroup.Earth);
    private static readonly RuntimeMaterialSpec EarthGrassBlend = new RuntimeMaterialSpec("Runtime_Earth_Grass_Blend", 82, 103, 68, 0.22f, AssignmentGroup.Earth);
    private static readonly RuntimeMaterialSpec PathCompacted = new RuntimeMaterialSpec("Runtime_Path_Compacted_Earth", 142, 105, 66, 0.18f, AssignmentGroup.Path);
    private static readonly RuntimeMaterialSpec PathEdge = new RuntimeMaterialSpec("Runtime_Path_Edge_Darker_Earth", 92, 65, 42, 0.18f, AssignmentGroup.Path);
    private static readonly RuntimeMaterialSpec StoneLight = new RuntimeMaterialSpec("Runtime_Stone_Light_Gray", 150, 150, 140, 0.24f, AssignmentGroup.Stone);
    private static readonly RuntimeMaterialSpec StoneDark = new RuntimeMaterialSpec("Runtime_Stone_Dark_Gray", 80, 82, 78, 0.23f, AssignmentGroup.Stone);
    private static readonly RuntimeMaterialSpec RoofClay = new RuntimeMaterialSpec("Runtime_Roof_Dark_Red_Clay", 135, 58, 38, 0.2f, AssignmentGroup.Roof);
    private static readonly RuntimeMaterialSpec WoodDark = new RuntimeMaterialSpec("Runtime_Wood_Dark_Brown", 72, 39, 22, 0.2f, AssignmentGroup.Wood);
    private static readonly RuntimeMaterialSpec DarkOpening = new RuntimeMaterialSpec("Runtime_Dark_Opening", 20, 18, 15, 0.15f, AssignmentGroup.DarkOpening);
    private static readonly RuntimeMaterialSpec BronzeDark = new RuntimeMaterialSpec("Runtime_Bronze_Dark", 105, 76, 38, 0.26f, AssignmentGroup.Bronze);

    private static readonly RuntimeMaterialSpec[] RuntimeSpecs =
    {
        Grass,
        EarthDark,
        EarthGrassBlend,
        PathCompacted,
        PathEdge,
        StoneLight,
        StoneDark,
        RoofClay,
        WoodDark,
        DarkOpening,
        BronzeDark,
    };

    [MenuItem("Tools/CoLoa/Force Fix Scene Materials")]
    public static void ForceFixSceneMaterials()
    {
        OpenS03SceneIfNeeded();
        GameObject mapRoot = FindCoLoaSceneObject();
        if (mapRoot == null)
        {
            Debug.LogError($"CoLoaMaterialFixer: Scene object '{SceneObjectName}' was not found, and no object contains '{SceneObjectSearchText}'.");
            return;
        }

        string runtimeMaterialFolder = GetRuntimeMaterialFolder();
        if (string.IsNullOrEmpty(runtimeMaterialFolder))
        {
            Debug.LogError($"CoLoaMaterialFixer: Neither {PreferredModelFolder} nor {AlternateModelFolder} exists. Cannot create runtime material folder.");
            return;
        }

        EnsureFolder(runtimeMaterialFolder);
        Dictionary<string, Material> runtimeMaterials = CreateOrUpdateRuntimeMaterials(runtimeMaterialFolder, out int materialsCreated, out int materialsUpdated);

        Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>(true);
        int materialSlotsReplaced = 0;
        Dictionary<AssignmentGroup, int> groupCounts = CreateGroupCountDictionary();
        int fallbackCount = 0;
        List<string> assignmentPreview = new List<string>();

        foreach (Renderer renderer in renderers)
        {
            Assignment assignment = ClassifyRenderer(renderer, mapRoot);
            Material replacement = runtimeMaterials[assignment.Spec.Name];
            Material[] currentMaterials = renderer.sharedMaterials;
            Material[] replacementMaterials = new Material[currentMaterials.Length];

            for (int i = 0; i < replacementMaterials.Length; i++)
                replacementMaterials[i] = replacement;

            Undo.RecordObject(renderer, "Force Fix CoLoa Scene Materials");
            renderer.sharedMaterials = replacementMaterials;
            EditorUtility.SetDirty(renderer);

            materialSlotsReplaced += replacementMaterials.Length;
            groupCounts[assignment.Spec.Group]++;
            if (assignment.IsFallback)
                fallbackCount++;

            if (assignmentPreview.Count < 150)
                assignmentPreview.Add($"{GetTransformPath(renderer.transform, mapRoot.transform)} -> {assignment.Spec.Name}");
        }

        ApplyLightingFix();

        List<string> whiteMaterials = new List<string>();
        int renderersStillUsingWhiteMaterials = CountRenderersUsingWhiteMaterials(renderers, whiteMaterials);
        bool oldFolderStillUsed = IsLegacyMaterialFolderStillUsed(renderers, out List<string> legacyMaterialReferences);

        foreach (Material material in runtimeMaterials.Values)
            EditorUtility.SetDirty(material);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        bool sceneSaved = EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log(
            "CoLoaMaterialFixer Force Fix Scene Materials complete.\n" +
            $"Scene object found: {mapRoot.name}.\n" +
            $"Material folder used: {runtimeMaterialFolder}.\n" +
            $"Runtime materials created/updated: created {materialsCreated}, updated {materialsUpdated}.\n" +
            $"Renderer count processed: {renderers.Length}.\n" +
            $"Material slots replaced: {materialSlotsReplaced}.\n" +
            $"Assigned grass: {groupCounts[AssignmentGroup.Grass]}.\n" +
            $"Assigned earth: {groupCounts[AssignmentGroup.Earth]}.\n" +
            $"Assigned path: {groupCounts[AssignmentGroup.Path]}.\n" +
            $"Assigned stone: {groupCounts[AssignmentGroup.Stone]}.\n" +
            $"Assigned roof: {groupCounts[AssignmentGroup.Roof]}.\n" +
            $"Assigned wood: {groupCounts[AssignmentGroup.Wood]}.\n" +
            $"Assigned dark opening: {groupCounts[AssignmentGroup.DarkOpening]}.\n" +
            $"Assigned bronze: {groupCounts[AssignmentGroup.Bronze]}.\n" +
            $"Assigned fallback: {fallbackCount}.\n" +
            $"Renderers still using a pure white material: {renderersStillUsingWhiteMaterials}.\n" +
            $"Remaining white materials: {FormatList(whiteMaterials)}.\n" +
            $"Materials from {LegacyMaterialFolder} still used: {oldFolderStillUsed}. {FormatList(legacyMaterialReferences)}.\n" +
            $"Scene saved: {sceneSaved}.\n" +
            "First 150 renderer assignments:\n" +
            FormatMultilineList(assignmentPreview));
    }

    private static void OpenS03SceneIfNeeded()
    {
        if (EditorSceneManager.GetActiveScene().path == ScenePath)
            return;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/CoLoa/Fix Map Materials")]
    public static void FixMapMaterials()
    {
        ForceFixSceneMaterials();
    }

    [MenuItem("Tools/CoLoa/Select CoLoa Map")]
    public static void SelectCoLoaMap()
    {
        GameObject mapRoot = FindCoLoaSceneObject();
        if (mapRoot == null)
        {
            Debug.LogError($"CoLoaMaterialFixer: Scene object '{SceneObjectName}' was not found.");
            return;
        }

        Selection.activeGameObject = mapRoot;
        EditorGUIUtility.PingObject(mapRoot);

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log($"CoLoaMaterialFixer: Selected scene object '{mapRoot.name}'.");
    }

    private static Dictionary<string, Material> CreateOrUpdateRuntimeMaterials(string materialFolder, out int created, out int updated)
    {
        Shader shader = FindVisibleShader();
        if (shader == null)
            throw new System.InvalidOperationException("CoLoaMaterialFixer: Could not find a supported color shader.");

        Dictionary<string, Material> materials = new Dictionary<string, Material>();
        created = 0;
        updated = 0;

        foreach (RuntimeMaterialSpec spec in RuntimeSpecs)
        {
            string path = $"{materialFolder}/{spec.Name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = spec.Name };
                AssetDatabase.CreateAsset(material, path);
                created++;
            }
            else
            {
                if (material.shader != shader)
                    material.shader = shader;
                updated++;
            }

            ConfigureRuntimeMaterial(material, spec);
            EditorUtility.SetDirty(material);
            materials.Add(spec.Name, material);
        }

        return materials;
    }

    private static Shader FindVisibleShader()
    {
        return Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
    }

    private static void ConfigureRuntimeMaterial(Material material, RuntimeMaterialSpec spec)
    {
        Color color = spec.Color;
        material.renderQueue = -1;
        material.SetOverrideTag("RenderType", "Opaque");

        SetFloatIfPresent(material, "_Surface", 0f);
        SetFloatIfPresent(material, "_Blend", 0f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);
        SetFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        SetFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        SetFloatIfPresent(material, "_ZWrite", 1f);
        SetFloatIfPresent(material, "_Metallic", 0f);
        SetFloatIfPresent(material, "_Smoothness", spec.Smoothness);
        SetFloatIfPresent(material, "_Glossiness", spec.Smoothness);
        SetFloatIfPresent(material, "_GlossMapScale", spec.Smoothness);

        SetTextureIfPresent(material, "_BaseMap", null);
        SetTextureIfPresent(material, "_MainTex", null);
        SetTextureIfPresent(material, "_MetallicGlossMap", null);
        SetTextureIfPresent(material, "_SpecGlossMap", null);
        SetTextureIfPresent(material, "_BumpMap", null);
        SetTextureIfPresent(material, "_EmissionMap", null);

        SetColorIfPresent(material, "_BaseColor", color);
        SetColorIfPresent(material, "_Color", color);
        SetColorIfPresent(material, "_Base_Color", color);
        SetColorIfPresent(material, "_TintColor", color);
        SetColorIfPresent(material, "_MainColor", color);
        SetColorIfPresent(material, "baseColor", color);

        try
        {
            material.color = color;
        }
        catch
        {
            // Some shaders expose _BaseColor but not mainColor/_Color.
        }

        SetColorIfPresent(material, "_BaseColor", color);
        SetColorIfPresent(material, "_EmissionColor", Color.black);

        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_EMISSION");
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_NORMALMAP");
    }

    private static Assignment ClassifyRenderer(Renderer renderer, GameObject mapRoot)
    {
        string text = BuildClassificationText(renderer, mapRoot);
        bool veryLarge = renderer.bounds.size.x > 100f || renderer.bounds.size.z > 100f;

        if (ContainsAny(text, "path_edge", "edge_darker"))
            return new Assignment(PathEdge, false);

        if (ContainsAny(text, "terrain_base", "terrain central", "central_plateau", "plateau", "grass", "ground", "level_top", "top_surface"))
            return new Assignment(Grass, false);

        if (ContainsAny(text, "earth", "slope", "earthwork", "embankment", "terrace", "dirt_side", "side_slope"))
        {
            RuntimeMaterialSpec earthSpec = ContainsAny(text, "earth_grass", "grass_blend", "blend", "transition")
                ? EarthGrassBlend
                : EarthDark;

            return new Assignment(earthSpec, false);
        }

        if (ContainsAny(text, "path", "access", "road", "route"))
            return new Assignment(PathCompacted, false);

        if (ContainsAny(text, "roof", "clay", "tile"))
            return new Assignment(RoofClay, false);

        if (ContainsAny(text, "opening", "shadow", "gap"))
            return new Assignment(DarkOpening, false);

        if (ContainsAny(text, "door", "wood", "column"))
            return new Assignment(WoodDark, false);

        if (ContainsAny(text, "wallmodule", "wallconnector", "wall", "stone", "pillar", "trim", "base", "gate_trim", "gate"))
        {
            RuntimeMaterialSpec stoneSpec = ContainsAny(text, "dark", "trim", "gate", "connector")
                ? StoneDark
                : StoneLight;

            return new Assignment(stoneSpec, false);
        }

        if (ContainsAny(text, "bronze", "accent"))
            return new Assignment(BronzeDark, false);

        if (ContainsAny(text, "dark"))
            return new Assignment(DarkOpening, false);

        if (veryLarge && !ContainsAny(text, "earth", "slope", "path", "wall", "gate", "roof", "door", "wood", "stone", "trim"))
            return new Assignment(Grass, true);

        return new Assignment(StoneLight, true);
    }

    private static string BuildClassificationText(Renderer renderer, GameObject mapRoot)
    {
        List<string> parts = new List<string> { renderer.gameObject.name };

        Transform current = renderer.transform.parent;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        Mesh mesh = GetSharedMesh(renderer);
        if (mesh != null)
            parts.Add(mesh.name);

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null || material.name.StartsWith("Runtime_", System.StringComparison.OrdinalIgnoreCase))
                continue;

            parts.Add(material.name);
        }

        string text = string.Join(" ", parts).ToLowerInvariant();
        text = text.Replace(SceneObjectName.ToLowerInvariant(), string.Empty);
        text = text.Replace(SceneObjectSearchText.ToLowerInvariant(), string.Empty);
        text = text.Replace("stage03_paths", string.Empty);
        return text;
    }

    private static Mesh GetSharedMesh(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            return skinnedMeshRenderer.sharedMesh;

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        return meshFilter != null ? meshFilter.sharedMesh : null;
    }

    private static void ApplyLightingFix()
    {
        GameObject lightObject = FindSceneObjectByExactName("Directional Light");
        if (lightObject == null)
            return;

        Light light = lightObject.GetComponent<Light>();
        if (light == null)
            return;

        Undo.RecordObject(light, "Fix CoLoa Directional Light");
        light.intensity = 0.8f;
        light.color = new Color(1f, 0.956f, 0.88f, 1f);
        EditorUtility.SetDirty(light);
    }

    private static GameObject FindCoLoaSceneObject()
    {
        GameObject exact = FindSceneObjectByExactName(SceneObjectName);
        if (exact != null)
            return exact;

        return EnumerateSceneObjects()
            .FirstOrDefault(gameObject => gameObject.name.IndexOf(SceneObjectSearchText, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static GameObject FindSceneObjectByExactName(string objectName)
    {
        return EnumerateSceneObjects()
            .FirstOrDefault(gameObject => string.Equals(gameObject.name, objectName, System.StringComparison.Ordinal));
    }

    private static IEnumerable<GameObject> EnumerateSceneObjects()
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                yield return transform.gameObject;
        }
    }

    private static string GetRuntimeMaterialFolder()
    {
        if (AssetDatabase.IsValidFolder(PreferredModelFolder))
            return $"{PreferredModelFolder}/{RuntimeFolderName}";

        if (AssetDatabase.IsValidFolder(AlternateModelFolder))
            return $"{AlternateModelFolder}/{RuntimeFolderName}";

        if (Directory.Exists(PreferredModelFolder))
            return $"{PreferredModelFolder}/{RuntimeFolderName}";

        if (Directory.Exists(AlternateModelFolder))
            return $"{AlternateModelFolder}/{RuntimeFolderName}";

        return string.Empty;
    }

    private static int CountRenderersUsingWhiteMaterials(Renderer[] renderers, List<string> whiteMaterials)
    {
        HashSet<string> whiteMaterialSet = new HashSet<string>();
        int rendererCount = 0;

        foreach (Renderer renderer in renderers)
        {
            bool rendererUsesWhite = false;
            foreach (Material material in renderer.sharedMaterials)
            {
                if (!IsPureWhiteMaterial(material))
                    continue;

                rendererUsesWhite = true;
                string path = AssetDatabase.GetAssetPath(material);
                whiteMaterialSet.Add(string.IsNullOrEmpty(path) ? material.name : $"{material.name} ({path})");
            }

            if (rendererUsesWhite)
                rendererCount++;
        }

        whiteMaterials.AddRange(whiteMaterialSet.OrderBy(value => value));
        return rendererCount;
    }

    private static bool IsPureWhiteMaterial(Material material)
    {
        if (material == null || !TryGetMaterialColor(material, out Color color))
            return false;

        return color.r >= 0.98f && color.g >= 0.98f && color.b >= 0.98f;
    }

    private static bool TryGetMaterialColor(Material material, out Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            color = material.GetColor("_BaseColor");
            return true;
        }

        if (material.HasProperty("_Color"))
        {
            color = material.GetColor("_Color");
            return true;
        }

        try
        {
            color = material.color;
            return true;
        }
        catch
        {
            color = default;
            return false;
        }
    }

    private static bool IsLegacyMaterialFolderStillUsed(Renderer[] renderers, out List<string> legacyMaterialReferences)
    {
        HashSet<string> references = new HashSet<string>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string path = NormalizePath(AssetDatabase.GetAssetPath(material));
                if (path.StartsWith(LegacyMaterialFolder + "/", System.StringComparison.Ordinal))
                    references.Add(path);
            }
        }

        legacyMaterialReferences = references.OrderBy(value => value).ToList();
        return legacyMaterialReferences.Count > 0;
    }

    private static Dictionary<AssignmentGroup, int> CreateGroupCountDictionary()
    {
        Dictionary<AssignmentGroup, int> counts = new Dictionary<AssignmentGroup, int>();
        foreach (AssignmentGroup group in System.Enum.GetValues(typeof(AssignmentGroup)))
            counts[group] = 0;
        return counts;
    }

    private static void EnsureFolder(string path)
    {
        string normalizedPath = NormalizePath(path);
        if (AssetDatabase.IsValidFolder(normalizedPath))
            return;

        string parent = NormalizePath(Path.GetDirectoryName(normalizedPath));
        string folderName = Path.GetFileName(normalizedPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }

    private static void SetColorIfPresent(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
            material.SetColor(propertyName, value);
    }

    private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
    {
        if (material.HasProperty(propertyName))
            material.SetTexture(propertyName, texture);
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (string needle in needles)
        {
            if (text.Contains(needle))
                return true;
        }

        return false;
    }

    private static string GetTransformPath(Transform transform, Transform root)
    {
        Stack<string> names = new Stack<string>();
        Transform current = transform;

        while (current != null)
        {
            names.Push(current.name);
            if (current == root)
                break;
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }

    private static string FormatList(IEnumerable<string> values)
    {
        string[] items = values == null
            ? new string[0]
            : values.Where(value => !string.IsNullOrEmpty(value)).Distinct().OrderBy(value => value).ToArray();

        return items.Length == 0 ? "(none)" : string.Join(", ", items);
    }

    private static string FormatMultilineList(IEnumerable<string> values)
    {
        string[] items = values == null ? new string[0] : values.ToArray();
        return items.Length == 0 ? "(none)" : string.Join("\n", items);
    }
}
