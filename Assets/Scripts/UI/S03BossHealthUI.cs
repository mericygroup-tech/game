using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class S03BossHealthUI : MonoBehaviour
{
    private MinionHealth3D bossHealth;
    private Slider healthSlider;
    private TMP_Text bossNameText;

    public void Setup(MinionHealth3D health, string bossName)
    {
        bossHealth = health;
        healthSlider = GetComponentInChildren<Slider>();
        bossNameText = GetComponentInChildren<TMP_Text>();

        if (healthSlider != null && bossHealth != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = bossHealth.maxHP;
            healthSlider.value = bossHealth.currentHP;
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossName;
        }
    }

    private void Update()
    {
        if (bossHealth == null)
        {
            Destroy(gameObject);
            return;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = bossHealth.maxHP;
            healthSlider.value = bossHealth.currentHP;
        }
    }
}
