using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class S01MinionRuntimeSetupBuilder
{
    public static void ConfigureS01MinionRuntime()
    {
        SetObjectActive("MinionSpawner", true);
        SetObjectActive("MinionSpawnPoint_01", true);
        SetObjectActive("MinionSpawnPoint_02", true);
        SetObjectActive("MinionSpawnPoint_03", true);
        SetObjectPosition("MinionSpawnPoint_01", new Vector3(-3f, 1f, -14f));
        SetObjectPosition("MinionSpawnPoint_02", new Vector3(0f, 1f, -17f));
        SetObjectPosition("MinionSpawnPoint_03", new Vector3(3f, 1f, -14f));

        MinionSpawner3D spawner = Object.FindAnyObjectByType<MinionSpawner3D>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            spawner.maxEnemies = 3;
            spawner.initialSpawnCount = 3;
            spawner.spawnInterval = 4f;
            spawner.spawnOnStart = false;
            spawner.enabled = true;

            if (spawner.minionPrefab == null)
                spawner.minionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minion.prefab");

            if (spawner.player == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    spawner.player = player.transform;
            }

            EditorUtility.SetDirty(spawner);
        }

        MinionDeathNotifier[] notifiers = Object.FindObjectsByType<MinionDeathNotifier>(FindObjectsInactive.Include);
        foreach (MinionDeathNotifier notifier in notifiers)
        {
            notifier.notifyOnlyWhenKilled = true;
            EditorUtility.SetDirty(notifier);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("S01MinionRuntimeSetupBuilder: configured S01 Minion runtime spawner for 3 base minions plus 2 ambush minions.");
    }

    private static void SetObjectActive(string objectName, bool active)
    {
        GameObject obj = FindSceneObject(objectName);
        if (obj == null)
            return;

        obj.SetActive(active);
        EditorUtility.SetDirty(obj);
    }

    private static void SetObjectPosition(string objectName, Vector3 position)
    {
        GameObject obj = FindSceneObject(objectName);
        if (obj == null)
            return;

        obj.transform.position = position;
        EditorUtility.SetDirty(obj.transform);
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
