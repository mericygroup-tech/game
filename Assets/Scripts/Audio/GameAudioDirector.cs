using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent audio coordinator for music, ambience and one-shot buses.
/// Music is cross-faded with unscaled time so transitions also work while UI
/// pauses gameplay between arena waves.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class GameAudioDirector : MonoBehaviour
{
    private const string ResourceRoot = "Audio/";
    private const string VolumeKeyPrefix = "GameAudio.Volume.";
    private const int SfxVoiceCount = 6;

    private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();
    private static readonly HashSet<string> MissingClipWarnings = new HashSet<string>();

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource ambienceSource;
    private AudioSource uiSource;
    private AudioSource uiHoverSource;
    private AudioSource blessingUiSource;
    private AudioSource[] sfxSources;

    private Coroutine musicFadeRoutine;
    private Coroutine ambienceFadeRoutine;
    private GameMusicState currentMusicState = GameMusicState.None;
    private GameAmbientState currentAmbientState = GameAmbientState.None;
    private float currentMusicGain = 1f;
    private float currentAmbientGain = 1f;
    private float currentBlessingUiGain = 1f;
    private float nextUiHoverTime;
    private float nextBlessingHoverTime;
    private int nextSfxVoice;

    private float masterVolume;
    private float musicVolume;
    private float ambienceVolume;
    private float sfxVolume;
    private float uiVolume;

    public static GameAudioDirector Instance { get; private set; }

    public GameMusicState CurrentMusicState => currentMusicState;
    public GameAmbientState CurrentAmbientState => currentAmbientState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject root = new GameObject("[GameAudio]");
        root.AddComponent<GameAudioDirector>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumes();
        CreateSources();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        RouteScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    public void PlayMusic(GameMusicState state, float fadeDuration = 0.75f)
    {
        MusicSpec spec = GetMusicSpec(state);
        AudioClip clip = LoadFirstAvailable(spec.Paths);

        if (state == currentMusicState && activeMusicSource != null && activeMusicSource.isPlaying)
            return;

        currentMusicState = state;
        currentMusicGain = spec.Gain;

        if (musicFadeRoutine != null)
            StopCoroutine(musicFadeRoutine);

        AudioSource outgoing = activeMusicSource;
        AudioSource incoming = outgoing == musicSourceA ? musicSourceB : musicSourceA;

        if (clip == null || state == GameMusicState.None)
        {
            activeMusicSource = null;
            musicFadeRoutine = StartCoroutine(FadeMusic(outgoing, null, Mathf.Max(0f, fadeDuration), 0f));
            return;
        }

        incoming.Stop();
        incoming.clip = clip;
        incoming.loop = spec.Loop;
        incoming.volume = 0f;
        incoming.Play();

        activeMusicSource = incoming;
        float targetVolume = GetBusGain(GameAudioBus.Music) * spec.Gain;
        musicFadeRoutine = StartCoroutine(FadeMusic(outgoing, incoming, Mathf.Max(0f, fadeDuration), targetVolume));
    }

    public void PlayAmbient(GameAmbientState state, float fadeDuration = 0.55f)
    {
        AmbientSpec spec = GetAmbientSpec(state);
        AudioClip clip = LoadFirstAvailable(spec.Paths);

        if (state == currentAmbientState && ambienceSource.isPlaying)
            return;

        currentAmbientState = state;
        currentAmbientGain = spec.Gain;

        if (ambienceFadeRoutine != null)
            StopCoroutine(ambienceFadeRoutine);

        ambienceFadeRoutine = StartCoroutine(ChangeAmbient(clip, Mathf.Max(0f, fadeDuration), spec.Gain));
    }

    public void PlaySwordSwing(bool heavy)
    {
        AudioClip clip = LoadClip(ResourceRoot + "SFX/SwordWhoosh_Fallback");
        float pitch = heavy ? Random.Range(0.78f, 0.88f) : Random.Range(1.02f, 1.16f);
        float gain = heavy ? 0.72f : 0.5f;
        PlaySfx(clip, gain, pitch);
    }

    public void PlaySwordImpact(bool heavy)
    {
        AudioClip clip = LoadClip(ResourceRoot + "SFX/SwordImpact_Fallback");
        float pitch = heavy ? Random.Range(0.78f, 0.9f) : Random.Range(0.96f, 1.08f);
        float gain = heavy ? 0.82f : 0.62f;
        PlaySfx(clip, gain, pitch);
    }

    public void PlayUiClick()
    {
        PlayUi(LoadClip(ResourceRoot + "UI/UI_Click"), 0.72f);
    }

    public void PlayUiHover()
    {
        if (Time.unscaledTime < nextUiHoverTime || uiHoverSource == null)
            return;

        AudioClip clip = LoadClip(ResourceRoot + "UI/UI_Click");
        if (clip == null)
            return;

        nextUiHoverTime = Time.unscaledTime + 0.07f;
        uiHoverSource.Stop();
        uiHoverSource.clip = clip;
        uiHoverSource.pitch = 1.16f;
        uiHoverSource.volume = GetBusGain(GameAudioBus.Ui) * 0.28f;
        uiHoverSource.Play();
    }

    public void PlaySliderTick()
    {
        PlayUi(LoadClip(ResourceRoot + "UI/UI_SliderTick"), 0.38f);
    }

    public void PlayBlessingHover()
    {
        if (Time.unscaledTime < nextBlessingHoverTime)
            return;

        nextBlessingHoverTime = Time.unscaledTime + 0.08f;
        PlayBlessingUi(LoadClip(ResourceRoot + "UI/UI_Blessing_Hover"), 0.34f);
    }

    public void PlayBlessingSelect()
    {
        PlayBlessingUi(LoadClip(ResourceRoot + "UI/UI_Blessing_Select"), 0.76f);
    }

    public void PlayBlessingSkip()
    {
        PlayBlessingUi(LoadClip(ResourceRoot + "UI/UI_Blessing_Skip"), 0.48f);
    }

    public void PlayBlessingReroll()
    {
        PlayBlessingUi(LoadClip(ResourceRoot + "UI/UI_Blessing_Reroll"), 0.68f);
    }

    public void PlayDefeat()
    {
        PlayUi(LoadClip(ResourceRoot + "UI/UI_Lose"), 0.82f);
        PlayMusic(GameMusicState.Defeat, 0.6f);
    }

    public void PlayVictory()
    {
        PlayUi(LoadClip(ResourceRoot + "UI/UI_Win"), 0.86f);
        PlayMusic(GameMusicState.Victory, 0.65f);
    }

    public float GetVolume(GameAudioBus bus)
    {
        switch (bus)
        {
            case GameAudioBus.Master: return masterVolume;
            case GameAudioBus.Music: return musicVolume;
            case GameAudioBus.Ambience: return ambienceVolume;
            case GameAudioBus.Sfx: return sfxVolume;
            case GameAudioBus.Ui: return uiVolume;
            default: return 1f;
        }
    }

    public void SetVolume(GameAudioBus bus, float normalizedVolume)
    {
        float value = Mathf.Clamp01(normalizedVolume);
        switch (bus)
        {
            case GameAudioBus.Master: masterVolume = value; break;
            case GameAudioBus.Music: musicVolume = value; break;
            case GameAudioBus.Ambience: ambienceVolume = value; break;
            case GameAudioBus.Sfx: sfxVolume = value; break;
            case GameAudioBus.Ui: uiVolume = value; break;
        }

        PlayerPrefs.SetFloat(VolumeKeyPrefix + bus, value);
        PlayerPrefs.Save();
        RefreshLoopVolumes();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RouteScene(scene);
        StartCoroutine(InstallUiFeedbackAfterSceneLoad());
    }

    private void RouteScene(Scene scene)
    {
        string sceneName = scene.name;

        if (sceneName == "MainMenu" || sceneName.StartsWith("MainMenu_"))
        {
            PlayAmbient(GameAmbientState.None, 0.3f);
            PlayMusic(GameMusicState.MainMenu, 0.75f);
            return;
        }

        if (sceneName == "S01" || sceneName.StartsWith("S01_before"))
        {
            // The authored intro video already contains its own soundtrack.
            // Keep game music silent so the two mixes never overlap.
            PlayAmbient(GameAmbientState.None, 0.3f);
            PlayMusic(GameMusicState.None, 0.25f);
            return;
        }

        if (sceneName == "S01_CityPrototype")
        {
            // S01Soundscape owns the city ambience and chase heartbeat.
            PlayMusic(GameMusicState.None, 0.45f);
            PlayAmbient(GameAmbientState.None, 0.3f);
            return;
        }

        if (sceneName == "S02_UndergroundCave")
        {
            PlayMusic(GameMusicState.None, 0.45f);
            PlayAmbient(GameAmbientState.Cave, 0.65f);
            return;
        }

        if (sceneName == "S02")
        {
            // This build scene is a video sequence with its own authored audio.
            PlayMusic(GameMusicState.None, 0.35f);
            PlayAmbient(GameAmbientState.None, 0.35f);
            return;
        }

        if (sceneName == "S03")
        {
            PlayAmbient(GameAmbientState.None, 0.45f);
            PlayMusic(GameMusicState.PreCombat, 0.65f);
            return;
        }

        PlayMusic(GameMusicState.None, 0.4f);
        PlayAmbient(GameAmbientState.None, 0.4f);
    }

    private IEnumerator InstallUiFeedbackAfterSceneLoad()
    {
        yield return null;
        AudioUiFeedbackInstaller.Install();
        yield return null;
        AudioUiFeedbackInstaller.Install();
    }

    private IEnumerator FadeMusic(AudioSource outgoing, AudioSource incoming, float duration, float targetVolume)
    {
        float outgoingStart = outgoing != null ? outgoing.volume : 0f;
        float incomingStart = incoming != null ? incoming.volume : 0f;

        if (duration <= 0.001f)
        {
            if (outgoing != null)
            {
                outgoing.Stop();
                outgoing.volume = 0f;
            }

            if (incoming != null)
                incoming.volume = targetVolume;

            musicFadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            if (outgoing != null)
                outgoing.volume = Mathf.Lerp(outgoingStart, 0f, eased);
            if (incoming != null)
                incoming.volume = Mathf.Lerp(incomingStart, targetVolume, eased);

            yield return null;
        }

        if (outgoing != null)
        {
            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0f;
        }

        if (incoming != null)
            incoming.volume = targetVolume;

        musicFadeRoutine = null;
    }

    private IEnumerator ChangeAmbient(AudioClip nextClip, float duration, float gain)
    {
        float startVolume = ambienceSource.volume;
        float halfDuration = duration * 0.5f;

        yield return FadeSource(ambienceSource, startVolume, 0f, halfDuration);
        ambienceSource.Stop();
        ambienceSource.clip = nextClip;

        if (nextClip != null)
        {
            ambienceSource.loop = true;
            ambienceSource.Play();
            yield return FadeSource(ambienceSource, 0f, GetBusGain(GameAudioBus.Ambience) * gain, halfDuration);
        }

        ambienceFadeRoutine = null;
    }

    private static IEnumerator FadeSource(AudioSource source, float from, float to, float duration)
    {
        if (source == null)
            yield break;

        if (duration <= 0.001f)
        {
            source.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.volume = to;
    }

    private void PlaySfx(AudioClip clip, float gain, float pitch)
    {
        if (clip == null || sfxSources == null || sfxSources.Length == 0)
            return;

        AudioSource source = sfxSources[nextSfxVoice];
        nextSfxVoice = (nextSfxVoice + 1) % sfxSources.Length;
        source.Stop();
        source.clip = clip;
        source.pitch = pitch;
        source.volume = GetBusGain(GameAudioBus.Sfx) * Mathf.Clamp01(gain);
        source.Play();
    }

    private void PlayUi(AudioClip clip, float gain)
    {
        if (clip == null || uiSource == null)
            return;

        uiSource.PlayOneShot(clip, GetBusGain(GameAudioBus.Ui) * Mathf.Clamp01(gain));
    }

    private void PlayBlessingUi(AudioClip clip, float gain)
    {
        if (clip == null || blessingUiSource == null)
            return;

        currentBlessingUiGain = Mathf.Clamp01(gain);
        blessingUiSource.Stop();
        blessingUiSource.clip = clip;
        blessingUiSource.volume = GetBusGain(GameAudioBus.Ui) * currentBlessingUiGain;
        blessingUiSource.Play();
    }

    private void CreateSources()
    {
        musicSourceA = CreateSource("Music_A");
        musicSourceB = CreateSource("Music_B");
        ambienceSource = CreateSource("Ambience");
        uiSource = CreateSource("UI");
        uiHoverSource = CreateSource("UI_Hover");
        blessingUiSource = CreateSource("UI_Blessing");

        musicSourceA.ignoreListenerPause = true;
        musicSourceB.ignoreListenerPause = true;
        ambienceSource.ignoreListenerPause = true;
        uiSource.ignoreListenerPause = true;
        uiHoverSource.ignoreListenerPause = true;
        blessingUiSource.ignoreListenerPause = true;

        sfxSources = new AudioSource[SfxVoiceCount];
        for (int i = 0; i < sfxSources.Length; i++)
            sfxSources[i] = CreateSource("SFX_" + (i + 1).ToString("00"));
    }

    private AudioSource CreateSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        return source;
    }

    private void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat(VolumeKeyPrefix + GameAudioBus.Master, 1f);
        musicVolume = PlayerPrefs.GetFloat(VolumeKeyPrefix + GameAudioBus.Music, 0.78f);
        ambienceVolume = PlayerPrefs.GetFloat(VolumeKeyPrefix + GameAudioBus.Ambience, 0.72f);
        sfxVolume = PlayerPrefs.GetFloat(VolumeKeyPrefix + GameAudioBus.Sfx, 0.9f);
        uiVolume = PlayerPrefs.GetFloat(VolumeKeyPrefix + GameAudioBus.Ui, 0.85f);
    }

    private float GetBusGain(GameAudioBus bus)
    {
        return masterVolume * GetVolume(bus);
    }

    private void RefreshLoopVolumes()
    {
        if (activeMusicSource != null && activeMusicSource.isPlaying)
            activeMusicSource.volume = GetBusGain(GameAudioBus.Music) * currentMusicGain;

        if (ambienceSource != null && ambienceSource.isPlaying)
            ambienceSource.volume = GetBusGain(GameAudioBus.Ambience) * currentAmbientGain;

        if (blessingUiSource != null && blessingUiSource.isPlaying)
            blessingUiSource.volume = GetBusGain(GameAudioBus.Ui) * currentBlessingUiGain;

        if (uiHoverSource != null && uiHoverSource.isPlaying)
            uiHoverSource.volume = GetBusGain(GameAudioBus.Ui) * 0.28f;
    }

    private static AudioClip LoadFirstAvailable(string[] resourcePaths)
    {
        if (resourcePaths == null)
            return null;

        for (int i = 0; i < resourcePaths.Length; i++)
        {
            AudioClip clip = LoadClip(resourcePaths[i]);
            if (clip != null)
                return clip;
        }

        return null;
    }

    private static AudioClip LoadClip(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;

        if (ClipCache.TryGetValue(resourcePath, out AudioClip cachedClip))
            return cachedClip;

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip != null)
        {
            ClipCache[resourcePath] = clip;
            return clip;
        }

        if (MissingClipWarnings.Add(resourcePath))
            Debug.LogWarning("[GameAudio] Missing Resources clip: " + resourcePath);

        return null;
    }

    private static MusicSpec GetMusicSpec(GameMusicState state)
    {
        switch (state)
        {
            case GameMusicState.MainMenu:
                return new MusicSpec(true, 0.68f, ResourceRoot + "Music/MUSIC_MainTheme_VietnamHeroic");
            case GameMusicState.Intro:
                return new MusicSpec(true, 0.34f, ResourceRoot + "Music/MUSIC_Intro_VietnamFlute");
            case GameMusicState.PreCombat:
                return new MusicSpec(true, 0.62f, ResourceRoot + "Music/SFX_WarDrums_Loop");
            case GameMusicState.Combat:
                return new MusicSpec(true, 0.78f, ResourceRoot + "Music/MUSIC_Battle_Pursuit");
            case GameMusicState.FinalBoss:
                return new MusicSpec(true, 0.82f, ResourceRoot + "Music/MUSIC_FinalBoss_Epic");
            case GameMusicState.LastStand:
                return new MusicSpec(true, 0.82f, ResourceRoot + "Music/MUSIC_LastStand_Heroic");
            case GameMusicState.Defeat:
                return new MusicSpec(false, 0.76f,
                    ResourceRoot + "Music/MUSIC_WarLament",
                    ResourceRoot + "Music/MUSIC_SadMemory_Vietnam");
            case GameMusicState.Victory:
                return new MusicSpec(false, 0.82f, ResourceRoot + "Music/MUSIC_Victory");
            default:
                return new MusicSpec(false, 0f);
        }
    }

    private static AmbientSpec GetAmbientSpec(GameAmbientState state)
    {
        switch (state)
        {
            case GameAmbientState.Cave:
                return new AmbientSpec(0.6f, ResourceRoot + "Ambience/CaveAmbience");
            case GameAmbientState.TimeRift:
                return new AmbientSpec(0.64f, ResourceRoot + "Ambience/TimeRiftHum");
            default:
                return new AmbientSpec(0f);
        }
    }

    private readonly struct MusicSpec
    {
        public readonly bool Loop;
        public readonly float Gain;
        public readonly string[] Paths;

        public MusicSpec(bool loop, float gain, params string[] paths)
        {
            Loop = loop;
            Gain = gain;
            Paths = paths;
        }
    }

    private readonly struct AmbientSpec
    {
        public readonly float Gain;
        public readonly string[] Paths;

        public AmbientSpec(float gain, params string[] paths)
        {
            Gain = gain;
            Paths = paths;
        }
    }
}
