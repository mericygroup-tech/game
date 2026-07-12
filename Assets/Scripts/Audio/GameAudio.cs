using UnityEngine;

/// <summary>
/// Small, null-safe facade used by gameplay code. The persistent director is
/// created before the first scene, so callers do not need scene references.
/// </summary>
public static class GameAudio
{
    public static void PlayMusic(GameMusicState state, float fadeDuration = 0.75f)
    {
        GameAudioDirector.Instance?.PlayMusic(state, fadeDuration);
    }

    public static void PlayAmbient(GameAmbientState state, float fadeDuration = 0.55f)
    {
        GameAudioDirector.Instance?.PlayAmbient(state, fadeDuration);
    }

    public static void PlaySwordSwing(bool heavy)
    {
        GameAudioDirector.Instance?.PlaySwordSwing(heavy);
    }

    public static void PlaySwordImpact(bool heavy)
    {
        GameAudioDirector.Instance?.PlaySwordImpact(heavy);
    }

    public static void PlayUiClick()
    {
        GameAudioDirector.Instance?.PlayUiClick();
    }

    public static void PlaySliderTick()
    {
        GameAudioDirector.Instance?.PlaySliderTick();
    }

    public static void PlayDefeat()
    {
        GameAudioDirector.Instance?.PlayDefeat();
    }

    public static void PlayVictory()
    {
        GameAudioDirector.Instance?.PlayVictory();
    }

    public static float GetVolume(GameAudioBus bus)
    {
        return GameAudioDirector.Instance != null
            ? GameAudioDirector.Instance.GetVolume(bus)
            : 1f;
    }

    public static void SetVolume(GameAudioBus bus, float normalizedVolume)
    {
        GameAudioDirector.Instance?.SetVolume(bus, Mathf.Clamp01(normalizedVolume));
    }
}
