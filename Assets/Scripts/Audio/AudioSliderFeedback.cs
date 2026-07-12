using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public sealed class AudioSliderFeedback : MonoBehaviour, IPointerDownHandler, IDragHandler, IMoveHandler
{
    private const float TickStep = 0.04f;
    private const float MinimumTickInterval = 0.035f;

    private Slider slider;
    private float lastNormalizedValue;
    private float nextAllowedTickTime;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        lastNormalizedValue = slider != null ? slider.normalizedValue : 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanPlay())
            return;

        lastNormalizedValue = slider.normalizedValue;
        PlayTick();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanPlay())
            return;

        float normalizedValue = slider.normalizedValue;
        if (Mathf.Abs(normalizedValue - lastNormalizedValue) < TickStep)
            return;

        lastNormalizedValue = normalizedValue;
        PlayTick();
    }

    public void OnMove(AxisEventData eventData)
    {
        if (!CanPlay())
            return;

        if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right ||
            eventData.moveDir == MoveDirection.Up || eventData.moveDir == MoveDirection.Down)
        {
            lastNormalizedValue = slider.normalizedValue;
            PlayTick();
        }
    }

    private bool CanPlay()
    {
        return slider != null && slider.IsActive() && slider.IsInteractable();
    }

    private void PlayTick()
    {
        if (Time.unscaledTime < nextAllowedTickTime)
            return;

        nextAllowedTickTime = Time.unscaledTime + MinimumTickInterval;
        GameAudio.PlaySliderTick();
    }
}
