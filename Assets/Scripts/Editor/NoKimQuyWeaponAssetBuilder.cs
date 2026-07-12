using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class NoKimQuyWeaponAssetBuilder
{
    public const string WeaponRootFolder = "Assets/Models/Weapons";
    public const string NoKimQuyFolder = WeaponRootFolder + "/NoKimQuy";
    public const string TextureFolder = NoKimQuyFolder + "/Textures";
    public const string MaterialFolder = NoKimQuyFolder + "/Materials";
    public const string PrefabFolder = NoKimQuyFolder + "/Prefabs";
    public const string AlbedoPath = TextureFolder + "/NoKimQuy_Albedo.png";
    public const string IconPath = TextureFolder + "/NoKimQuy_Icon.png";
    public const string MeshPath = NoKimQuyFolder + "/NoKimQuy_HandheldMesh.asset";
    public const string MaterialPath = MaterialFolder + "/NoKimQuy_Transparent.mat";
    public const string SwordBladeMaterialPath = MaterialFolder + "/VanAn_Sword_Blade.mat";
    public const string SwordHiltMaterialPath = MaterialFolder + "/VanAn_Sword_Hilt.mat";
    public const string PrefabPath = PrefabFolder + "/NoKimQuy_Handheld.prefab";

    [MenuItem("Tools/Dong Chay Anh Hung/Weapons/Rebuild No Kim Quy Assets")]
    public static void RebuildFromMenu()
    {
        GameObject prefab = EnsureAssets();
        Selection.activeObject = prefab;
    }

    public static GameObject EnsureAssets()
    {
        EnsureFolder("Assets", "Models");
        EnsureFolder("Assets/Models", "Weapons");
        EnsureFolder(WeaponRootFolder, "NoKimQuy");
        EnsureFolder(NoKimQuyFolder, "Textures");
        EnsureFolder(NoKimQuyFolder, "Materials");
        EnsureFolder(NoKimQuyFolder, "Prefabs");

        AssetDatabase.Refresh();
        ConfigureAlbedoImporter();
        ConfigureIconImporter();

        Material material = GetOrCreateCrossbowMaterial();
        Mesh mesh = GetOrCreateHandheldMesh();
        GameObject prefab = SaveCrossbowPrefab(mesh, material);

        GetOrCreateSwordBladeMaterial();
        GetOrCreateSwordHiltMaterial();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefab;
    }

    public static Material GetOrCreateSwordBladeMaterial()
    {
        return GetOrCreateSimpleMaterial(
            SwordBladeMaterialPath,
            "VanAn_Sword_Blade",
            new Color(0.75f, 0.84f, 0.9f, 1f),
            new Color(0.38f, 0.6f, 0.85f, 1f),
            0.05f,
            0.55f);
    }

    public static Material GetOrCreateSwordHiltMaterial()
    {
        return GetOrCreateSimpleMaterial(
            SwordHiltMaterialPath,
            "VanAn_Sword_Hilt",
            new Color(0.93f, 0.57f, 0.16f, 1f),
            new Color(0.8f, 0.38f, 0.06f, 1f),
            0.5f,
            0.42f);
    }

    private static Material GetOrCreateCrossbowMaterial()
    {
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
        if (albedo == null)
            Debug.LogWarning("NoKimQuyWeaponAssetBuilder: missing albedo texture at " + AlbedoPath);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = "NoKimQuy_Transparent"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        material.color = Color.white;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (albedo != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", albedo);
        }

        ConfigureTransparentMaterial(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh GetOrCreateHandheldMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        bool isNew = mesh == null;
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "NoKimQuy_HandheldMesh"
            };
        }
        else
        {
            mesh.Clear();
        }

        const float width = 0.42f;
        const float height = 0.28f;
        const float pivotXFromLeft = 0.48f;
        const float pivotYFromBottom = 0.38f;

        float left = -width * pivotXFromLeft;
        float right = width * (1f - pivotXFromLeft);
        float bottom = -height * pivotYFromBottom;
        float top = height * (1f - pivotYFromBottom);

        mesh.vertices = new[]
        {
            new Vector3(left, bottom, 0f),
            new Vector3(right, bottom, 0f),
            new Vector3(left, top, 0f),
            new Vector3(right, top, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        mesh.tangents = new[]
        {
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(1f, 0f, 0f, 1f)
        };
        mesh.RecalculateBounds();

        if (isNew)
            AssetDatabase.CreateAsset(mesh, MeshPath);

        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static GameObject SaveCrossbowPrefab(Mesh mesh, Material material)
    {
        GameObject root = new GameObject("NoKimQuy_Handheld");
        GameObject plane = new GameObject("NoKimQuy_ImagePlane");
        plane.transform.SetParent(root.transform, false);

        MeshFilter meshFilter = plane.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer = plane.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Material GetOrCreateSimpleMaterial(
        string path,
        string name,
        Color baseColor,
        Color emission,
        float metallic,
        float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = name
            };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        material.color = baseColor;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", baseColor);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", emission);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);

        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureAlbedoImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(AlbedoPath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            changed = true;
        }
        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (!importer.sRGBTexture)
        {
            importer.sRGBTexture = true;
            changed = true;
        }
        if (!importer.mipmapEnabled)
        {
            importer.mipmapEnabled = true;
            changed = true;
        }
        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }
        if (importer.filterMode != FilterMode.Trilinear)
        {
            importer.filterMode = FilterMode.Trilinear;
            changed = true;
        }
        if (importer.npotScale != TextureImporterNPOTScale.None)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            changed = true;
        }
        if (importer.maxTextureSize != 2048)
        {
            importer.maxTextureSize = 2048;
            changed = true;
        }
        if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static void ConfigureIconImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (!importer.sRGBTexture)
        {
            importer.sRGBTexture = true;
            changed = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }
        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }
        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            changed = true;
        }
        if (importer.maxTextureSize != 1024)
        {
            importer.maxTextureSize = 1024;
            changed = true;
        }
        if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
            return;

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.enableInstancing = true;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)CullMode.Off);
        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
