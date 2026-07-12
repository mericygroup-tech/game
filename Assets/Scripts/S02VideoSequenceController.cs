using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public sealed class S02VideoSequenceController : MonoBehaviour
{
    [Header("Video References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;

    [Header("Separate WAV Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    [Header("Sequence Settings")]
    [SerializeField] private string nextSceneName = "S03";
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float prepareTimeoutSeconds = 15f;

    private bool loadingNextScene;
    private bool playbackStarted;

    private Coroutine prepareTimeoutRoutine;
    private Coroutine startPlaybackRoutine;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        FindMissingReferences();

        if (!ValidateReferences())
        {
            return;
        }

        ConfigureVideoPlayer();
        ConfigureAudioSource();
        RegisterCallbacks();
        PrepareVideo();
    }

    private void Update()
    {
        if (!allowSkip || loadingNextScene)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return))
        {
            LoadNextScene();
        }
    }

    private void OnDisable()
    {
        StopRunningCoroutines();
        StopMedia();
        UnregisterCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();
    }

    private void FindMissingReferences()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (videoPlayer == null)
        {
            Debug.LogError(
                "[S02 Video] Missing VideoPlayer.",
                this
            );

            valid = false;
        }

        if (videoClip == null)
        {
            Debug.LogError(
                "[S02 Video] Missing Video Clip.",
                this
            );

            valid = false;
        }

        if (audioSource == null)
        {
            Debug.LogError(
                "[S02 Video] Missing AudioSource.",
                this
            );

            valid = false;
        }

        if (audioClip == null)
        {
            Debug.LogError(
                "[S02 Video] Missing WAV Audio Clip.",
                this
            );

            valid = false;
        }

        if (videoPlayer != null &&
            videoPlayer.targetTexture == null)
        {
            Debug.LogError(
                "[S02 Video] VideoPlayer has no Target Texture.",
                videoPlayer
            );

            valid = false;
        }

        return valid;
    }

    private void ConfigureVideoPlayer()
    {
        videoPlayer.Stop();

        videoPlayer.source =
            VideoSource.VideoClip;

        videoPlayer.clip =
            videoClip;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = false;
        videoPlayer.playbackSpeed = 1f;

        /*
         * MP4 chỉ phát hình.
         * WAV được phát bằng AudioSource riêng.
         */
        videoPlayer.audioOutputMode =
            VideoAudioOutputMode.None;

        /*
         * Video không bị ảnh hưởng nếu Time.timeScale thay đổi.
         */
        videoPlayer.timeUpdateMode =
            VideoTimeUpdateMode.UnscaledGameTime;
    }

    private void ConfigureAudioSource()
    {
        audioSource.Stop();

        audioSource.clip =
            audioClip;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.mute = false;
        audioSource.volume = 1f;
        audioSource.pitch = 1f;
        audioSource.spatialBlend = 0f;

        if (audioClip.loadState ==
            AudioDataLoadState.Unloaded)
        {
            audioClip.LoadAudioData();
        }
    }

    private void RegisterCallbacks()
    {
        UnregisterCallbacks();

        videoPlayer.prepareCompleted +=
            OnVideoPrepared;

        videoPlayer.started +=
            OnVideoStarted;

        videoPlayer.loopPointReached +=
            OnVideoFinished;

        videoPlayer.errorReceived +=
            OnVideoError;
    }

    private void UnregisterCallbacks()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -=
            OnVideoPrepared;

        videoPlayer.started -=
            OnVideoStarted;

        videoPlayer.loopPointReached -=
            OnVideoFinished;

        videoPlayer.errorReceived -=
            OnVideoError;
    }

    private void PrepareVideo()
    {
        playbackStarted = false;

        Debug.Log(
            "[S02 Video] Preparing video.",
            videoPlayer
        );

        videoPlayer.Prepare();

        prepareTimeoutRoutine =
            StartCoroutine(WatchPrepareTimeout());
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (source != videoPlayer ||
            loadingNextScene)
        {
            return;
        }

        if (prepareTimeoutRoutine != null)
        {
            StopCoroutine(prepareTimeoutRoutine);
            prepareTimeoutRoutine = null;
        }

        if (startPlaybackRoutine != null)
        {
            StopCoroutine(startPlaybackRoutine);
        }

        startPlaybackRoutine =
            StartCoroutine(StartPreparedPlayback());
    }

    private IEnumerator StartPreparedPlayback()
    {
        while (audioClip.loadState ==
               AudioDataLoadState.Loading &&
               !loadingNextScene)
        {
            yield return null;
        }

        if (loadingNextScene)
        {
            yield break;
        }

        if (audioClip.loadState ==
            AudioDataLoadState.Failed)
        {
            Debug.LogError(
                "[S02 Video] WAV failed to load.",
                audioClip
            );

            LoadNextScene();
            yield break;
        }

        audioSource.Stop();
        audioSource.time = 0f;

        /*
         * Audio chỉ bắt đầu trong callback started,
         * khi VideoPlayer thực sự bắt đầu chạy.
         */
        videoPlayer.Play();

        Debug.Log(
            "[S02 Video] Video Play requested.",
            videoPlayer
        );

        startPlaybackRoutine = null;
    }

    private void OnVideoStarted(VideoPlayer source)
    {
        if (source != videoPlayer ||
            loadingNextScene ||
            playbackStarted)
        {
            return;
        }

        playbackStarted = true;

        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.Play();

        Debug.Log(
            "[S02 Video] Video and WAV playback started.",
            this
        );
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (source != videoPlayer ||
            loadingNextScene)
        {
            return;
        }

        Debug.Log(
            "[S02 Video] Video playback finished.",
            source
        );

        LoadNextScene();
    }

    private void OnVideoError(
        VideoPlayer source,
        string message)
    {
        Debug.LogError(
            $"[S02 Video] VideoPlayer error: {message}",
            source
        );

        LoadNextScene();
    }

    private IEnumerator WatchPrepareTimeout()
    {
        float endTime =
            Time.realtimeSinceStartup +
            Mathf.Max(1f, prepareTimeoutSeconds);

        while (!videoPlayer.isPrepared &&
               Time.realtimeSinceStartup < endTime &&
               !loadingNextScene)
        {
            yield return null;
        }

        prepareTimeoutRoutine = null;

        if (!videoPlayer.isPrepared &&
            !loadingNextScene)
        {
            Debug.LogError(
                "[S02 Video] Video preparation timed out.",
                videoPlayer
            );

            LoadNextScene();
        }
    }

    private void StopMedia()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void StopRunningCoroutines()
    {
        if (prepareTimeoutRoutine != null)
        {
            StopCoroutine(prepareTimeoutRoutine);
            prepareTimeoutRoutine = null;
        }

        if (startPlaybackRoutine != null)
        {
            StopCoroutine(startPlaybackRoutine);
            startPlaybackRoutine = null;
        }
    }

    private void LoadNextScene()
    {
        if (loadingNextScene)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "[S02 Video] Next Scene Name is empty.",
                this
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                nextSceneName))
        {
            Debug.LogError(
                $"[S02 Video] Scene '{nextSceneName}' " +
                "is not included in Build Profiles.",
                this
            );

            return;
        }

        loadingNextScene = true;
        Time.timeScale = 1f;

        StopRunningCoroutines();
        StopMedia();
        UnregisterCallbacks();

        SceneManager.LoadScene(nextSceneName);
    }
}