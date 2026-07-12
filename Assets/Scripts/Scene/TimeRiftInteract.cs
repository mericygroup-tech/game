using UnityEngine;
using TMPro;

public class TimeRiftInteract : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 4f;
    public KeyCode interactKey = KeyCode.E;

    [Header("References")]
    public Transform player;
    public GameObject interactionText;
    public GameObject storyText;

    [Header("Story")]
    [TextArea(2, 4)]
    public string interactMessage = "Nhấn E để chạm vào lỗ hổng thời gian.";

    [TextArea(2, 5)]
    public string activatedMessage = "Lỗ hổng thời gian bắt đầu cộng hưởng... Những Dị Thể Hắc Tinh đang kéo đến!";

    [Header("Enemy Spawn")]
    public GameObject enemyPrefab;
    public int enemyCount = 3;
    public float spawnRadius = 4f;

    private bool activated = false;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                player = playerObj.transform;
        }

        if (interactionText != null)
            interactionText.SetActive(false);

        if (storyText != null)
            storyText.SetActive(false);
    }

    private void Update()
    {
        if (player == null || activated)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= interactRange)
        {
            ShowInteractionText();

            if (Input.GetKeyDown(interactKey))
            {
                ActivateTimeRift();
            }
        }
        else
        {
            HideInteractionText();
        }
    }

    private void ShowInteractionText()
    {
        if (interactionText == null)
            return;

        interactionText.SetActive(true);

        TMP_Text tmpText = interactionText.GetComponent<TMP_Text>();
        if (tmpText != null)
            tmpText.text = interactMessage;
    }

    private void HideInteractionText()
    {
        if (interactionText != null)
            interactionText.SetActive(false);
    }

    private void ActivateTimeRift()
    {
        activated = true;

        HideInteractionText();

        if (storyText != null)
        {
            storyText.SetActive(true);

            TMP_Text tmpText = storyText.GetComponent<TMP_Text>();
            if (tmpText != null)
                tmpText.text = activatedMessage;
        }

        SpawnEnemies();

        Debug.Log("TimeRift activated. Spawn Hắc Tinh.");
    }

    private void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Chưa gán Enemy Prefab cho TimeRiftInteract.");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            float angle = i * Mathf.PI * 2f / enemyCount;

            Vector3 spawnPosition = transform.position + new Vector3(
                Mathf.Cos(angle) * spawnRadius,
                1f,
                Mathf.Sin(angle) * spawnRadius
            );

            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPosition,
                Quaternion.identity
            );

            MinionChase3D enemyChase = enemy.GetComponent<MinionChase3D>();

            if (enemyChase != null && player != null)
            {
                enemyChase.target = player;
            }

            enemy.tag = "Enemy";
        }
    }
}
