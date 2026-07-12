using TMPro;
using UnityEngine;

public class S01WarningTextUI : MonoBehaviour
{
    public TMP_Text warningText;
    public TMP_Text storyText;
    public float defaultDuration = 5f;

    private float warningHideTime;
    private float storyHideTime;

    private void Awake()
    {
        HideWarning();
        HideStory();
    }

    private void Update()
    {
        if (warningText != null && warningText.gameObject.activeSelf && Time.time >= warningHideTime)
            HideWarning();

        if (storyText != null && storyText.gameObject.activeSelf && Time.time >= storyHideTime)
            HideStory();
    }

    public void ShowWarning(string message)
    {
        ShowWarning(message, defaultDuration);
    }

    public void ShowWarning(string message, float duration)
    {
        if (warningText == null)
            return;

        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningHideTime = Time.time + duration;
    }

    public void ShowStory(string message)
    {
        ShowStory(message, defaultDuration);
    }

    public void ShowStory(string message, float duration)
    {
        if (storyText == null)
            return;

        storyText.text = message;
        storyText.gameObject.SetActive(true);
        storyHideTime = Time.time + duration;
    }

    private void HideWarning()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    private void HideStory()
    {
        if (storyText != null)
            storyText.gameObject.SetActive(false);
    }
}
