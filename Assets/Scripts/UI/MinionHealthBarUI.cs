using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MinionHealthBarUI : MonoBehaviour
{
    [FormerlySerializedAs("enemyHealth")]
    public MinionHealth3D minionHealth;
    public Slider healthSlider;
    public Transform cameraTransform;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (minionHealth != null && healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = minionHealth.maxHP;
            healthSlider.value = minionHealth.currentHP;
        }
    }

    private void LateUpdate()
    {
        if (minionHealth == null || healthSlider == null)
            return;

        healthSlider.maxValue = minionHealth.maxHP;
        healthSlider.value = minionHealth.currentHP;

        if (cameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - cameraTransform.position
            );
        }
    }
}
