using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class S01TransitionSetupBuilder
{
    public static void ConfigureCollapseTransition()
    {
        SceneTransitionTrigger[] triggers = Object.FindObjectsByType<SceneTransitionTrigger>(FindObjectsInactive.Include);
        foreach (SceneTransitionTrigger trigger in triggers)
        {
            if (trigger == null)
                continue;

            trigger.delayBeforeLoad = 0.1f;
            trigger.waitForGroundCollapseSound = true;
            trigger.maxGroundCollapseWaitTime = 1.2f;
            trigger.postSoundLoadPadding = 0f;
            EditorUtility.SetDirty(trigger);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("S01TransitionSetupBuilder: configured " + triggers.Length + " scene transition trigger(s) to load after collapse sound.");
    }
}
