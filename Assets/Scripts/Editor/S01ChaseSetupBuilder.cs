using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class S01ChaseSetupBuilder
{
    private const string ThreatName = "S01_ChaseThreat";
    private const string WaypointsName = "S01_ChaseWaypoints";

    public static void CreateS01ChaseThreat()
    {
        DeleteIfExists(ThreatName);
        DeleteIfExists(WaypointsName);

        Vector3[] waypointPositions = S01CityEscapeBuilder.GetChaseWaypointPositions();
        Transform chaseParent = FindOrCreateChaseParent();

        Material threatMaterial = CreateMaterial("S01_ChaseThreat_PurpleBlack", new Color32(45, 16, 70, 255));
        Material blackMaterial = CreateMaterial("S01_ChaseThreat_Black", new Color32(5, 5, 8, 255));

        GameObject threat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(threat, "Create S01 Chase Threat");
        threat.name = ThreatName;
        threat.transform.SetParent(chaseParent, true);
        threat.transform.position = GetThreatStartPosition(waypointPositions);
        threat.transform.localScale = new Vector3(0.95f, 1.2f, 0.95f);

        Renderer threatRenderer = threat.GetComponent<Renderer>();
        if (threatRenderer != null)
            threatRenderer.sharedMaterial = threatMaterial;

        CreateThreatAccent(threat, "Black_Core", new Vector3(0f, 0.15f, 0.18f), new Vector3(0.82f, 1.3f, 0.18f), blackMaterial);
        CreateThreatAccent(threat, "Black_ShoulderBand", new Vector3(0f, 0.62f, 0f), new Vector3(1.12f, 0.16f, 1.12f), blackMaterial);

        GameObject waypointsRoot = new GameObject(WaypointsName);
        Undo.RegisterCreatedObjectUndo(waypointsRoot, "Create S01 Chase Waypoints");
        waypointsRoot.transform.SetParent(chaseParent, false);

        Transform[] waypoints = new Transform[waypointPositions.Length];

        for (int i = 0; i < waypointPositions.Length; i++)
        {
            GameObject waypoint = new GameObject("ChaseWaypoint_" + (i + 1).ToString("00"));
            Undo.RegisterCreatedObjectUndo(waypoint, "Create S01 Chase Waypoint");
            waypoint.transform.SetParent(waypointsRoot.transform);
            waypoint.transform.position = waypointPositions[i];

            waypoints[i] = waypoint.transform;
        }

        S01ChaseThreat chaseThreat = threat.AddComponent<S01ChaseThreat>();
        chaseThreat.waypoints = waypoints;
        chaseThreat.player = FindPlayer();
        chaseThreat.startDelay = 0f;
        chaseThreat.directChaseSpeed = 6f;
        chaseThreat.waypointSpeed = 5.2f;
        chaseThreat.moveSpeed = 5.2f;
        chaseThreat.catchUpSpeed = 7.5f;
        chaseThreat.catchDistance = 1.6f;
        chaseThreat.waypointReachDistance = 0.8f;
        chaseThreat.farFromPlayerDistance = 22f;
        chaseThreat.movementStartDistance = 1.2f;
        chaseThreat.hideUntilChaseStarts = true;
        chaseThreat.nearPlayerDistance = 6f;
        chaseThreat.farPlayerDistance = 22f;
        chaseThreat.veryFarPlayerDistance = 35f;
        chaseThreat.farSpeedMultiplier = 1.55f;
        chaseThreat.veryFarSpeedMultiplier = 2f;
        chaseThreat.speedMultiplierChangeRate = 1.8f;
        chaseThreat.catchDamage = 20;
        chaseThreat.catchAttackCooldown = 0.85f;
        chaseThreat.debugLogs = true;

        Selection.activeGameObject = threat;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Created S01 chase threat and waypoint route.");
    }

    private static Transform FindOrCreateChaseParent()
    {
        GameObject root = FindSceneObject("S01_CityEscape_Generated");
        if (root == null)
            root = new GameObject("S01_CityEscape_Generated");

        Transform chaseParent = root.transform.Find("Chase_Lane_Triggers");
        if (chaseParent != null)
            return chaseParent;

        GameObject parentObject = new GameObject("Chase_Lane_Triggers");
        parentObject.transform.SetParent(root.transform, false);
        return parentObject.transform;
    }

    private static Transform FindPlayer()
    {
        GameObject player = GameObject.Find("Player");

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        return player != null ? player.transform : null;
    }

    private static Vector3 GetThreatStartPosition(Vector3[] fallbackWaypoints)
    {
        GameObject spawnPoint = GameObject.Find("MinionSpawn_ChaseStart");

        if (spawnPoint != null)
            return spawnPoint.transform.position;

        if (fallbackWaypoints != null && fallbackWaypoints.Length > 0)
            return fallbackWaypoints[0];

        return new Vector3(0f, 1f, -8f);
    }

    private static void CreateThreatAccent(GameObject parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject accent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(accent, "Create S01 Chase Threat Accent");
        accent.name = name;
        accent.transform.SetParent(parent.transform);
        accent.transform.localPosition = localPosition;
        accent.transform.localRotation = Quaternion.identity;
        accent.transform.localScale = scale;

        Collider collider = accent.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        Renderer renderer = accent.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = name;
        material.color = color;

        return material;
    }

    private static void DeleteIfExists(string objectName)
    {
        GameObject existingObject = FindSceneObject(objectName);

        if (existingObject != null)
            Object.DestroyImmediate(existingObject);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform sceneTransform in transforms)
        {
            if (sceneTransform.name == objectName && sceneTransform.gameObject.scene.IsValid())
                return sceneTransform.gameObject;
        }

        return null;
    }
}

