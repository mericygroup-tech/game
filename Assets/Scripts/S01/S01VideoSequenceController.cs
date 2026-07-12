using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class S01VideoSequenceController : MonoBehaviour
{
    [Header("Video References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip introClip;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private RenderTexture targetTexture;

    [Header("Separate Audio")]
    [SerializeField] private AudioSource introAudioSource;
    [SerializeField] private AudioClip introAudioClip;

    [Header("Sequence Settings")]
    [SerializeField] private string nextSceneName = "S03";
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool allowSkip = true;

    private bool videoFinished;
    private bool loadingNextScene;
    private bool loopPointRegistered;
    private bool playbackStarted;

    private Coroutine playbackRoutine;
    private Coroutine prepareTimeoutRoutine;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        EnsureReferences();
        ConfigureFadeOverlay();
        ConfigureVideoDisplay();

        if (!ValidateReferences())
        {
            return;
        }

        ConfigureSeparateAudio();
        PrepareAndPlayVideo();
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
        UnregisterVideoCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterVideoCallbacks();
    }

    private void EnsureReferences()
    {
        if (videoPlayer == null)
        {
            GameObject playerObject =
                GameObject.Find("VideoPlayer_1");

            if (playerObject != null)
            {
                videoPlayer =
                    playerObject.GetComponent<VideoPlayer>();
            }
        }

        if (introAudioSource == null &&
            videoPlayer != null)
        {
            introAudioSource =
                videoPlayer.GetComponent<AudioSource>();
        }

        Canvas canvas =
            Object.FindAnyObjectByType<Canvas>();

        if (videoDisplay == null &&
            canvas != null)
        {
            Transform displayTransform =
                canvas.transform.Find("VideoRawImage");

            if (displayTransform != null)
            {
                videoDisplay =
                    displayTransform.GetComponent<RawImage>();
            }
        }

        if (fadeOverlay == null &&
            canvas != null)
        {
            Transform fadeTransform =
                canvas.transform.Find("FadeOverlay");

            if (fadeTransform != null)
            {
                fadeOverlay =
                    fadeTransform.GetComponent<CanvasGroup>();
            }
        }

        if (targetTexture == null &&
            videoDisplay != null)
        {
            targetTexture =
                videoDisplay.texture as RenderTexture;
        }

        if (targetTexture == null &&
            videoPlayer != null)
        {
            targetTexture =
                videoPlayer.targetTexture;
        }
    }

    private void ConfigureFadeOverlay()
    {
        if (fadeOverlay == null)
        {
            return;
        }

        fadeOverlay.alpha = 1f;
        fadeOverlay.interactable = false;
        fadeOverlay.blocksRaycasts = false;

        Image fadeImage =
            fadeOverlay.GetComponent<Image>();

        if (fadeImage != null)
        {
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
        }
    }

    private void ConfigureVideoDisplay()
    {
        if (videoDisplay == null ||
            targetTexture == null)
        {
            return;
        }

        videoDisplay.texture = targetTexture;
        videoDisplay.color = Color.white;
        videoDisplay.raycastTarget = false;
        videoDisplay.gameObject.SetActive(true);
    }

    private void ConfigureSeparateAudio()
    {
        if (videoPlayer == null ||
            introAudioSource == null)
        {
            return;
        }

        /*
         * VideoPlayer chỉ phát hình.
         * File WAV riêng được phát bằng AudioSource.
         */
        videoPlayer.audioOutputMode =
            VideoAudioOutputMode.None;

        introAudioSource.playOnAwake = false;
        introAudioSource.loop = false;
        introAudioSource.mute = false;
        introAudioSource.volume = 1f;
        introAudioSource.pitch = 1f;
        introAudioSource.spatialBlend = 0f;
        introAudioSource.clip = introAudioClip;

        if (introAudioClip != null &&
            introAudioClip.loadState ==
            AudioDataLoadState.Unloaded)
        {
            introAudioClip.LoadAudioData();
        }
    }

    private void PrepareAndPlayVideo()
    {
        videoFinished = false;
        playbackStarted = false;

        /*
         * Chỉ dừng trước khi bắt đầu Prepare.
         */
        StopMedia();

        videoPlayer.source =
            VideoSource.VideoClip;

        videoPlayer.clip = introClip;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;

        /*
         * Không bỏ frame khi bộ giải mã chậm
         * trong lúc chuyển từ MainMenu sang S01.
         */
        videoPlayer.skipOnDrop = false;

        /*
         * Video không phụ thuộc DSP Time hoặc Time.timeScale.
         */
        videoPlayer.timeUpdateMode =
            VideoTimeUpdateMode.UnscaledGameTime;

        videoPlayer.playbackSpeed = 1f;

        videoPlayer.renderMode =
            VideoRenderMode.RenderTexture;

        videoPlayer.targetTexture =
            targetTexture;

        videoPlayer.audioOutputMode =
            VideoAudioOutputMode.None;

        ConfigureSeparateAudio();
        RegisterVideoCallbacks();

        Debug.Log(
            "[S01 Video] Preparing video-only playback.",
            videoPlayer
        );

        videoPlayer.Prepare();

        prepareTimeoutRoutine =
            StartCoroutine(WatchPrepareTimeout());
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        /*
         * Ngăn callback Prepare chạy lại
         * và tạo thêm coroutine phát video.
         */
        if (source != videoPlayer ||
            loadingNextScene ||
            playbackStarted)
        {
            return;
        }

        playbackStarted = true;

        if (prepareTimeoutRoutine != null)
        {
            StopCoroutine(prepareTimeoutRoutine);
            prepareTimeoutRoutine = null;
        }

        source.audioOutputMode =
            VideoAudioOutputMode.None;

        source.skipOnDrop = false;

        source.timeUpdateMode =
            VideoTimeUpdateMode.UnscaledGameTime;

        source.playbackSpeed = 1f;

        ConfigureSeparateAudio();

        Debug.Log(
            $"[S01 Video] Prepared: {source.isPrepared}",
            source
        );

        Debug.Log(
            $"[S01 Video] Update mode: " +
            $"{source.timeUpdateMode}",
            source
        );

        Debug.Log(
            $"[S01 Video] Embedded audio output: " +
            $"{source.audioOutputMode}",
            source
        );

        Debug.Log(
            $"[S01 Video] Separate audio clip: " +
            $"{introAudioClip.name}",
            introAudioSource
        );

        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
        }

        playbackRoutine =
            StartCoroutine(PlayPreparedVideo());
    }

    private void OnVideoError(
        VideoPlayer source,
        string message)
    {
        Debug.LogError(
            $"[S01 Video] VideoPlayer error: {message}",
            source
        );
    }

    private IEnumerator WatchPrepareTimeout()
    {
        float timeoutAt =
            Time.realtimeSinceStartup + 15f;

        while (!videoPlayer.isPrepared &&
               Time.realtimeSinceStartup < timeoutAt &&
               !loadingNextScene)
        {
            yield return null;
        }

        prepareTimeoutRoutine = null;

        if (!videoPlayer.isPrepared &&
            !loadingNextScene)
        {
            Debug.LogError(
                "S01 intro video did not prepare in time.",
                this
            );
        }
    }

    private IEnumerator PlayPreparedVideo()
    {
        /*
         * Fade từ màn hình đen sang video.
         */
        yield return Fade(1f, 0f);

        if (loadingNextScene)
        {
            yield break;
        }

        /*
         * Chờ file WAV nạp hoàn tất.
         */
        while (introAudioClip.loadState ==
               AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (introAudioClip.loadState ==
            AudioDataLoadState.Failed)
        {
            Debug.LogError(
                "[S01 Video] Failed to load separate audio clip.",
                introAudioClip
            );

            yield break;
        }

        videoFinished = false;

        /*
         * Chỉ dừng WAV.
         * Không Stop VideoPlayer vì đã Prepare xong.
         */
        introAudioSource.Stop();
        introAudioSource.time = 0f;

        /*
         * Đảm bảo cấu hình không bị component khác đổi lại.
         */
        videoPlayer.skipOnDrop = false;

        videoPlayer.timeUpdateMode =
            VideoTimeUpdateMode.UnscaledGameTime;

        videoPlayer.playbackSpeed = 1f;

        /*
         * Không gán videoPlayer.time = 0
         * để tránh tạo lệnh Seek sau Prepare.
         */
        videoPlayer.Play();

        /*
         * Chờ một frame để Unity thực thi Play.
         */
        yield return null;

        if (loadingNextScene)
        {
            yield break;
        }

        introAudioSource.Play();

        Debug.Log(
            "[S01 Video] Video and WAV playback started.",
            this
        );

        /*
         * Chờ video phát hết.
         */
        while (!videoFinished &&
               !loadingNextScene)
        {
            yield return null;
        }

        if (loadingNextScene)
        {
            yield break;
        }

        StopMedia();

        yield return Fade(0f, 1f);

        LoadNextScene();
    }

    private void RegisterVideoCallbacks()
    {
        videoPlayer.prepareCompleted -=
            OnVideoPrepared;

        videoPlayer.prepareCompleted +=
            OnVideoPrepared;

        videoPlayer.errorReceived -=
            OnVideoError;

        videoPlayer.errorReceived +=
            OnVideoError;

        RegisterLoopPoint();
    }

    private void UnregisterVideoCallbacks()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -=
            OnVideoPrepared;

        videoPlayer.errorReceived -=
            OnVideoError;

        UnregisterLoopPoint();
    }

    private void RegisterLoopPoint()
    {
        if (videoPlayer == null ||
            loopPointRegistered)
        {
            return;
        }

        videoPlayer.loopPointReached +=
            HandleVideoFinished;

        loopPointRegistered = true;
    }

    private void UnregisterLoopPoint()
    {
        if (videoPlayer == null ||
            !loopPointRegistered)
        {
            return;
        }

        videoPlayer.loopPointReached -=
            HandleVideoFinished;

        loopPointRegistered = false;
    }

    private void HandleVideoFinished(
        VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        videoFinished = true;

        Debug.Log(
            "[S01 Video] Video playback finished.",
            source
        );
    }

    private IEnumerator Fade(
        float from,
        float to)
    {
        if (fadeOverlay == null)
        {
            yield break;
        }

        float duration =
            Mathf.Max(0.01f, fadeDuration);

        float elapsed = 0f;
        fadeOverlay.alpha = from;

        while (elapsed < duration &&
               !loadingNextScene)
        {
            elapsed += Time.unscaledDeltaTime;

            fadeOverlay.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(
                        elapsed / duration
                    )
                );

            yield return null;
        }

        if (!loadingNextScene)
        {
            fadeOverlay.alpha = to;
        }
    }

    private void StopMedia()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (introAudioSource != null)
        {
            introAudioSource.Stop();
        }
    }

    private void StopRunningCoroutines()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (prepareTimeoutRoutine != null)
        {
            StopCoroutine(prepareTimeoutRoutine);
            prepareTimeoutRoutine = null;
        }
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (videoPlayer == null)
        {
            Debug.LogError(
                "S01VideoSequenceController missing VideoPlayer.",
                this
            );

            valid = false;
        }

        if (introClip == null)
        {
            Debug.LogError(
                "S01VideoSequenceController missing Intro Video Clip.",
                this
            );

            valid = false;
        }

        if (introAudioSource == null)
        {
            Debug.LogError(
                "S01VideoSequenceController missing Intro Audio Source.",
                this
            );

            valid = false;
        }

        if (introAudioClip == null)
        {
            Debug.LogError(
                "S01VideoSequenceController missing Intro Audio Clip.",
                this
            );

            valid = false;
        }

        if (videoDisplay == null)
        {
            Debug.LogError(
                "S01VideoSequenceController missing Video Display.",
                this
            );

            valid = false;
        }

        if (targetTexture == null)
        {
            Debug.LogError(
                "S01VideoSequenceController missing Target Texture.",
                this
            );

            valid = false;
        }

        if (fadeOverlay == null)
        {
            Debug.LogError(
                "S01VideoSequenceController missing Fade Overlay.",
                this
            );

            valid = false;
        }

        return valid;
    }

    private void LoadNextScene()
    {
        if (loadingNextScene)
        {
            return;
        }

        loadingNextScene = true;
        Time.timeScale = 1f;

        StopRunningCoroutines();
        StopMedia();
        UnregisterVideoCallbacks();

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "S01VideoSequenceController has no next scene name.",
                this
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                nextSceneName))
        {
            Debug.LogError(
                $"Scene '{nextSceneName}' is not included " +
                "in Build Profiles.",
                this
            );

            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}