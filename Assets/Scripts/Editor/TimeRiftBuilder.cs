using UnityEditor;
using UnityEngine;

public class TimeRiftBuilder : MonoBehaviour
{
    public static void CreateTimeRift()
    {
        GameObject oldRift = GameObject.Find("TimeRift");
        if (oldRift != null)
        {
            DestroyImmediate(oldRift);
        }

        GameObject timeRift = new GameObject("TimeRift");
        timeRift.transform.position = new Vector3(0f, 0f, 22f);

        Material stoneMat = CreateMaterial(
            "Mat_RiftBase_Stone",
            new Color32(55, 50, 60, 255),
            false
        );

        Material cyanMat = CreateMaterial(
            "Mat_PortalSurface_Cyan",
            new Color32(0, 220, 255, 200),
            true,
            new Color32(0, 255, 255, 255)
        );

        Material purpleMat = CreateMaterial(
            "Mat_RiftRing_Purple",
            new Color32(160, 50, 255, 255),
            true,
            new Color32(190, 80, 255, 255)
        );

        Material orbMat = CreateMaterial(
            "Mat_RiftOrb_Cyan",
            new Color32(0, 255, 255, 255),
            true,
            new Color32(0, 255, 255, 255)
        );

        CreateCylinder(
            "TimeRift_Base",
            timeRift.transform,
            new Vector3(0f, 0.25f, 0f),
            Vector3.zero,
            new Vector3(2.8f, 0.25f, 2.8f),
            stoneMat
        );

        CreatePlane(
            "TimeRift_PortalSurface",
            timeRift.transform,
            new Vector3(0f, 2f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(0.25f, 1f, 0.35f),
            cyanMat
        );

        CreatePlane(
            "TimeRift_PortalSurface_02",
            timeRift.transform,
            new Vector3(0f, 2f, 0.03f),
            new Vector3(90f, 0f, 25f),
            new Vector3(0.20f, 1f, 0.30f),
            cyanMat
        );

        GameObject ring = CreateCylinder(
            "TimeRift_Ring",
            timeRift.transform,
            new Vector3(0f, 2f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(2.6f, 0.06f, 2.6f),
            purpleMat
        );

        AddRotateScript(ring, 40f);

        CreateStone("RiftStone_01", timeRift.transform, new Vector3(-1.7f, 0.9f, 0f), new Vector3(0f, 0f, 20f), new Vector3(0.35f, 1.2f, 0.35f), stoneMat);
        CreateStone("RiftStone_02", timeRift.transform, new Vector3(1.7f, 0.9f, 0f), new Vector3(0f, 0f, -20f), new Vector3(0.35f, 1.2f, 0.35f), stoneMat);
        CreateStone("RiftStone_03", timeRift.transform, new Vector3(-0.9f, 3.3f, 0f), new Vector3(0f, 0f, -35f), new Vector3(0.25f, 0.8f, 0.25f), stoneMat);
        CreateStone("RiftStone_04", timeRift.transform, new Vector3(0.9f, 3.3f, 0f), new Vector3(0f, 0f, 35f), new Vector3(0.25f, 0.8f, 0.25f), stoneMat);

        CreateOrb("RiftOrb_01", timeRift.transform, new Vector3(1.3f, 2.7f, 0f), 0.15f, orbMat);
        CreateOrb("RiftOrb_02", timeRift.transform, new Vector3(-1.2f, 1.5f, 0f), 0.12f, orbMat);
        CreateOrb("RiftOrb_03", timeRift.transform, new Vector3(0.4f, 3.1f, 0f), 0.1f, orbMat);

        GameObject lightObj = new GameObject("TimeRift_Light");
        lightObj.transform.SetParent(timeRift.transform);
        lightObj.transform.localPosition = new Vector3(0f, 2.3f, -1f);

        Light pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color32(120, 50, 255, 255);
        pointLight.range = 12f;
        pointLight.intensity = 4f;

        Selection.activeGameObject = timeRift;

        Debug.Log("Đã tạo TimeRift portal tại vị trí Z = 22.");
    }

    private static Material CreateMaterial(string name, Color baseColor, bool emission, Color? emissionColor = null)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;
        mat.color = baseColor;

        if (emission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor ?? baseColor);
        }

        return mat;
    }

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = rot;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = mat;
        return obj;
    }

    private static GameObject CreatePlane(string name, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = rot;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = mat;
        return obj;
    }

    private static GameObject CreateStone(string name, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = rot;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = mat;
        return obj;
    }

    private static GameObject CreateOrb(string name, Transform parent, Vector3 pos, float size, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = Vector3.one * size;
        obj.GetComponent<Renderer>().material = mat;
        return obj;
    }

    private static void AddRotateScript(GameObject target, float speed)
    {
        TimeRiftRotate rotate = target.GetComponent<TimeRiftRotate>();

        if (rotate == null)
        {
            rotate = target.AddComponent<TimeRiftRotate>();
        }

        rotate.rotateSpeed = speed;
    }
}
