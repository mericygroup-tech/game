using UnityEngine;

[DisallowMultipleComponent]
public class S01Soundscape : MonoBehaviour
{
    public static S01Soundscape Instance { get; private set; }

    public AudioClip cityAmbience;
    public AudioClip chaseHeartbeatLoop;
    public AudioClip slowMotionWhoosh;
    public AudioClip phoneGlitchSignalLoss;
    public AudioClip mudStep;
    public AudioClip impactHit;
    public AudioClip groundCollapse;
    public AudioClip debrisPushQte;
    public AudioClip darkStarRoar;

    [Range(0f, 1f)] public float ambienceVolume = 0.28f;
    [Range(0f, 1f)] public float heartbeatVolume = 0.42f;
    [Range(0f, 1f)] public float oneShotVolume = 0.78f;

    private AudioSource ambienceSource;
    private AudioSource heartbeatSource;
    private AudioSource oneShotSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSources();
    }

    private void Start()
    {
        PlayAmbience();
    }

    public static void PlaySlowMotionWhoosh()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.slowMotionWhoosh);
    }

    public static void PlayPhoneGlitch()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.phoneGlitchSignalLoss);
    }

    public static void PlayMudStep()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.mudStep, 0.7f);
    }

    public static void PlayImpactHit()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.impactHit);
    }

    public static void PlayGroundCollapse()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.groundCollapse);
    }

    public static float PlayGroundCollapseAndGetDuration()
    {
        if (Instance == null || Instance.groundCollapse == null)
            return 0f;

        Instance.PlayOneShot(Instance.groundCollapse);
        return Instance.groundCollapse.length;
    }

    public static void PlayDebrisPush()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.debrisPushQte, 0.85f);
    }

    public static void PlayDarkStarRoar()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.darkStarRoar);
    }

    public static void StartHeartbeat()
    {
        if (Instance != null)
            Instance.PlayHeartbeat();
    }

    public static void StopHeartbeat()
    {
        if (Instance != null && Instance.heartbeatSource != null)
            Instance.heartbeatSource.Stop();
    }

    public static void StopActionSounds()
    {
        if (Instance == null)
            return;

        if (Instance.oneShotSource != null)
            Instance.oneShotSource.Stop();

        StopHeartbeat();
    }

    public static void PlayWarningCue(string triggerName, string message)
    {
        if (Instance == null)
            return;

        string key = ((triggerName ?? string.Empty) + " " + (message ?? string.Empty)).ToLowerInvariant();

        if (key.Contains("mud") || key.Contains("bùn") || key.Contains("bun"))
            PlayMudStep();
        else if (key.Contains("chase") || key.Contains("hắc tinh") || key.Contains("hac tinh"))
        {
            PlayDarkStarRoar();
            StartHeartbeat();
        }
        else if (key.Contains("phone") || key.Contains("signal") || key.Contains("tín hiệu") || key.Contains("tin hieu"))
            PlayPhoneGlitch();
    }

    private void PlayAmbience()
    {
        if (cityAmbience == null || ambienceSource == null)
            return;

        ambienceSource.clip = cityAmbience;
        ambienceSource.loop = true;
        ambienceSource.volume = ambienceVolume;

        if (!ambienceSource.isPlaying)
            ambienceSource.Play();
    }

    private void PlayHeartbeat()
    {
        if (chaseHeartbeatLoop == null || heartbeatSource == null)
            return;

        heartbeatSource.clip = chaseHeartbeatLoop;
        heartbeatSource.loop = true;
        heartbeatSource.volume = heartbeatVolume;

        if (!heartbeatSource.isPlaying)
            heartbeatSource.Play();
    }

    private void PlayOneShot(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || oneShotSource == null)
            return;

        oneShotSource.PlayOneShot(clip, oneShotVolume * volumeMultiplier);
    }

    private void EnsureSources()
    {
        ambienceSource = CreateSource("S01_Ambience_Source", true);
        heartbeatSource = CreateSource("S01_Heartbeat_Source", true);
        oneShotSource = CreateSource("S01_OneShot_Source", false);
    }

    private AudioSource CreateSource(string sourceName, bool looping)
    {
        Transform existing = transform.Find(sourceName);
        GameObject sourceObject = existing != null ? existing.gameObject : new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null)
            source = sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = looping;
        source.spatialBlend = 0f;
        return source;
    }
}
