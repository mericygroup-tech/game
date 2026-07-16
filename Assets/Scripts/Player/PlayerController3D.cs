using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Input Settings")]
    private InputSettingsManager inputManager;

    [Header("Movement")]
    public float moveSpeed = 8f; // Run by default
    public float rotationSpeed = 12f;
    public float gravity = -20f;
    public float groundedStickForce = -2f;
    public float stepOffset = 0.55f;
    public float slopeLimit = 55f;

    [Header("Camera")]
    public Transform cameraTransform;
    public bool alwaysFaceCamera = true;

    [Header("Dash Settings")]
    public bool enableDash = true;
    public float dashSpeed = 16f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.4f;
    public bool dashInvincible = true;
    public bool dashTowardsMouse = true;

    // Dash state
    private bool isDashing;
    private float dashTimeRemaining;
    private float lastDashTime = -999f;
    private Vector3 dashDirection;
    private int originalLayer;

    // Dash charges (1 charge, 0.4s recharge)
    private int dashCharges = 1;
    private float dashRechargeTimer;
    private const int MaxDashCharges = 1;

    // Buffer and Lock features
    private bool dashBuffered;
    private float dashBufferTime;
    private const float DashBufferWindow = 0.1f;

    public bool IsStunned { get; set; }
    public bool InputLocked { get; set; }

    private CharacterController controller;
    private Vector3 velocity;
    private float originalStepOffset;
    private float temporarySpeedMultiplier = 1f;
    private float temporarySpeedUntil;

    public bool IsDashing => isDashing;
    public int DashCharges => dashCharges;
    private float DashRechargeTime => Mathf.Max(0.08f, dashCooldown);
    public float DashRechargeProgress => dashCharges >= MaxDashCharges ? 1f : (dashRechargeTimer / DashRechargeTime);

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputManager = GetComponent<InputSettingsManager>();
        if (inputManager == null)
        {
            inputManager = gameObject.AddComponent<InputSettingsManager>();
        }

        originalStepOffset = controller.stepOffset;
        originalLayer = gameObject.layer;
        ConfigureControllerForPrototypeTraversal();
    }

    private void Update()
    {
        // Update recharge timer for dash charges
        if (dashCharges < MaxDashCharges)
        {
            dashRechargeTimer += Time.deltaTime;
            if (dashRechargeTimer >= DashRechargeTime)
            {
                dashCharges++;
                dashRechargeTimer = 0f;
            }
        }

        MovePlayer();
    }

    private void MovePlayer()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (controller == null || !controller.enabled)
            return;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Process buffer
        if (dashBuffered)
        {
            if (Time.time - dashBufferTime > DashBufferWindow)
            {
                dashBuffered = false;
            }
            else if (!isDashing && dashCharges > 0 && Time.time >= lastDashTime + dashCooldown && !IsStunned && !InputLocked)
            {
                dashBuffered = false;
                StartDash();
            }
        }

        // Check for dash input
        KeyCode dashKey = (inputManager != null && inputManager.Keyboard != null) ? inputManager.Keyboard.dash : KeyCode.LeftShift;
        if (enableDash && !InputLocked && Input.GetKeyDown(dashKey))
        {
            if (!isDashing && dashCharges > 0 && Time.time >= lastDashTime + dashCooldown && !IsStunned)
            {
                StartDash();
            }
            else if (!IsStunned)
            {
                dashBuffered = true;
                dashBufferTime = Time.time;
            }
        }

        if (isDashing)
        {
            UpdateDash();
            return;
        }

        // Normal WASD movement
        Vector3 moveDirection = GetWasdMoveDirection();
        float currentSpeed = moveSpeed * GetTemporarySpeedMultiplier();

        if (!InputLocked && !IsStunned)
        {
            if (moveDirection.magnitude >= 0.1f)
            {
                controller.Move(moveDirection * currentSpeed * Time.deltaTime);
            }

            // Handle rotation
            bool shouldFaceCamera = alwaysFaceCamera;
            if (shouldFaceCamera && cameraTransform != null)
            {
                ThirdPersonCamera cam = cameraTransform.GetComponent<ThirdPersonCamera>();
                if (cam != null && cam.fixedAngle)
                {
                    shouldFaceCamera = false;
                }
            }

            if (shouldFaceCamera)
            {
                if (cameraTransform != null)
                {
                    Vector3 cameraForward = cameraTransform.forward;
                    cameraForward.y = 0f;
                    if (cameraForward.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(cameraForward.normalized);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            targetRotation,
                            rotationSpeed * Time.deltaTime
                        );
                    }
                }
            }
            else
            {
                if (moveDirection.magnitude >= 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        bool grounded = controller.isGrounded;

        if (grounded && velocity.y < 0f)
            velocity.y = groundedStickForce;

        // Apply normal gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void StartDash()
    {
        PlayerCombat3D combat = GetComponent<PlayerCombat3D>();
        if (combat != null && combat.IsHeavyAttackActive)
            return;

        isDashing = true;
        dashTimeRemaining = dashDuration;
        lastDashTime = Time.time;
        dashCharges--;
        dashRechargeTimer = 0f;

        // Cancel active light attack if applicable
        if (combat != null && combat.CanCancelCurrentAttack())
        {
            combat.CancelAttack();
        }

        // Determine dash direction
        if (dashTowardsMouse)
        {
            Vector3 mouseDir = GetMouseDirection();
            dashDirection = mouseDir.sqrMagnitude > 0.001f ? mouseDir : transform.forward;
        }
        else
        {
            Vector3 moveInput = GetWasdMoveDirection();
            dashDirection = moveInput.magnitude > 0.1f ? moveInput : transform.forward;
        }
        dashDirection.y = 0f;
        dashDirection.Normalize();

        // Rotate player immediately to dash direction
        if (dashDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dashDirection);
        }

        // Trigger animation safely
        PlayerAnimatorDriver animatorDriver = GetComponent<PlayerAnimatorDriver>();
        if (animatorDriver != null)
        {
            animatorDriver.PlayDash();
        }

        // Set layer to Ignore Raycast to handle custom collision logic (gliding through enemies)
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer != -1)
            gameObject.layer = ignoreRaycastLayer;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1 && ignoreRaycastLayer != -1)
        {
            Physics.IgnoreLayerCollision(gameObject.layer, enemyLayer, true);
        }
    }

    private void UpdateDash()
    {
        dashTimeRemaining -= Time.deltaTime;

        // Horizontal dash movement
        Vector3 horizontalMove = dashDirection * dashSpeed * Time.deltaTime;
        Vector3 safeHorizontalMove = GetSafeDashMovement(horizontalMove);
        controller.Move(safeHorizontalMove);

        // Vertical movement (keep gravity)
        bool grounded = controller.isGrounded;
        if (grounded && velocity.y < 0f)
        {
            velocity.y = groundedStickForce;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
        controller.Move(velocity * Time.deltaTime);

        if (dashTimeRemaining <= 0f)
        {
            EndDash();
        }
    }

    public void EndDash()
    {
        isDashing = false;
        gameObject.layer = originalLayer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (enemyLayer != -1 && ignoreRaycastLayer != -1)
        {
            Physics.IgnoreLayerCollision(ignoreRaycastLayer, enemyLayer, false);
        }
    }

    public void RestoreDashCharge()
    {
        dashCharges = MaxDashCharges;
        dashRechargeTimer = 0f;
        dashBuffered = false;
    }

    public void SetInputEnabled(bool enabled)
    {
        InputLocked = !enabled;
        if (!enabled)
            dashBuffered = false;
    }

    public void ResetMotion()
    {
        velocity = Vector3.zero;
        isDashing = false;
        dashTimeRemaining = 0f;
        dashDirection = Vector3.zero;
        dashBuffered = false;
        dashBufferTime = 0f;
        dashCharges = MaxDashCharges;
        dashRechargeTimer = 0f;
        temporarySpeedMultiplier = 1f;
        temporarySpeedUntil = 0f;
        IsStunned = false;

        gameObject.layer = originalLayer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (enemyLayer != -1 && ignoreRaycastLayer != -1)
            Physics.IgnoreLayerCollision(ignoreRaycastLayer, enemyLayer, false);
    }

    public void TeleportAndReset(Vector3 position, Quaternion rotation)
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        bool wasEnabled = controller == null || controller.enabled;
        if (controller != null)
            controller.enabled = false;

        ResetMotion();
        transform.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();

        if (controller != null)
            controller.enabled = wasEnabled;
    }

    public void TeleportTo(Vector3 position)
    {
        TeleportAndReset(position, transform.rotation);
    }

    private Vector3 GetSafeDashMovement(Vector3 desiredMove)
    {
        float distance = desiredMove.magnitude;
        if (distance < 0.001f)
            return desiredMove;

        Vector3 direction = desiredMove.normalized;
        float radius = controller.radius;
        float height = controller.height;
        Vector3 point1 = transform.position + Vector3.up * radius;
        Vector3 point2 = transform.position + Vector3.up * (height - radius);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        int transparentFXLayer = LayerMask.NameToLayer("TransparentFX");
        int waterLayer = LayerMask.NameToLayer("Water");
        int uiLayer = LayerMask.NameToLayer("UI");

        int ignoreMask = (1 << ignoreRaycastLayer) | (1 << transparentFXLayer) | (1 << uiLayer);
        if (enemyLayer != -1)
            ignoreMask |= (1 << enemyLayer);

        int blockMask = ~ignoreMask;

        if (Physics.CapsuleCast(point1, point2, radius * 0.9f, direction, out RaycastHit hit, distance, blockMask, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
            return direction * safeDistance;
        }

        return desiredMove;
    }

    private Vector3 GetWasdMoveDirection()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (inputManager != null && inputManager.Keyboard != null)
        {
            if (Input.GetKey(inputManager.Keyboard.moveLeft)) horizontal -= 1f;
            if (Input.GetKey(inputManager.Keyboard.moveRight)) horizontal += 1f;
            if (Input.GetKey(inputManager.Keyboard.moveForward)) vertical += 1f;
            if (Input.GetKey(inputManager.Keyboard.moveBackward)) vertical -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        }

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

        if (input.magnitude < 0.1f)
            return Vector3.zero;

        if (cameraTransform == null)
            return input;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraForward * input.z + cameraRight * input.x).normalized;
    }

    private Vector3 GetMouseDirection()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector3 camForward = mainCamera.transform.forward;
                camForward.y = 0f;
                return camForward.normalized;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane playerPlane = new Plane(Vector3.up, transform.position);
            if (playerPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0f;
                return direction.normalized;
            }
        }
        return Vector3.zero;
    }

    private void ConfigureControllerForPrototypeTraversal()
    {
        controller.stepOffset = Mathf.Max(originalStepOffset, stepOffset);
        controller.slopeLimit = Mathf.Max(controller.slopeLimit, slopeLimit);
    }

    public void ApplyTemporarySlow(float multiplier, float duration)
    {
        temporarySpeedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        temporarySpeedUntil = Time.time + Mathf.Max(0f, duration);
    }

    private float GetTemporarySpeedMultiplier()
    {
        if (Time.time <= temporarySpeedUntil)
            return temporarySpeedMultiplier;

        temporarySpeedMultiplier = 1f;
        return 1f;
    }
}
