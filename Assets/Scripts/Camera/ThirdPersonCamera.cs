using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 4.8f;
    public float height = 4f;
    public float mouseSensitivity = 3f;
    public float smoothSpeed = 8f;
    public bool fixedAngle = true;
    public float fixedYaw = 45f;
    public bool useTargetHeadingWhenFixed = false;
    public float fixedYawOffset = 0f;
    public float fixedPitch = 58f;
    public float lookAtHeight = 1.5f;
    public float shoulderOffset = 0f;
    public bool lockCursor = false;

    [Header("Death View")]
    public float deathYaw = 45f;
    public float deathPitch = 76f;
    public float deathDistance = 6.4f;
    public float deathHeight = 5.9f;
    public float deathLookAtHeight = 0.45f;
    public float deathSmoothSpeed = 4.5f;

    [Header("Vertical Clamp")]
    public float minPitch = -20f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch = 20f;
    private bool deathViewActive;

    private void Start()
    {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;

        Vector3 angles = transform.eulerAngles;
        yaw = fixedAngle ? GetFixedYaw() : angles.y;
        pitch = fixedAngle ? fixedPitch : angles.x;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        GetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation, out float targetSmoothSpeed);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            targetSmoothSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            targetSmoothSpeed * Time.deltaTime
        );
    }

    public void BeginDeathView(Transform deathTarget)
    {
        if (deathTarget != null)
            target = deathTarget;

        deathViewActive = true;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        ResetYawToTargetHeading();
    }

    public void ResetYawToTargetHeading()
    {
        if (target != null && useTargetHeadingWhenFixed)
            yaw = target.eulerAngles.y + fixedYawOffset;
    }

    public void ApplyAdventureFraming(
        float cameraDistance,
        float cameraHeight,
        float downwardPitch,
        float horizontalShoulderOffset,
        float damping)
    {
        distance = Mathf.Max(0.1f, cameraDistance);
        height = cameraHeight;
        fixedPitch = Mathf.Clamp(downwardPitch, minPitch, maxPitch);
        shoulderOffset = horizontalShoulderOffset;
        smoothSpeed = Mathf.Max(0.1f, damping);
        fixedAngle = true;
        useTargetHeadingWhenFixed = true;
        ResetYawToTargetHeading();
    }

    public void SnapToDesiredPose()
    {
        if (target == null)
            return;

        GetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation, out _);
        transform.SetPositionAndRotation(desiredPosition, desiredRotation);
    }

    public void GetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation)
    {
        GetDesiredPose(out desiredPosition, out desiredRotation, out _);
    }

    private void GetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation, out float targetSmoothSpeed)
    {
        if (target == null)
        {
            desiredPosition = transform.position;
            desiredRotation = transform.rotation;
            targetSmoothSpeed = smoothSpeed;
            return;
        }

        float targetYaw;
        float targetPitch;
        float targetDistance;
        float targetHeight;
        float targetLookAtHeight;
        float targetShoulderOffset;

        if (deathViewActive)
        {
            targetYaw = deathYaw;
            targetPitch = deathPitch;
            targetDistance = deathDistance;
            targetHeight = deathHeight;
            targetLookAtHeight = deathLookAtHeight;
            targetShoulderOffset = 0f;
            targetSmoothSpeed = deathSmoothSpeed;
            yaw = targetYaw;
            pitch = targetPitch;
        }
        else if (fixedAngle)
        {
            yaw = GetFixedYaw();
            pitch = fixedPitch;
            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;
            targetHeight = height;
            targetLookAtHeight = lookAtHeight;
            targetShoulderOffset = shoulderOffset;
            targetSmoothSpeed = smoothSpeed;
        }
        else
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;
            targetHeight = height;
            targetLookAtHeight = lookAtHeight;
            targetShoulderOffset = shoulderOffset;
            targetSmoothSpeed = smoothSpeed;
        }

        Quaternion orbitRotation = Quaternion.Euler(targetPitch, targetYaw, 0f);
        Vector3 shoulder = orbitRotation * Vector3.right * targetShoulderOffset;
        Vector3 lookAt = target.position + Vector3.up * targetLookAtHeight + shoulder * 0.35f;

        desiredPosition = target.position - orbitRotation * Vector3.forward * targetDistance;
        desiredPosition.y += targetHeight;
        desiredPosition += shoulder;

        Vector3 lookDirection = lookAt - desiredPosition;
        desiredRotation = lookDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : transform.rotation;
    }

    private float GetFixedYaw()
    {
        if (useTargetHeadingWhenFixed && target != null)
            return target.eulerAngles.y + fixedYawOffset;

        return fixedYaw;
    }
}
