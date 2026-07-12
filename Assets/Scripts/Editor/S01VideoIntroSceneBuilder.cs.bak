using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class S01VideoIntroSceneBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string S01ScenePath = "Assets/Scenes/S01.unity";
    private const string S03ScenePath = "Assets/Scenes/S03.unity";
    private const string IntroVideoPath = "Assets/Models/video/0710.mp4";
    private const string IntroVideoRenderTexturePath = "Assets/RenderTextures/IntroVideoRT.renderTexture";

    [MenuItem("Tools/Dong Chay Anh Hung/Build S01 Video Intro Flow")]
    public static void BuildS01VideoIntroFlow()
    {
        VideoClip introClip = LoadRequiredAsset<VideoClip>(IntroVideoPath);
        RenderTexture introVideoRenderTexture = LoadRequiredAsset<RenderTexture>(IntroVideoRenderTexturePath);

        UpdateMainMenuStartScene();
        CreateS01Scene(introClip, introVideoRenderTexture);
        EnsureBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("S01 video intro flow built: MainMenu -> S01 -> S03.");
    }

    private static void UpdateMainMenuStartScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        MainMenuController controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        if (controller == null)
            throw new UnityException("MainMenuController was not found in " + MainMenuScenePath + ".");

        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty startSceneName = serializedController.FindProperty("startSceneName");
        if (startSceneName == null)
            throw new UnityException("MainMenuController.startSceneName serialized field was not found.");

        startSceneName.stringValue = "S01";
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CreateS01Scene(VideoClip introClip, RenderTexture introVideoRenderTexture)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject videoImageObject = new GameObject("VideoRawImage");
        videoImageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform videoRect = videoImageObject.AddComponent<RectTransform>();
        SetFullScreen(videoRect);

        RawImage rawImage = videoImageObject.AddComponent<RawImage>();
        rawImage.texture = introVideoRenderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;

        GameObject fadeObject = new GameObject("FadeOverlay");
        fadeObject.transform.SetParent(canvasObject.transform, false);
        RectTransform fadeRect = fadeObject.AddComponent<RectTransform>();
        SetFullScreen(fadeRect);

        Image fadeImage = fadeObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        CanvasGroup fadeGroup = fadeObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 1f;
        fadeGroup.interactable = false;
        fadeGroup.blocksRaycasts = false;

        VideoPlayer player = CreateVideoPlayer("VideoPlayer_1", introClip, introVideoRenderTexture);

        GameObject controllerObject = new GameObject("S01VideoSequenceController");
        S01VideoSequenceController controller = controllerObject.AddComponent<S01VideoSequenceController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("videoPlayer").objectReferenceValue = player;
        serializedController.FindProperty("introClip").objectReferenceValue = introClip;
        serializedController.FindProperty("videoDisplay").objectReferenceValue = rawImage;
        serializedController.FindProperty("fadeOverlay").objectReferenceValue = fadeGroup;
        serializedController.FindProperty("targetTexture").objectReferenceValue = introVideoRenderTexture;
        serializedController.FindProperty("nextSceneName").stringValue = "S03";
        serializedController.FindProperty("fadeDuration").floatValue = 0.5f;
        serializedController.FindProperty("allowSkip").boolValue = true;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, S01ScenePath);
    }

    private static VideoPlayer CreateVideoPlayer(string name, VideoClip clip, RenderTexture introVideoRenderTexture)
    {
        GameObject playerObject = new GameObject(name);

        VideoPlayer player = playerObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.skipOnDrop = true;
        player.isLooping = false;
        player.source = VideoSource.VideoClip;
        player.clip = clip;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = introVideoRenderTexture;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.controlledAudioTrackCount = 1;
        player.EnableAudioTrack(0, true);
        player.SetDirectAudioMute(0, false);
        player.SetDirectAudioVolume(0, 1f);
        return player;
    }

    private static void EnsureBuildSettings()
    {
        string[] requiredFirst = { MainMenuScenePath, S01ScenePath, S03ScenePath };
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        foreach (string path in requiredFirst)
            scenes.Add(new EditorBuildSettingsScene(path, true));

        HashSet<string> alreadyAdded = new HashSet<string>(requiredFirst);
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (alreadyAdded.Contains(scene.path) || IsBackupScene(scene.path))
                continue;

            scenes.Add(scene);
            alreadyAdded.Add(scene.path);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static bool IsBackupScene(string path)
    {
        string sceneName = Path.GetFileNameWithoutExtension(path);
        return sceneName != null && sceneName.Contains("_before_");
    }

    private static void SetFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new UnityException("Required asset was not found: " + path);
        return asset;
    }
}
