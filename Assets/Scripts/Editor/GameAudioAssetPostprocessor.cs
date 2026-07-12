using System;
using UnityEditor;
using UnityEngine;

public sealed class GameAudioAssetPostprocessor : AssetPostprocessor
{
    private const string AudioRoot = "Assets/Resources/Audio/";

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(AudioRoot, StringComparison.OrdinalIgnoreCase))
            return;

        AudioImporter importer = (AudioImporter)assetImporter;
        bool isLongForm = assetPath.Contains("/Music/") || assetPath.Contains("/Ambience/");

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        settings.loadType = isLongForm ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = isLongForm ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        settings.quality = isLongForm ? 0.72f : 1f;
        settings.preloadAudioData = !isLongForm;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = isLongForm;
        importer.forceToMono = false;
    }
}

public static class GameAudioBuildValidator
{
    private static readonly string[] RequiredClipPaths =
    {
        "Assets/Resources/Audio/Music/MUSIC_MainTheme_VietnamHeroic.mp3",
        "Assets/Resources/Audio/Music/MUSIC_Intro_VietnamFlute.mp3",
        "Assets/Resources/Audio/Music/SFX_WarDrums_Loop.wav",
        "Assets/Resources/Audio/Music/MUSIC_Battle_Pursuit.wav",
        "Assets/Resources/Audio/Music/MUSIC_FinalBoss_Epic.wav",
        "Assets/Resources/Audio/Music/MUSIC_LastStand_Heroic.wav",
        "Assets/Resources/Audio/Music/MUSIC_WarLament.mp3",
        "Assets/Resources/Audio/Music/MUSIC_SadMemory_Vietnam.mp3",
        "Assets/Resources/Audio/Music/MUSIC_Victory.mp3",
        "Assets/Resources/Audio/UI/UI_Click.wav",
        "Assets/Resources/Audio/UI/UI_SliderTick.wav",
        "Assets/Resources/Audio/UI/UI_Win.wav",
        "Assets/Resources/Audio/UI/UI_Lose.wav",
        "Assets/Resources/Audio/SFX/SwordWhoosh_Fallback.mp3",
        "Assets/Resources/Audio/SFX/SwordImpact_Fallback.mp3",
        "Assets/Resources/Audio/Ambience/CaveAmbience.mp3",
        "Assets/Resources/Audio/Ambience/TimeRiftHum.mp3"
    };

    [MenuItem("Tools/Dong Chay Anh Hung/Audio/Validate Audio Setup")]
    public static void ValidateAudioSetup()
    {
        int missingCount = 0;
        for (int i = 0; i < RequiredClipPaths.Length; i++)
        {
            string path = RequiredClipPaths[i];
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) != null)
                continue;

            missingCount++;
            Debug.LogError("[GameAudio] Missing or invalid clip: " + path);
        }

        if (missingCount > 0)
            throw new InvalidOperationException("Game audio validation failed. Missing clips: " + missingCount);

        Debug.Log("[GameAudio] Validation passed: " + RequiredClipPaths.Length + " audio clips are imported and loadable.");
    }
}
