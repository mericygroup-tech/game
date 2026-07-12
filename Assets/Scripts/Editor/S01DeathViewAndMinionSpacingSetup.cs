using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;

public static class S01DeathViewAndMinionSpacingSetup
{
    private const string MinionPrefabPath = "Assets/Prefabs/Minion.prefab";
    private const float RouteBarrierHeight = 2.2f;
    private const float RouteBarrierThickness = 0.45f;
    private const float RouteBarrierEdgePadding = 0.25f;

    public static void ConfigureDeathViewAndMinionSpacing()
    {
        ConfigureMinionPrefab();
        ConfigureSceneObjects();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("S01DeathViewAndMinionSpacingSetup: configured minion spacing and death view UI.");
    }

    private static void ConfigureMinionPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath);
        if (prefab == null)
            return;

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            MinionChase3D chase = prefabContents.GetComponent<MinionChase3D>();
            if (chase != null)
            {
                chase.attackRange = 1.55f;
                chase.damage = 20;
                chase.separationRadius = 1.35f;
                chase.separationStrength = 0.45f;
                chase.personalSpaceDistance = 1.15f;
                chase.maxOverlapCorrection = 0.55f;
                chase.visualGroundOffset = 0.45f;
                EditorUtility.SetDirty(chase);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void ConfigureSceneObjects()
    {
        ThirdPersonCamera[] cameras = Object.FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        foreach (ThirdPersonCamera camera in cameras)
        {
            if (camera == null)
                continue;

            camera.deathPitch = 76f;
            camera.deathDistance = 6.4f;
            camera.deathHeight = 5.9f;
            camera.deathLookAtHeight = 0.45f;
            EditorUtility.SetDirty(camera);
        }

        MinionChase3D[] minions = Object.FindObjectsByType<MinionChase3D>(FindObjectsInactive.Include);
        foreach (MinionChase3D minion in minions)
        {
            if (minion == null)
                continue;

            minion.attackRange = 1.55f;
            minion.damage = 20;
            minion.separationRadius = 1.35f;
            minion.separationStrength = 0.45f;
            minion.personalSpaceDistance = 1.15f;
            minion.maxOverlapCorrection = 0.55f;
            minion.visualGroundOffset = 0.45f;
            minion.ForceSnapToGround();
            EditorUtility.SetDirty(minion);
        }

        S01ChaseThreat[] routeThreats = Object.FindObjectsByType<S01ChaseThreat>(FindObjectsInactive.Include);
        foreach (S01ChaseThreat threat in routeThreats)
        {
            if (threat == null)
                continue;

            threat.separationRadius = 1.5f;
            threat.separationStrength = 0.45f;
            threat.personalSpaceDistance = 1.35f;
            threat.catchDamage = 20;
            threat.catchAttackCooldown = 0.85f;
            EditorUtility.SetDirty(threat);
        }

        S01ChaseIntroCutscene[] intros = Object.FindObjectsByType<S01ChaseIntroCutscene>(FindObjectsInactive.Include);
        foreach (S01ChaseIntroCutscene intro in intros)
        {
            if (intro == null)
                continue;

            intro.spawnSideSpacing = 2f;
            EditorUtility.SetDirty(intro);
        }

        S01AmbushDodgeQTE[] ambushes = Object.FindObjectsByType<S01AmbushDodgeQTE>(FindObjectsInactive.Include);
        foreach (S01AmbushDodgeQTE ambush in ambushes)
        {
            if (ambush == null)
                continue;

            ambush.joinSideSpacing = 1.2f;
            ambush.failedDodgeDamage = 20;
            EditorUtility.SetDirty(ambush);
        }

        SceneTransitionTrigger[] transitions = Object.FindObjectsByType<SceneTransitionTrigger>(FindObjectsInactive.Include);
        foreach (SceneTransitionTrigger transition in transitions)
        {
            if (transition == null)
                continue;

            transition.delayBeforeLoad = 0.1f;
            transition.waitForGroundCollapseSound = true;
            transition.maxGroundCollapseWaitTime = 1.2f;
            transition.postSoundLoadPadding = 0f;
            EditorUtility.SetDirty(transition);
        }

        ConfigurePlayerDamageFeedback();
        ConfigureChaseRoadWidths();
        EnsureConstructionFenceSideBlockers();

        PlayerHealth3D playerHealth = Object.FindAnyObjectByType<PlayerHealth3D>(FindObjectsInactive.Include);
        if (playerHealth == null || playerHealth.gameOverUI == null)
            return;

        RectTransform root = playerHealth.gameOverUI.GetComponent<RectTransform>();
        if (root != null)
        {
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(0f, -80f);
            root.sizeDelta = new Vector2(420f, 92f);
            EditorUtility.SetDirty(root);
        }

        TMP_Text[] texts = playerHealth.gameOverUI.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            text.fontSize = 34f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 34f;
            text.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(text);
        }
    }

    private static void ConfigurePlayerDamageFeedback()
    {
        PlayerHealth3D[] healths = Object.FindObjectsByType<PlayerHealth3D>(FindObjectsInactive.Include);
        foreach (PlayerHealth3D health in healths)
        {
            if (health == null)
                continue;

            health.damageSlowDuration = 0.5f;
            health.damageSlowMultiplier = 0.45f;
            EditorUtility.SetDirty(health);
        }

        PlayerAnimatorDriver[] drivers = Object.FindObjectsByType<PlayerAnimatorDriver>(FindObjectsInactive.Include);
        FieldInfo hitDurationField = typeof(PlayerAnimatorDriver).GetField("hitDuration", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (PlayerAnimatorDriver driver in drivers)
        {
            if (driver == null)
                continue;

            hitDurationField?.SetValue(driver, 0.5f);
            EditorUtility.SetDirty(driver);
        }
    }

    private static void ConfigureChaseRoadWidths()
    {
        SetMinimumFloorWidth("MuseumStreet_Start_Floor", 18f);
        SetMinimumFloorWidth("ConstructionDetour_East_Floor", 14.5f);
        SetMinimumFloorWidth("ConstructionRun_North_Floor", 14.5f);
        SetMinimumFloorWidth("DebrisRun_West_Floor", 12.5f);
        SetMinimumFloorWidth("MudRun_North_Floor", 13.5f);
        SetMinimumFloorWidth("NarrowRun_West_Floor", 12.5f);
        SetMinimumFloorWidth("LongEscape_North_Floor", 12.5f);
        SetMinimumFloorWidth("LongEscape_East_Floor", 14.5f);
        SetMinimumFloorWidth("CollapseApproach_North_Floor", 13.5f);
        AlignRouteBarriersToFloor("MuseumStreet_Start", "MuseumStreet_Start_Floor");
        AlignRouteBarriersToFloor("ConstructionDetour_East", "ConstructionDetour_East_Floor");
        AlignRouteBarriersToFloor("ConstructionRun_North", "ConstructionRun_North_Floor");
        AlignRouteBarriersToFloor("DebrisRun_West", "DebrisRun_West_Floor");
        AlignRouteBarriersToFloor("MudRun_North", "MudRun_North_Floor");
        AlignRouteBarriersToFloor("NarrowRun_West", "NarrowRun_West_Floor");
        AlignRouteBarriersToFloor("LongEscape_North", "LongEscape_North_Floor");
        AlignRouteBarriersToFloor("LongEscape_East", "LongEscape_East_Floor");
        AlignRouteBarriersToFloor("CollapseApproach_North", "CollapseApproach_North_Floor");
    }

    private static void SetMinimumFloorWidth(string objectName, float minWidth)
    {
        GameObject floor = GameObject.Find(objectName);
        if (floor == null)
            return;

        Vector3 scale = floor.transform.localScale;
        bool changed = false;

        if (scale.x <= scale.z)
        {
            if (scale.x < minWidth)
            {
                scale.x = minWidth;
                changed = true;
            }
        }
        else if (scale.z < minWidth)
        {
            scale.z = minWidth;
            changed = true;
        }

        if (!changed)
            return;

        floor.transform.localScale = scale;
        EditorUtility.SetDirty(floor);
    }

    private static void AlignRouteBarriersToFloor(string segmentName, string floorName)
    {
        GameObject floor = GameObject.Find(floorName);
        if (floor == null)
            return;

        bool alongZ = floor.transform.localScale.z >= floor.transform.localScale.x;
        float width = alongZ ? floor.transform.localScale.x : floor.transform.localScale.z;
        float offset = width * 0.5f + RouteBarrierEdgePadding;
        Vector3 center = floor.transform.position;

        AlignRouteBarrier(GameObject.Find(segmentName + "_Barrier_A"), center, offset, alongZ, true, floor.transform.localScale);
        AlignRouteBarrier(GameObject.Find(segmentName + "_Barrier_B"), center, offset, alongZ, false, floor.transform.localScale);
    }

    private static void AlignRouteBarrier(GameObject barrier, Vector3 center, float offset, bool alongZ, bool positiveSide, Vector3 floorScale)
    {
        if (barrier == null)
            return;

        Vector3 position = barrier.transform.position;
        position.y = RouteBarrierHeight * 0.5f;
        if (alongZ)
        {
            position.x = center.x + (positiveSide ? offset : -offset);
            position.z = center.z;
            barrier.transform.localScale = new Vector3(RouteBarrierThickness, RouteBarrierHeight, Mathf.Max(barrier.transform.localScale.z, floorScale.z - 10f));
        }
        else
        {
            position.x = center.x;
            position.z = center.z + (positiveSide ? offset : -offset);
            barrier.transform.localScale = new Vector3(Mathf.Max(barrier.transform.localScale.x, floorScale.x - 10f), RouteBarrierHeight, RouteBarrierThickness);
        }

        barrier.transform.position = position;
        EditorUtility.SetDirty(barrier);
    }

    private static void EnsureConstructionFenceSideBlockers()
    {
        Transform parent = GetOrCreateRouteBarrierParent();
        Material material = ResolveBarrierMaterial();
        EnsureBlocker("ConstructionFence_LeftSideBlocker", parent, new Vector3(40.4f, 1.1f, 91f), new Vector3(5.2f, 2.2f, 1.2f), material);
        EnsureBlocker("ConstructionFence_RightSideBlocker", parent, new Vector3(49.6f, 1.1f, 91f), new Vector3(5.2f, 2.2f, 1.2f), material);
    }

    private static Transform GetOrCreateRouteBarrierParent()
    {
        GameObject parent = GameObject.Find("Route_Barriers");
        if (parent == null)
            parent = new GameObject("Route_Barriers");

        return parent.transform;
    }

    private static Material ResolveBarrierMaterial()
    {
        Renderer sample = null;
        GameObject sampleObject = GameObject.Find("ConstructionRun_North_Barrier_A");
        if (sampleObject != null)
            sample = sampleObject.GetComponent<Renderer>();

        return sample != null ? sample.sharedMaterial : null;
    }

    private static void EnsureBlocker(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject blocker = GameObject.Find(objectName);
        if (blocker == null)
        {
            blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = objectName;
            blocker.transform.SetParent(parent, true);
        }

        blocker.transform.position = position;
        blocker.transform.rotation = Quaternion.identity;
        blocker.transform.localScale = scale;

        BoxCollider collider = blocker.GetComponent<BoxCollider>();
        if (collider == null)
            collider = blocker.AddComponent<BoxCollider>();
        collider.isTrigger = false;

        Renderer renderer = blocker.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        EditorUtility.SetDirty(blocker);
    }
}
