using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuButtonFX : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Graphic frame;
    [SerializeField] private Graphic glow;
    [SerializeField] private Color normalTextColor = new Color32(220, 194, 138, 255);
    [SerializeField] private Color hoverTextColor = Color.white;
    [SerializeField] private Color normalFrameColor = new Color32(25, 21, 17, 225);
    [SerializeField] private Color hoverFrameColor = new Color32(142, 32, 25, 242);
    [SerializeField] private float hoverScale = 1.045f;
    [SerializeField] private float transitionSpeed = 12f;

    private bool isHighlighted;
    [SerializeField] private CanvasGroup artworkHighlight;
    private Vector3 baseScale;
    private bool isPointerOver;
    private bool isSelected;
    private bool isPressed;

    public void Configure(TMP_Text label, Graphic frame, Graphic glow, bool primary)
    {
        this.label = label;
        this.frame = frame;
        this.glow = glow;

        if (primary)
        {
            normalFrameColor = new Color32(112, 27, 20, 236);
            hoverFrameColor = new Color32(172, 41, 28, 245);
        }
    }

    public void ConfigureArtwork(CanvasGroup highlight)
    {
        label = null;
        frame = null;
        glow = null;
        artworkHighlight = highlight;
        hoverScale = 1f;
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

        if (artworkHighlight != null)
        {
            float targetAlpha = isPressed ? 0.34f : isHighlighted ? 0.24f : 0f;
            artworkHighlight.alpha = Mathf.Lerp(
                artworkHighlight.alpha,
                targetAlpha,
                Time.unscaledDeltaTime * transitionSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        RefreshHighlight();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        isPressed = false;
        RefreshHighlight();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isPressed = true;
            RefreshHighlight();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            isPressed = false;
            RefreshHighlight();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        RefreshHighlight();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        isPressed = false;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        bool highlighted = isPointerOver || isSelected || isPressed;
        if (highlighted && !isHighlighted)
            GameAudio.PlayUiHover();

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

        if (artworkHighlight != null)
            artworkHighlight.alpha = 0f;
    }
}
