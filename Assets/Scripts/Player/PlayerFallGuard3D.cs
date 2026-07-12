using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerFallGuard3D : MonoBehaviour
{
    [SerializeField] private Vector3 safePosition;
    [SerializeField] private float minimumAllowedY = -10f;
    [SerializeField] private bool recoverWhenBelowMinimumY = true;

    private PlayerController3D playerController;
    private CharacterController characterController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController3D>();
        characterController = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (!recoverWhenBelowMinimumY || transform.position.y >= minimumAllowedY)
            return;

        Recover();
    }

    public void Configure(Vector3 fallbackPosition, float minimumY)
    {
        safePosition = fallbackPosition;
        minimumAllowedY = minimumY;
    }

    public void Recover()
    {
        if (playerController != null)
        {
            playerController.TeleportTo(safePosition);
            return;
        }

        bool wasEnabled = characterController == null || characterController.enabled;
        if (characterController != null)
            characterController.enabled = false;

        transform.position = safePosition;

        if (characterController != null)
            characterController.enabled = wasEnabled;
    }
}
