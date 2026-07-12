using UnityEngine;

public class S01WarningTrigger : MonoBehaviour
{
    public string message;
    public bool showAsStory;
    public float duration = 5f;
    public S01WarningTextUI warningUI;

    private bool triggered;

    private void Start()
    {
        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        triggered = true;

        S01ChaseIntroCutscene chaseIntroCutscene = GetComponent<S01ChaseIntroCutscene>();
        if (chaseIntroCutscene != null)
        {
            if (chaseIntroCutscene.TryPlay(other.transform.root) || chaseIntroCutscene.HasPlayed)
                return;
        }

        S01Soundscape.PlayWarningCue(name, message);

        if (warningUI == null)
            return;

        if (showAsStory)
            warningUI.ShowStory(message, duration);
        else
            warningUI.ShowWarning(message, duration);
    }
}
