using UnityEngine;

public sealed class S03StartTrigger : MonoBehaviour
{
    private S03ArenaDirector director;
    private bool triggered;

    private void Start()
    {
        director = FindAnyObjectByType<S03ArenaDirector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("S03StartTrigger: Something entered the trigger: Name=" + other.name + ", Tag=" + other.tag);

        if (triggered)
            return;

        if (other.CompareTag("Player") || other.name.Contains("Player") || other.name.Contains("Van An"))
        {
            triggered = true;
            if (director != null)
            {
                director.BeginArena();
                Debug.Log("S03StartTrigger: Player crossed the starting gate. Starting Arena!");
            }
            else
            {
                Debug.LogError("S03StartTrigger: Director is NULL!");
            }
            Destroy(gameObject);
        }
    }
}
