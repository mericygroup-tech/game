using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashChargeUI : MonoBehaviour
{
    [Header("Player Target")]
    public PlayerController3D playerController;

    [Header("UI References")]
    public Image dashIcon;
    public Image cooldownOverlay; // Should have Image Type set to Filled (Radial 360)
    public TMP_Text chargesText;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController3D>();
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        }
    }

    private void Update()
    {
        if (playerController == null)
            return;

        // Display remaining charges
        if (chargesText != null)
        {
            chargesText.text = playerController.DashCharges.ToString();
        }

        // Display cooldown/recharge fill (1 = fully on cooldown/start, 0 = recharged/ready)
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 1f - playerController.DashRechargeProgress;
        }
    }
}
