using UnityEngine;

public enum PlayerWeaponType3D
{
    Sword = 0,
    Crossbow = 1
}

[DisallowMultipleComponent]
public sealed class PlayerWeaponSlot3D : MonoBehaviour
{
    private const string WeaponSocketName = "RightHandWeaponSocket";
    private const string SwordVisualName = "VanAn_SwordVisual";
    private const string CrossbowVisualName = "NoKimQuy_CrossbowVisual";

    private static readonly Vector3 SwordLocalPosition = new Vector3(0.02f, 0.02f, 0f);
    private static readonly Vector3 SwordLocalEulerAngles = new Vector3(0f, 0f, -12f);
    private static readonly Vector3 SwordLocalScale = Vector3.one;
    private static readonly Vector3 CrossbowLocalPosition = new Vector3(0.012f, 0.004f, -0.006f);
    private static readonly Vector3 CrossbowLocalEulerAngles = new Vector3(0f, 0f, -5f);
    private static readonly Vector3 CrossbowLocalScale = Vector3.one * 0.85f;

    [Header("Weapon Visuals")]
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private GameObject swordVisual;
    [SerializeField] private GameObject crossbowVisual;

    [Header("Input")]
    [SerializeField] private bool enableMouseWheelSwap = true;
    [SerializeField] private bool blockSwapWhenInputLocked = true;
    [SerializeField] private float mouseWheelThreshold = 0.08f;
    [SerializeField] private float swapCooldown = 0.12f;

    [Header("Startup")]
    [SerializeField] private PlayerWeaponType3D startingWeapon = PlayerWeaponType3D.Sword;
    [SerializeField] private bool equipStartingWeaponOnAwake = true;

    private PlayerController3D playerController;
    private WeaponVisualAnchor3D swordAnchor;
    private WeaponVisualAnchor3D crossbowAnchor;
    private float nextSwapTime;

    public PlayerWeaponType3D ActiveWeapon { get; private set; }
    public Transform WeaponSocket => weaponSocket;
    public GameObject SwordVisual => swordVisual;
    public GameObject CrossbowVisual => crossbowVisual;
    public bool IsCrossbowEquipped => ActiveWeapon == PlayerWeaponType3D.Crossbow;

    private void Awake()
    {
        playerController = GetComponent<PlayerController3D>();
        ResolveSceneReferences();
        EnsureVisualAnchors();

        if (equipStartingWeaponOnAwake)
            SetWeapon(startingWeapon, true);
    }

    private void Update()
    {
        if (!enableMouseWheelSwap)
            return;

        if (blockSwapWhenInputLocked && playerController != null && playerController.InputLocked)
            return;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) < mouseWheelThreshold || Time.unscaledTime < nextSwapTime)
            return;

        ToggleWeapon();
        nextSwapTime = Time.unscaledTime + Mathf.Max(0f, swapCooldown);
    }

    private void LateUpdate()
    {
        EnsureVisualAnchors();
        swordAnchor?.LockNow();
        crossbowAnchor?.LockNow();
    }

    public void Configure(Transform socket, GameObject sword, GameObject crossbow, PlayerWeaponType3D startingType)
    {
        weaponSocket = socket;
        swordVisual = sword;
        crossbowVisual = crossbow;
        startingWeapon = startingType;
        EnsureVisualAnchors();
        SetWeapon(startingWeapon, true);
    }

    public void ToggleWeapon()
    {
        if (swordVisual == null || crossbowVisual == null)
        {
            SetWeapon(swordVisual != null ? PlayerWeaponType3D.Sword : PlayerWeaponType3D.Crossbow);
            return;
        }

        SetWeapon(ActiveWeapon == PlayerWeaponType3D.Crossbow
            ? PlayerWeaponType3D.Sword
            : PlayerWeaponType3D.Crossbow);
    }

    public void SetWeapon(PlayerWeaponType3D weaponType)
    {
        SetWeapon(weaponType, false);
    }

    public void SetWeapon(PlayerWeaponType3D weaponType, bool force)
    {
        if (!force && ActiveWeapon == weaponType)
            return;

        ActiveWeapon = weaponType;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        if (swordVisual != null)
            swordVisual.SetActive(ActiveWeapon == PlayerWeaponType3D.Sword);

        if (crossbowVisual != null)
            crossbowVisual.SetActive(ActiveWeapon == PlayerWeaponType3D.Crossbow);

        swordAnchor?.LockNow();
        crossbowAnchor?.LockNow();
    }

    private void ResolveSceneReferences()
    {
        if (weaponSocket == null)
            weaponSocket = FindChildRecursive(transform, WeaponSocketName);

        if (swordVisual == null)
        {
            Transform sword = FindChildRecursive(transform, SwordVisualName);
            if (sword != null)
                swordVisual = sword.gameObject;
        }

        if (crossbowVisual == null)
        {
            Transform crossbow = FindChildRecursive(transform, CrossbowVisualName);
            if (crossbow != null)
                crossbowVisual = crossbow.gameObject;
        }
    }

    private void EnsureVisualAnchors()
    {
        if (weaponSocket == null)
            return;

        swordAnchor = EnsureVisualAnchor(swordVisual, SwordLocalPosition, SwordLocalEulerAngles, SwordLocalScale);
        crossbowAnchor = EnsureVisualAnchor(crossbowVisual, CrossbowLocalPosition, CrossbowLocalEulerAngles, CrossbowLocalScale);
    }

    private WeaponVisualAnchor3D EnsureVisualAnchor(
        GameObject visual,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale)
    {
        if (visual == null)
            return null;

        WeaponVisualAnchor3D anchor = visual.GetComponent<WeaponVisualAnchor3D>();
        if (anchor == null)
            anchor = visual.AddComponent<WeaponVisualAnchor3D>();

        anchor.Configure(weaponSocket, localPosition, localEulerAngles, localScale);
        return anchor;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
