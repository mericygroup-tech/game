using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Small editor-only visual QA utility. It waits for the cinematic reveal to
/// finish, captures the Game view, then exits Play Mode automatically.
/// </summary>
[InitializeOnLoad]
public static class MainMenuPreviewCapture
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PhaseKey = "DCAH.MainMenuPreview.Phase";
    private const string TargetFrameKey = "DCAH.MainMenuPreview.TargetFrame";
    private const string PreviewPath = "Temp/MainMenuPreview.png";

    static MainMenuPreviewCapture()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Dong Chay Anh Hung/Capture Main Menu Preview")]
    public static void CapturePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Main Menu] Stop Play Mode before starting a preview capture.");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Directory.CreateDirectory("Temp");
        if (File.Exists(PreviewPath))
            File.Delete(PreviewPath);

        SessionState.SetInt(PhaseKey, 1);
        SessionState.SetInt(TargetFrameKey, 0);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        int phase = SessionState.GetInt(PhaseKey, 0);
        if (phase == 0 || !EditorApplication.isPlaying)
            return;

        int targetFrame = SessionState.GetInt(TargetFrameKey, 0);
        if (targetFrame == 0)
        {
            // About four seconds at 60 fps: enough for the complete reveal.
            SessionState.SetInt(TargetFrameKey, Time.frameCount + 240);
            return;
        }

        if (phase == 1 && Time.frameCount >= targetFrame)
        {
            ScreenCapture.CaptureScreenshot(PreviewPath, 1);
            SessionState.SetInt(PhaseKey, 2);
            SessionState.SetInt(TargetFrameKey, Time.frameCount + 12);
            return;
        }

        if (phase == 2 && Time.frameCount >= targetFrame && File.Exists(PreviewPath))
        {
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetInt(TargetFrameKey, 0);
            Debug.Log("[Main Menu] Preview captured at " + Path.GetFullPath(PreviewPath));
            EditorApplication.isPlaying = false;
        }
    }
}
