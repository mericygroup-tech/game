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
        if (button != null && button.IsActive() && button.IsInteractable())
            GameAudio.PlayUiClick();
    }
}
