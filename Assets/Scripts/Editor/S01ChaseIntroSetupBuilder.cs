using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class S01ChaseIntroSetupBuilder
{
    public static void ConfigureChaseIntroCutscene()
    {
        GameObject trigger = FindSceneObject("WarningTrigger_ChaseStart");
        if (trigger == null)
        {
            Debug.LogWarning("S01ChaseIntroSetupBuilder: WarningTrigger_ChaseStart not found.");
            return;
        }

        S01ChaseIntroCutscene cutscene = trigger.GetComponent<S01ChaseIntroCutscene>();
        if (cutscene == null)
            cutscene = trigger.AddComponent<S01ChaseIntroCutscene>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Camera mainCamera = Camera.main;

        cutscene.player = player != null ? player.transform : null;
        cutscene.mainCamera = mainCamera;
        cutscene.thirdPersonCamera = mainCamera != null ? mainCamera.GetComponent<ThirdPersonCamera>() : null;
        cutscene.warningUI = Object.FindAnyObjectByType<S01WarningTextUI>();
        cutscene.subtitle = "Nhìn phía sau! Hắc Tinh đang rượt tới. Chạy ngay!";
        cutscene.cutsceneDuration = 3.1f;
        cutscene.cameraMoveDuration = 1.15f;
        cutscene.cameraDistanceFromPlayer = 4f;
        cutscene.cameraHeight = 2.5f;
        cutscene.lookAtHeight = 1.1f;
        cutscene.spawnBehindDistance = 14f;
        cutscene.spawnSideSpacing = 2f;
        cutscene.introStopBehindDistance = 6f;
        cutscene.introApproachSpeed = 4.5f;
        cutscene.postCutsceneAttackGrace = 1.4f;

        EditorUtility.SetDirty(cutscene);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("S01ChaseIntroSetupBuilder: configured chase intro cutscene on WarningTrigger_ChaseStart.");
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
