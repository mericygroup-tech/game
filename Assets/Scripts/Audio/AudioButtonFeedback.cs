using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class AudioButtonFeedback : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    ISelectHandler,
    ISubmitHandler
{
    [SerializeField] private bool playHoverFeedback;

    private Button button;

    public bool HoverFeedbackEnabled => playHoverFeedback;

    private void Awake()
    {
        ResolveButton();
    }

    /// <summary>
    /// Hover/select feedback is opt-in so menus with their own specialized
    /// audio components cannot accidentally play the same sound twice.
    /// </summary>
    public void Configure(bool enableHoverFeedback)
    {
        playHoverFeedback = enableHoverFeedback;
        ResolveButton();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickIfInteractable();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverIfInteractable();
    }

    public void OnSelect(BaseEventData eventData)
    {
        PlayHoverIfInteractable();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClickIfInteractable();
    }

    private void PlayClickIfInteractable()
    {
        if (CanPlayFeedback())
            GameAudio.PlayUiClick();
    }

    private void PlayHoverIfInteractable()
    {
        if (playHoverFeedback && CanPlayFeedback())
            GameAudio.PlayUiHover();
    }

    private bool CanPlayFeedback()
    {
        ResolveButton();
        return !UsesSpecializedBlessingFeedback() &&
               button != null &&
               button.IsActive() &&
               button.IsInteractable();
    }

    private void ResolveButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private bool UsesSpecializedBlessingFeedback()
    {
        return GetComponent<BlessingChoiceUI>() != null || gameObject.name.StartsWith("S03_Blessing");
    }
}
