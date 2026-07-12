using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class S01SoundscapeSetupBuilder
{
    private const string SoundscapeName = "S01_Soundscape";
    private const string SoundRoot = "Assets/Sound/S01/";

    public static void ConfigureS01Soundscape()
    {
        GameObject soundscapeObject = GameObject.Find(SoundscapeName);
        if (soundscapeObject == null)
            soundscapeObject = new GameObject(SoundscapeName);

        S01Soundscape soundscape = soundscapeObject.GetComponent<S01Soundscape>();
        if (soundscape == null)
            soundscape = soundscapeObject.AddComponent<S01Soundscape>();

        soundscape.cityAmbience = LoadClip("City ambience.mp3");
        soundscape.chaseHeartbeatLoop = LoadClip("Chase Heartbeat Loop.mp3");
        soundscape.slowMotionWhoosh = LoadClip("Slow Motion Whoosh.mp3");
        soundscape.phoneGlitchSignalLoss = LoadClip("Phone glitch_signal loss.mp3");
        soundscape.mudStep = LoadClip("Mud step.mp3");
        soundscape.impactHit = LoadClip("Impact Hit.mp3");
        soundscape.groundCollapse = LoadClip("Ground Collapse.mp3");
        soundscape.debrisPushQte = LoadClip("debris push QTE.mp3");
        soundscape.darkStarRoar = LoadClip("Dark Star Roar.mp3");
        soundscape.ambienceVolume = 0.28f;
        soundscape.heartbeatVolume = 0.42f;
        soundscape.oneShotVolume = 0.78f;

        S01AmbushDodgeQTE[] ambushes = Object.FindObjectsByType<S01AmbushDodgeQTE>(FindObjectsInactive.Include);
        foreach (S01AmbushDodgeQTE ambush in ambushes)
        {
            ambush.attackFeedbackPause = 0.85f;
            EditorUtility.SetDirty(ambush);
        }

        EditorUtility.SetDirty(soundscape);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("S01SoundscapeSetupBuilder: configured S01 soundscape and updated " + ambushes.Length + " ambush trigger(s).");
    }

    private static AudioClip LoadClip(string fileName)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundRoot + fileName);
        if (clip == null)
            Debug.LogWarning("S01SoundscapeSetupBuilder: missing audio clip " + SoundRoot + fileName);

        return clip;
    }
}
