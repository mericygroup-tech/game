using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class AudioButtonFeedback : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayIfInteractable();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayIfInteractable();
    }

    private void PlayIfInteractable()
    {
        if (UsesSpecializedBlessingFeedback())
            return;

        if (button != null && button.IsActive() && button.IsInteractable())
            GameAudio.PlayUiClick();
    }

    private bool UsesSpecializedBlessingFeedback()
    {
        return GetComponent<BlessingChoiceUI>() != null || gameObject.name.StartsWith("S03_Blessing");
    }
}
