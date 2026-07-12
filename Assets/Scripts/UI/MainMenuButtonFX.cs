using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Graphic frame;
    [SerializeField] private Graphic glow;
    [SerializeField] private Color normalTextColor = new Color32(220, 194, 138, 255);
    [SerializeField] private Color hoverTextColor = Color.white;
    [SerializeField] private Color normalFrameColor = new Color32(83, 53, 28, 190);
    [SerializeField] private Color hoverFrameColor = new Color32(142, 32, 25, 235);
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float transitionSpeed = 12f;

    private bool isHighlighted;
    private Vector3 baseScale;

    public void Configure(TMP_Text label, Graphic frame, Graphic glow, bool primary)
    {
        this.label = label;
        this.frame = frame;
        this.glow = glow;

        if (primary)
        {
            normalFrameColor = new Color32(102, 26, 22, 220);
            hoverFrameColor = new Color32(172, 41, 28, 245);
        }
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        ApplyImmediate();
    }

    private void Update()
    {
        float target = isHighlighted ? 1f : 0f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * Mathf.Lerp(1f, hoverScale, target), Time.unscaledDeltaTime * transitionSpeed);

        if (label != null)
            label.color = Color.Lerp(label.color, isHighlighted ? hoverTextColor : normalTextColor, Time.unscaledDeltaTime * transitionSpeed);

        if (frame != null)
            frame.color = Color.Lerp(frame.color, isHighlighted ? hoverFrameColor : normalFrameColor, Time.unscaledDeltaTime * transitionSpeed);

        if (glow != null)
        {
            Color glowColor = glow.color;
            glowColor.a = Mathf.Lerp(glowColor.a, isHighlighted ? 0.55f : 0f, Time.unscaledDeltaTime * transitionSpeed);
            glow.color = glowColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlighted(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlighted(false);
    }

    private void SetHighlighted(bool highlighted)
    {
        isHighlighted = highlighted;
    }

    private void ApplyImmediate()
    {
        if (label != null)
            label.color = normalTextColor;

        if (frame != null)
            frame.color = normalFrameColor;

        if (glow != null)
        {
            Color color = glow.color;
            color.a = 0f;
            glow.color = color;
        }
    }
}
