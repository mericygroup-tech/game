using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private const string ImmediatePreviewPath = "Temp/MainMenuSettingsPreview.png";

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

    public static void CaptureNow()
    {
        if (!EditorApplication.isPlaying)
            throw new UnityException("Main Menu preview capture requires Play Mode.");

        Directory.CreateDirectory("Temp");
        ScreenCapture.CaptureScreenshot(ImmediatePreviewPath, 1);
        Debug.Log("[Main Menu] Immediate preview capture requested at " + Path.GetFullPath(ImmediatePreviewPath));
    }

    public static void OpenSettingsViaButton()
    {
        if (!EditorApplication.isPlaying)
            throw new UnityException("Main Menu settings QA requires Play Mode.");

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || buttons[i].name != "MainMenu_SettingsButton")
                continue;

            PointerEventData pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(buttons[i].gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(buttons[i].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            return;
        }

        throw new UnityException("Main Menu settings QA failed: settings button was not found.");
    }

    public static void HoverStartButtonForPreview()
    {
        if (!EditorApplication.isPlaying)
            throw new UnityException("Main Menu hover preview requires Play Mode.");
        if (EventSystem.current == null)
            throw new UnityException("Main Menu hover preview failed: EventSystem was not found.");

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || buttons[i].name != "MainMenu_StartButton")
                continue;

            PointerEventData pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(buttons[i].gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            Debug.Log("[Main Menu] Start button hover visual activated for preview.");
            return;
        }

        throw new UnityException("Main Menu hover preview failed: start button was not found.");
    }

    public static void ValidateSettingsButtonRaycast()
    {
        if (!EditorApplication.isPlaying)
            throw new UnityException("Main Menu raycast QA requires Play Mode.");
        if (EventSystem.current == null)
            throw new UnityException("Main Menu raycast QA failed: EventSystem was not found.");

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        Button settingsButton = null;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == "MainMenu_SettingsButton")
            {
                settingsButton = buttons[i];
                break;
            }
        }

        if (settingsButton == null)
            throw new UnityException("Main Menu raycast QA failed: settings button was not found.");

        Canvas.ForceUpdateCanvases();
        RectTransform rect = settingsButton.GetComponent<RectTransform>();
        Vector3 worldCenter = rect.TransformPoint(rect.rect.center);
        PointerEventData pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, worldCenter)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject == settingsButton.gameObject)
            {
                Debug.Log("[Main Menu] GraphicRaycaster reached the settings button successfully.");
                return;
            }
        }

        throw new UnityException("Main Menu raycast QA failed: settings button is blocked at screen point " + pointer.position + ".");
    }

    public static void ValidateAllMenuButtonRaycasts()
    {
        if (!EditorApplication.isPlaying)
            throw new UnityException("Main Menu raycast QA requires Play Mode.");
        if (EventSystem.current == null)
            throw new UnityException("Main Menu raycast QA failed: EventSystem was not found.");

        string[] requiredNames =
        {
            "MainMenu_StartButton",
            "MainMenu_SettingsButton",
            "MainMenu_AchievementsButton",
            "MainMenu_ExitButton"
        };

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        Canvas.ForceUpdateCanvases();
        for (int nameIndex = 0; nameIndex < requiredNames.Length; nameIndex++)
        {
            Button target = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == requiredNames[nameIndex])
                {
                    target = buttons[i];
                    break;
                }
            }

            if (target == null)
                throw new UnityException("Main Menu raycast QA failed: missing " + requiredNames[nameIndex] + ".");

            RectTransform rect = target.GetComponent<RectTransform>();
            PointerEventData pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            bool reached = false;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].gameObject == target.gameObject)
                {
                    reached = true;
                    break;
                }
            }

            if (!reached)
            {
                System.Text.StringBuilder diagnostics = new System.Text.StringBuilder();
                for (int i = 0; i < results.Count; i++)
                {
                    if (i > 0)
                        diagnostics.Append(", ");
                    diagnostics.Append(results[i].gameObject != null ? results[i].gameObject.name : "<null>");
                }

                Image hitImage = target.GetComponent<Image>();
                CanvasGroup[] groups = target.GetComponentsInParent<CanvasGroup>(true);
                System.Text.StringBuilder groupState = new System.Text.StringBuilder();
                for (int i = 0; i < groups.Length; i++)
                {
                    groupState.Append(groups[i].name)
                        .Append("[a=").Append(groups[i].alpha)
                        .Append(",ray=").Append(groups[i].blocksRaycasts)
                        .Append("] ");
                }

                throw new UnityException(
                    "Main Menu raycast QA failed: " + requiredNames[nameIndex] +
                    " is blocked at " + pointer.position +
                    ". Results: " + (results.Count > 0 ? diagnostics.ToString() : "<none>") +
                    ". Graphic alpha=" + (hitImage != null ? hitImage.color.a : -1f) +
                    ", culled=" + (hitImage != null && hitImage.canvasRenderer.cull) +
                    ", groups=" + groupState);
            }
        }

        Debug.Log("[Main Menu] GraphicRaycaster reached all four menu buttons successfully.");
    }

    public static void ValidateVolumeSlider()
    {
        if (!EditorApplication.isPlaying)
            throw new UnityException("Main Menu volume QA requires Play Mode.");

        Slider[] sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include);
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null || slider.name != "MainMenu_MasterVolumeSlider")
                continue;

            if (slider.GetComponent<AudioSliderFeedback>() == null || slider.GetComponent<MainMenuVolumeControl>() == null)
                throw new UnityException("Main Menu volume QA failed: slider feedback components are missing.");

            float originalVolume = GameAudio.GetVolume(GameAudioBus.Master);
            slider.value = 0.63f;
            if (!Mathf.Approximately(GameAudio.GetVolume(GameAudioBus.Master), 0.63f))
                throw new UnityException("Main Menu volume QA failed: Master bus did not follow the slider.");

            slider.value = originalVolume;
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Debug.Log("[Main Menu] Master volume slider validation passed.");
            return;
        }

        throw new UnityException("Main Menu volume QA failed: Master volume slider was not found.");
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
