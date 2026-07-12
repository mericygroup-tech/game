using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth3D playerHealth;
    public Slider healthSlider;
    public TMP_Text healthText;
    public Image fillImage;
    public Color healthyColor = new Color(0.86f, 0.1f, 0.13f, 1f);
    public Color dangerColor = new Color(0.55f, 0.02f, 0.04f, 1f);

    private void Start()
    {
        if (playerHealth == null || healthSlider == null)
            return;

        healthSlider.minValue = 0;
        healthSlider.maxValue = playerHealth.maxHP;
        Refresh();
    }

    private void Update()
    {
        if (playerHealth == null || healthSlider == null)
            return;

        Refresh();
    }

    private void Refresh()
    {
        healthSlider.maxValue = playerHealth.maxHP;
        healthSlider.value = playerHealth.currentHP;

        if (healthText != null)
            healthText.text = playerHealth.currentHP + " / " + playerHealth.maxHP;

        if (fillImage != null && playerHealth.maxHP > 0)
        {
            float normalizedHealth = Mathf.Clamp01(playerHealth.currentHP / (float)playerHealth.maxHP);
            fillImage.color = Color.Lerp(dangerColor, healthyColor, normalizedHealth);
        }
    }
}
