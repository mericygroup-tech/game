using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor-only smoke-test hooks for the generated Blessing UI. These methods
/// let designers validate feedback without waiting for an arena wave to end.
/// </summary>
public static class BlessingAudioQa
{
    [MenuItem("Tools/Dong Chay Anh Hung/Audio/Blessing/Show Choices (Play Mode)")]
    public static void ShowChoices()
    {
        RequirePlayMode();
        BlessingManager manager = Object.FindAnyObjectByType<BlessingManager>(FindObjectsInactive.Include);
        if (manager == null)
            throw new UnityException("Blessing audio QA failed: BlessingManager was not found.");

        manager.PresentChoices(null);
    }

    public static void HoverFirstChoice()
    {
        RequirePlayMode();
        BlessingChoiceUI[] cards = Object.FindObjectsByType<BlessingChoiceUI>(FindObjectsInactive.Include);
        if (cards.Length == 0)
            throw new UnityException("Blessing audio QA failed: no BlessingChoiceUI was found.");

        PointerEventData pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(cards[0].gameObject, pointer, ExecuteEvents.pointerEnterHandler);
    }

    public static void SelectFirstChoice()
    {
        RequirePlayMode();
        BlessingChoiceUI[] cards = Object.FindObjectsByType<BlessingChoiceUI>(FindObjectsInactive.Include);
        if (cards.Length == 0)
            throw new UnityException("Blessing audio QA failed: no BlessingChoiceUI was found.");

        Button button = cards[0].GetComponent<Button>();
        if (button == null)
            throw new UnityException("Blessing audio QA failed: first card has no Button.");

        button.onClick.Invoke();
    }

    public static void Reroll()
    {
        ClickButton("S03_BlessingRerollButton");
    }

    public static void Skip()
    {
        ClickButton("S03_BlessingSkipButton");
    }

    public static void ValidateMusicIsSilent()
    {
        RequirePlayMode();
        GameAudioDirector director = GameAudioDirector.Instance;
        if (director == null)
            throw new UnityException("Audio QA failed: GameAudioDirector was not created.");
        if (director.CurrentMusicState != GameMusicState.None)
            throw new UnityException("Audio QA failed: expected silent music state, got " + director.CurrentMusicState + ".");

        Debug.Log("[GameAudio] Current scene music state is None; authored video audio is isolated.");
    }

    private static void ClickButton(string objectName)
    {
        RequirePlayMode();
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == objectName)
            {
                buttons[i].onClick.Invoke();
                return;
            }
        }

        throw new UnityException("Blessing audio QA failed: missing button " + objectName + ".");
    }

    private static void RequirePlayMode()
    {
        if (!EditorApplication.isPlaying)
            throw new UnityException("Blessing audio QA requires Play Mode.");
    }
}
