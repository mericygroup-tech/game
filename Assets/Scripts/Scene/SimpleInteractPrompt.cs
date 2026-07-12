using System.Collections;
using TMPro;
using UnityEngine;

public class SimpleInteractPrompt : MonoBehaviour
{
    public Transform player;
    public TMP_Text interactionText;
    public TMP_Text storyText;
    public S01WarningTextUI warningUI;
    public string promptMessage = "Nhấn E để tương tác";
    public string storyMessage = "";
    public float storyDuration = 6f;
    public bool triggerOnce = true;
    public Renderer highlightRenderer;
    public GameObject activateOnInteract;
    public Color feedbackColor = new Color(0.1f, 0.85f, 1f, 1f);
    public float feedbackDuration = 0.55f;

    private bool playerInRange;
    private bool hasTriggered;
    private bool ownsPrompt;
    private bool warnedMissingInteractionText;
    private bool warnedMissingStoryTarget;
    private float storyHideTime;
    private Coroutine feedbackRoutine;

    private void Start()
    {
        FindReferencesIfNeeded();
        HidePrompt();
    }

    private void Update()
    {
        if (storyText != null && storyText.gameObject.activeSelf && storyHideTime > 0f && Time.time >= storyHideTime)
            storyText.gameObject.SetActive(false);

        if (!playerInRange || (triggerOnce && hasTriggered))
            return;

        PlayerController3D playerCtrl = FindAnyObjectByType<PlayerController3D>();
        if (playerCtrl != null && playerCtrl.InputLocked)
        {
            HidePrompt();
            return;
        }

        ShowPrompt();

        InputSettingsManager inputSettings = FindAnyObjectByType<InputSettingsManager>();
        KeyCode interactKey = (inputSettings != null && inputSettings.Keyboard != null) ? inputSettings.Keyboard.interact : KeyCode.E;

        if (Input.GetKeyDown(interactKey))
            Interact();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = true;
        FindReferencesIfNeeded();

        if (!triggerOnce || !hasTriggered)
            ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInRange = false;
        HidePrompt();
    }

    private void Interact()
    {
        hasTriggered = true;
        HidePrompt();

        if (activateOnInteract != null)
            activateOnInteract.SetActive(true);

        ShowInteractionResult();
        ShowFeedback();
        Debug.Log("S02 interaction completed: " + gameObject.name);
    }

    private void ShowPrompt()
    {
        if (interactionText == null)
        {
            FindReferencesIfNeeded();
            if (interactionText == null)
            {
                if (!warnedMissingInteractionText)
                {
                    warnedMissingInteractionText = true;
                    Debug.LogWarning("InteractionText missing for interaction prompt: " + gameObject.name, this);
                }

                return;
            }
        }

        interactionText.text = promptMessage;
        interactionText.gameObject.SetActive(true);
        ownsPrompt = true;
    }

    private void HidePrompt()
    {
        if (interactionText != null && ownsPrompt)
            interactionText.gameObject.SetActive(false);

        ownsPrompt = false;
    }

    private void ShowInteractionResult()
    {
        if (string.IsNullOrWhiteSpace(storyMessage))
            return;

        if (warningUI != null)
        {
            warningUI.ShowStory(storyMessage, storyDuration);
            return;
        }

        if (storyText != null)
        {
            storyText.text = storyMessage;
            storyText.gameObject.SetActive(true);
            storyHideTime = Time.time + storyDuration;
            return;
        }

        if (!warnedMissingStoryTarget)
        {
            warnedMissingStoryTarget = true;
            Debug.LogWarning("No StoryText or S01WarningTextUI found for interaction: " + gameObject.name, this);
        }

        Debug.Log(storyMessage);
    }

    private void ShowFeedback()
    {
        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(FeedbackSequence());
    }

    private IEnumerator FeedbackSequence()
    {
        SpawnFeedbackPulse();

        if (highlightRenderer == null)
            yield break;

        Material material = highlightRenderer.material;
        Color originalColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        Color originalEmission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;

        SetMaterialColor(material, feedbackColor, feedbackColor * 1.8f);
        yield return new WaitForSeconds(Mathf.Max(0.05f, feedbackDuration));
        SetMaterialColor(material, originalColor, originalEmission);
    }

    private void SpawnFeedbackPulse()
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = "Interaction_Complete_Pulse";
        pulse.transform.position = transform.position + Vector3.up * 1.35f;
        pulse.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);

        Collider pulseCollider = pulse.GetComponent<Collider>();
        if (pulseCollider != null)
            Destroy(pulseCollider);

        Renderer renderer = pulse.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            SetMaterialColor(material, feedbackColor, feedbackColor * 2f);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Destroy(pulse, Mathf.Max(0.2f, feedbackDuration));
    }

    private void SetMaterialColor(Material material, Color color, Color emission)
    {
        if (material == null)
            return;

        material.color = color;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
    }

    private void FindReferencesIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (interactionText == null)
            interactionText = FindTextInScene("InteractionText");

        if (storyText == null)
            storyText = FindTextInScene("StoryText");

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
}
