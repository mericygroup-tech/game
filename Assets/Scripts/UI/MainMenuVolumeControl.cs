using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public sealed class MainMenuVolumeControl : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    private Slider slider;

    public void Configure(TMP_Text percentageText)
    {
        valueText = percentageText;
    }

    private void Awake()
    {
        slider = GetComponent<Slider>();
        float savedVolume = GameAudio.GetVolume(GameAudioBus.Master);
        slider.SetValueWithoutNotify(savedVolume);
        UpdateValueText(savedVolume);
    }

    public void SetMasterVolume(float value)
    {
        float normalizedValue = Mathf.Clamp01(value);
        GameAudio.SetVolume(GameAudioBus.Master, normalizedValue);
        UpdateValueText(normalizedValue);
    }

    private void UpdateValueText(float value)
    {
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }
}
