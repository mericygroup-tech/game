using UnityEngine;

[DisallowMultipleComponent]
public sealed class WeaponVisualAnchor3D : MonoBehaviour
{
    [SerializeField] private Transform anchor;
    [SerializeField] private Vector3 lockedLocalPosition;
    [SerializeField] private Vector3 lockedLocalEulerAngles;
    [SerializeField] private Vector3 lockedLocalScale = Vector3.one;
    [SerializeField] private bool lockParent = true;
    [SerializeField] private bool removePhysicsEveryFrame = true;

    private Quaternion lockedLocalRotation = Quaternion.identity;

    public Transform Anchor => anchor;

    private void Awake()
    {
        CacheRotation();
        LockNow();
        RemovePhysics();
    }

    private void OnEnable()
    {
        CacheRotation();
        LockNow();
        RemovePhysics();
    }

    private void FixedUpdate()
    {
        LockNow();
        RemovePhysics();
    }

    private void LateUpdate()
    {
        LockNow();
        if (removePhysicsEveryFrame)
            RemovePhysics();
    }

    public void Configure(Transform targetAnchor, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        anchor = targetAnchor;
        lockedLocalPosition = localPosition;
        lockedLocalEulerAngles = localEulerAngles;
        lockedLocalScale = localScale;
        CacheRotation();
        LockNow();
        RemovePhysics();
    }

    public void LockNow()
    {
        if (anchor != null && lockParent && transform.parent != anchor)
            transform.SetParent(anchor, false);

        transform.localPosition = lockedLocalPosition;
        transform.localRotation = lockedLocalRotation;
        transform.localScale = lockedLocalScale;
    }

    public void RemovePhysics()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody body in rigidbodies)
        {
            if (body == null)
                continue;

            body.useGravity = false;
            body.isKinematic = true;
            body.detectCollisions = false;
            DestroyComponent(body);
        }

        Rigidbody2D[] rigidbodies2D = GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D body in rigidbodies2D)
        {
            if (body == null)
                continue;

            body.gravityScale = 0f;
            body.simulated = false;
            DestroyComponent(body);
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            collider.enabled = false;
            DestroyComponent(collider);
        }

        Collider2D[] colliders2D = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders2D)
        {
            if (collider == null)
                continue;

            collider.enabled = false;
            DestroyComponent(collider);
        }
    }

    private void CacheRotation()
    {
        lockedLocalRotation = Quaternion.Euler(lockedLocalEulerAngles);
    }

    private static void DestroyComponent(Component component)
    {
        if (component == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(component);
            return;
        }
#endif

        Destroy(component);
    }
}
