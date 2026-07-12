using UnityEngine;
using TMPro;
using System.Collections;

public class EscapeDoorQTE : MonoBehaviour
{
    public int requiredPressCount = 5;
    public float interactRange = 3f;
    public string promptText = "Nhấn E liên tục để vượt chướng ngại";

    public Transform player;
    public GameObject interactionText;
    public bool deactivateOnComplete = true;
    public bool disableColliderOnComplete = true;
    public Vector3 completedLocalMoveOffset = Vector3.zero;
    public Vector3 completedLocalRotationOffset = Vector3.zero;
    public float completionAnimationDuration = 0.65f;
    public GameObject activateOnComplete;
    public string completionMessage = "";
    public float completionMessageDuration = 5f;
    public string promptShownLogMessage = "";
    public string completedLogMessage = "";

    private int currentPressCount = 0;
    private bool opened = false;
    private bool promptVisible;
    private TMP_Text interactionLabel;
    private bool warnedMissingInteractionText;
    private S01WarningTextUI warningUI;

    private void Start()
    {
        ResolvePlayer();
        ResolveInteractionText();
        ResolveWarningUI();

        HideText();
    }

    private void Update()
    {
        if (opened)
        {
            HideText();
            return;
        }

        if (player == null)
            ResolvePlayer();

        if (interactionText == null || interactionLabel == null)
            ResolveInteractionText();

        if (warningUI == null)
            ResolveWarningUI();

        if (player == null)
            return;

        PlayerController3D playerCtrl = player.GetComponent<PlayerController3D>();
        if (playerCtrl != null && playerCtrl.InputLocked)
        {
            HideText();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange)
        {
            ShowText();

            InputSettingsManager inputSettings = FindAnyObjectByType<InputSettingsManager>();
            KeyCode interactKey = (inputSettings != null && inputSettings.Keyboard != null) ? inputSettings.Keyboard.interact : KeyCode.E;

            if (Input.GetKeyDown(interactKey))
            {
                currentPressCount++;

                if (currentPressCount >= requiredPressCount)
                    CompleteQTE();
                else
                    UpdateText();
            }
        }
        else
        {
            HideText();
        }
    }

    private void ResolvePlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void ResolveInteractionText()
    {
        if (interactionText == null)
        {
            GameObject activeObject = GameObject.Find("InteractionText");
            if (activeObject != null)
                interactionText = activeObject;
        }

        if (interactionText == null)
        {
            TMP_Text[] labels = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (TMP_Text label in labels)
            {
                if (label.name == "InteractionText" && label.gameObject.scene.IsValid())
                {
                    interactionText = label.gameObject;
                    break;
                }
            }
        }

        interactionLabel = interactionText != null ? interactionText.GetComponent<TMP_Text>() : null;

        if (interactionText == null && !warnedMissingInteractionText)
        {
            warnedMissingInteractionText = true;
            Debug.LogWarning(name + " could not find InteractionText. Run the S01 builder or create a TextMeshProUGUI named InteractionText under Canvas.");
        }
    }

    private void ResolveWarningUI()
    {
        warningUI = FindAnyObjectByType<S01WarningTextUI>();
    }

    private void ShowText()
    {
        if (interactionText == null)
            return;

        interactionText.SetActive(true);
        UpdateText();

        if (!promptVisible)
        {
            promptVisible = true;

            if (!string.IsNullOrEmpty(promptShownLogMessage))
                Debug.Log(promptShownLogMessage);
        }
    }

    private void UpdateText()
    {
        if (interactionLabel == null)
            return;

        interactionLabel.text = promptText + ": " + currentPressCount + "/" + requiredPressCount;
    }

    private void HideText()
    {
        if (interactionText != null)
            interactionText.SetActive(false);

        promptVisible = false;
    }

    private void CompleteQTE()
    {
        opened = true;

        HideText();

        if (!string.IsNullOrEmpty(completedLogMessage))
            Debug.Log(completedLogMessage);
        else
            Debug.Log(gameObject.name + " QTE completed.");

        if (!string.IsNullOrEmpty(completionMessage))
        {
            if (warningUI != null)
                warningUI.ShowWarning(completionMessage, completionMessageDuration);
            else
                Debug.Log(completionMessage);
        }

        if (activateOnComplete != null)
            activateOnComplete.SetActive(true);

        StartCoroutine(CompleteAnimation());
    }

    private IEnumerator CompleteAnimation()
    {
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;
        Vector3 targetPosition = startPosition + completedLocalMoveOffset;
        Quaternion targetRotation = startRotation * Quaternion.Euler(completedLocalRotationOffset);
        float duration = Mathf.Max(0.01f, completionAnimationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;

        if (disableColliderOnComplete)
        {
            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        if (deactivateOnComplete)
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        HideText();
    }
}
