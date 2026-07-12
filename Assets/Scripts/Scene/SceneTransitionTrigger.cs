using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    public string nextSceneName = "S02_UndergroundCave";
    public float delayBeforeLoad = 2f;
    public bool waitForGroundCollapseSound = true;
    public float maxGroundCollapseWaitTime = 1.2f;
    public float postSoundLoadPadding = 0f;

    private bool triggered = false;
    private PlayerHealth3D triggeringPlayerHealth;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (other.CompareTag("Player"))
        {
            triggeringPlayerHealth = other.GetComponent<PlayerHealth3D>() ?? other.GetComponentInParent<PlayerHealth3D>();
            if (IsTriggeringPlayerDead())
                return;

            triggered = true;
            Debug.Log("Mặt đất rung chuyển dữ dội... cả nhóm rơi xuống hang cổ.");
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        float waitTime = delayBeforeLoad;
        if (waitForGroundCollapseSound)
        {
            float collapseDuration = S01Soundscape.PlayGroundCollapseAndGetDuration();
            if (collapseDuration > 0f)
                waitTime = Mathf.Min(collapseDuration, Mathf.Max(0.05f, maxGroundCollapseWaitTime)) +
                           Mathf.Max(0f, postSoundLoadPadding);
        }

        float elapsed = 0f;
        while (elapsed < waitTime)
        {
            if (IsTriggeringPlayerDead())
            {
                CancelTransition();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (IsTriggeringPlayerDead())
        {
            CancelTransition();
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private bool IsTriggeringPlayerDead()
    {
        return triggeringPlayerHealth != null && triggeringPlayerHealth.isDead;
    }

    public void CancelTransition()
    {
        triggered = false;
        StopAllCoroutines();
        S01Soundscape.StopActionSounds();
    }
}
