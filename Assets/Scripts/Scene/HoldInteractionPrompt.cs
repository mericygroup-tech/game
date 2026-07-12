using System.Collections;
using TMPro;
using UnityEngine;

public class HoldInteractionPrompt : MonoBehaviour
{
    public Transform player;
    public TMP_Text interactionText;
    public S01WarningTextUI warningUI;

    public string promptText = "Giữ E để tương tác";
    public string progressText = "Đang tương tác: X%";
    public float holdDuration = 1.5f;
    public float releaseResetSpeed = 3f;
    public bool triggerOnce = true;
    public bool playPlayerPushAnimation = true;

    public Transform targetTransform;
    public Vector3 completedLocalMoveOffset;
    public Vector3 completedLocalRotationOffset;
    public float completionAnimationDuration = 0.7f;
    public Collider colliderToDisable;
    public GameObject activateOnComplete;
    public string completionMessage = "";
    public float completionMessageDuration = 5f;
    public string promptShownLogMessage = "";
    public string completedLogMessage = "";

    private bool playerInRange;
    private bool completed;
    private bool promptShown;
    private float holdProgress;
    private bool pushAnimationStarted;
    private PlayerAnimatorDriver playerAnimatorDriver;

    private void Start()
    {
        FindReferencesIfNeeded();
        HidePrompt();

        if (targetTransform == null)
            targetTransform = transform;
    }

    private void Update()
    {
        if (!playerInRange || completed)
            return;

        FindReferencesIfNeeded();

        if (Input.GetKey(KeyCode.E))
        {
            PlayPushAnimationOnce();
            holdProgress += Time.deltaTime;
            ShowProgress();

            if (holdProgress >= holdDuration)
                CompleteInteraction();
        }
        else
        {
            StopPushAnimation();
            holdProgress = Mathf.MoveTowards(holdProgress, 0f, releaseResetSpeed * Time.deltaTime);
            ShowPrompt();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = true;
        FindReferencesIfNeeded();
        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = false;
        holdProgress = 0f;
        StopPushAnimation();
        HidePrompt();
    }

    private void CompleteInteraction()
    {
        if (completed)
            return;

        completed = true;
        HidePrompt();
        StopPushAnimation();

        if (activateOnComplete != null)
            activateOnComplete.SetActive(true);

        if (!string.IsNullOrEmpty(completedLogMessage))
            Debug.Log(completedLogMessage);

        if (!string.IsNullOrEmpty(completionMessage))
        {
            if (warningUI != null)
                warningUI.ShowWarning(completionMessage, completionMessageDuration);
            else
                Debug.Log(completionMessage);
        }

        StartCoroutine(AnimateCompletion());
    }

    private IEnumerator AnimateCompletion()
    {
        Transform animatedTarget = targetTransform != null ? targetTransform : transform;
        Vector3 startPosition = animatedTarget.localPosition;
        Quaternion startRotation = animatedTarget.localRotation;
        Vector3 targetPosition = startPosition + completedLocalMoveOffset;
        Quaternion targetRotation = startRotation * Quaternion.Euler(completedLocalRotationOffset);
        float duration = Mathf.Max(0.01f, completionAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            animatedTarget.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            animatedTarget.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        animatedTarget.localPosition = targetPosition;
        animatedTarget.localRotation = targetRotation;

        if (colliderToDisable != null)
            colliderToDisable.enabled = false;

        if (triggerOnce)
            gameObject.SetActive(false);
    }

    private void ShowPrompt()
    {
        if (interactionText == null || completed)
            return;

        interactionText.text = promptText;
        interactionText.gameObject.SetActive(true);

        if (!promptShown && !string.IsNullOrEmpty(promptShownLogMessage))
        {
            promptShown = true;
            Debug.Log(promptShownLogMessage);
        }
    }

    private void ShowProgress()
    {
        if (interactionText == null)
            return;

        int percent = Mathf.RoundToInt(Mathf.Clamp01(holdProgress / Mathf.Max(0.01f, holdDuration)) * 100f);
        interactionText.text = progressText.Replace("X", percent.ToString());
        interactionText.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);

        promptShown = false;
    }

    private void FindReferencesIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerAnimatorDriver == null && player != null)
            playerAnimatorDriver = player.GetComponent<PlayerAnimatorDriver>() ?? player.GetComponentInParent<PlayerAnimatorDriver>();

        if (interactionText == null)
            interactionText = FindTextInScene("InteractionText");

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();
    }

    private TMP_Text FindTextInScene(string objectName)
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text.name == objectName && text.gameObject.scene.IsValid())
                return text;
        }

        return null;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private void PlayPushAnimationOnce()
    {
        if (!playPlayerPushAnimation || pushAnimationStarted)
            return;

        FindReferencesIfNeeded();
        if (playerAnimatorDriver == null)
            return;

        playerAnimatorDriver.PlayPush();
        S01Soundscape.PlayDebrisPush();
        pushAnimationStarted = true;
    }

    private void StopPushAnimation()
    {
        if (!pushAnimationStarted)
            return;

        FindReferencesIfNeeded();
        if (playerAnimatorDriver != null)
            playerAnimatorDriver.StopPush();

        pushAnimationStarted = false;
    }
}
