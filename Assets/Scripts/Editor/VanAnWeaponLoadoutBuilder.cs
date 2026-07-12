using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class VanAnWeaponLoadoutBuilder
{
    private const string WeaponSocketName = "RightHandWeaponSocket";
    private const string SwordVisualName = "VanAn_SwordVisual";
    private const string CrossbowVisualName = "NoKimQuy_CrossbowVisual";

    [MenuItem("Tools/Dong Chay Anh Hung/Weapons/Install Van An Sword Loadout")]
    public static void InstallOnActivePlayer()
    {
        GameObject player = Selection.activeGameObject;
        if (player == null || player.GetComponent<PlayerController3D>() == null)
            player = FindPlayer();

        if (player == null)
        {
            Debug.LogWarning("VanAnWeaponLoadoutBuilder: Player was not found.");
            return;
        }

        SetupSwordOnPlayer(player);
        Selection.activeGameObject = player;
    }

    public static PlayerWeaponSlot3D SetupOnPlayer(GameObject player)
    {
        return SetupSwordOnPlayer(player);
    }

    public static PlayerWeaponSlot3D SetupSwordOnPlayer(GameObject player)
    {
        if (player == null)
            return null;

        Transform socket = EnsureWeaponSocket(player);
        RemoveExistingChild(socket, CrossbowVisualName);
        GameObject swordVisual = EnsureSwordVisual(socket);

        PlayerWeaponSlot3D weaponSlot = player.GetComponent<PlayerWeaponSlot3D>();
        if (weaponSlot == null)
            weaponSlot = player.AddComponent<PlayerWeaponSlot3D>();

        weaponSlot.Configure(socket, swordVisual, null, PlayerWeaponType3D.Sword);
        EditorUtility.SetDirty(weaponSlot);
        EditorUtility.SetDirty(player);
        return weaponSlot;
    }

    private static Transform EnsureWeaponSocket(GameObject player)
    {
        Transform existing = FindChildRecursive(player.transform, WeaponSocketName);
        Transform parent = FindRightHand(player);
        bool usingHandBone = parent != null;

        if (parent == null)
        {
            Transform visual = player.transform.Find("PlayerVisual");
            parent = visual != null ? visual : player.transform;
        }

        GameObject socketObject;
        if (existing == null)
        {
            socketObject = new GameObject(WeaponSocketName);
            socketObject.transform.SetParent(parent, false);
        }
        else
        {
            socketObject = existing.gameObject;
            if (socketObject.transform.parent != parent)
                socketObject.transform.SetParent(parent, false);
        }

        Transform socket = socketObject.transform;
        if (usingHandBone)
        {
            socket.localPosition = ScaleCompensatedLocalOffset(parent, new Vector3(0.035f, 0.018f, 0.015f));
            socket.localEulerAngles = new Vector3(6f, 84f, 92f);
            socket.localScale = InverseLossyScale(parent);
        }
        else
        {
            socket.localPosition = new Vector3(0.42f, 0.92f, 0.38f);
            socket.localEulerAngles = new Vector3(0f, 78f, -12f);
            socket.localScale = Vector3.one;
        }

        EditorUtility.SetDirty(socketObject);
        return socket;
    }

    private static void RemoveExistingChild(Transform parent, string childName)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        if (child != null)
            Object.DestroyImmediate(child.gameObject);
    }

    private static GameObject EnsureSwordVisual(Transform socket)
    {
        Transform old = socket != null ? socket.Find(SwordVisualName) : null;
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        Material bladeMaterial = NoKimQuyWeaponAssetBuilder.GetOrCreateSwordBladeMaterial();
        Material hiltMaterial = NoKimQuyWeaponAssetBuilder.GetOrCreateSwordHiltMaterial();

        GameObject root = new GameObject(SwordVisualName);
        root.transform.SetParent(socket, false);
        root.transform.localPosition = new Vector3(0.02f, 0.02f, 0f);
        root.transform.localEulerAngles = new Vector3(0f, 0f, -12f);
        root.transform.localScale = Vector3.one;

        CreatePrimitive(root.transform, "Blade", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f), Vector3.zero, new Vector3(0.055f, 0.86f, 0.025f), bladeMaterial);
        CreatePrimitive(root.transform, "Tip", PrimitiveType.Cube, new Vector3(0f, 0.88f, 0f), new Vector3(0f, 0f, 45f), new Vector3(0.058f, 0.058f, 0.027f), bladeMaterial);
        CreatePrimitive(root.transform, "Guard", PrimitiveType.Cube, new Vector3(0f, -0.04f, 0f), Vector3.zero, new Vector3(0.32f, 0.055f, 0.06f), hiltMaterial);
        CreatePrimitive(root.transform, "Grip", PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.055f, 0.18f, 0.055f), hiltMaterial);
        CreatePrimitive(root.transform, "Pommel", PrimitiveType.Sphere, new Vector3(0f, -0.41f, 0f), Vector3.zero, new Vector3(0.11f, 0.11f, 0.11f), hiltMaterial);

        root.SetActive(false);
        StripPhysicsAndColliders(root);
        EnsureVisualAnchor(root, socket, root.transform.localPosition, root.transform.localEulerAngles, root.transform.localScale);
        EditorUtility.SetDirty(root);
        return root;
    }

    private static GameObject CreatePrimitive(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale,
        Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localEulerAngles = localEulerAngles;
        obj.transform.localScale = localScale;

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return obj;
    }

    private static void StripPhysicsAndColliders(GameObject root)
    {
        if (root == null)
            return;

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody body in rigidbodies)
            Object.DestroyImmediate(body);

        Rigidbody2D[] rigidbodies2D = root.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D body in rigidbodies2D)
            Object.DestroyImmediate(body);

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            Object.DestroyImmediate(collider);

        Collider2D[] colliders2D = root.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders2D)
            Object.DestroyImmediate(collider);
    }

    private static void EnsureVisualAnchor(
        GameObject visual,
        Transform socket,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale)
    {
        if (visual == null)
            return;

        WeaponVisualAnchor3D anchor = visual.GetComponent<WeaponVisualAnchor3D>();
        if (anchor == null)
            anchor = visual.AddComponent<WeaponVisualAnchor3D>();

        anchor.Configure(socket, localPosition, localEulerAngles, localScale);
        EditorUtility.SetDirty(anchor);
    }

    private static Vector3 ScaleCompensatedLocalOffset(Transform parent, Vector3 desiredOffset)
    {
        Vector3 scale = SafeLossyScale(parent);
        return new Vector3(desiredOffset.x / scale.x, desiredOffset.y / scale.y, desiredOffset.z / scale.z);
    }

    private static Vector3 InverseLossyScale(Transform parent)
    {
        Vector3 scale = SafeLossyScale(parent);
        return new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
    }

    private static Vector3 SafeLossyScale(Transform transform)
    {
        if (transform == null)
            return Vector3.one;

        Vector3 scale = transform.lossyScale;
        scale.x = Mathf.Approximately(scale.x, 0f) ? 1f : Mathf.Abs(scale.x);
        scale.y = Mathf.Approximately(scale.y, 0f) ? 1f : Mathf.Abs(scale.y);
        scale.z = Mathf.Approximately(scale.z, 0f) ? 1f : Mathf.Abs(scale.z);
        return scale;
    }

    private static Transform FindRightHand(GameObject player)
    {
        Animator animator = player.GetComponentInChildren<Animator>(true);
        if (animator == null || !animator.isHuman)
            return null;

        return animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    private static GameObject FindPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        return player;
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
