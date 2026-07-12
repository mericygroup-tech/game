using UnityEngine;

public class S02TimeRiftTrigger : MonoBehaviour
{
    public S02CaveEventController controller;
    public TriggerKind triggerKind;

    public enum TriggerKind
    {
        AncientSigns,
        Voices,
        BlackStarDescent,
        TimeRift
    }

    private bool fired;

    private void Start()
    {
        if (controller == null)
            controller = FindAnyObjectByType<S02CaveEventController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) || controller == null)
            return;

        if (triggerKind == TriggerKind.TimeRift)
        {
            controller.SetPlayerNearTimeRift(true);
            return;
        }

        if (fired)
            return;

        fired = true;

        if (triggerKind == TriggerKind.AncientSigns)
            controller.TriggerAncientSigns();
        else if (triggerKind == TriggerKind.Voices)
            controller.TriggerVoices();
        else if (triggerKind == TriggerKind.BlackStarDescent)
            controller.TriggerBlackStarDescent();
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerKind != TriggerKind.TimeRift || controller == null || !IsPlayer(other))
            return;

        controller.SetPlayerNearTimeRift(false);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }
}
