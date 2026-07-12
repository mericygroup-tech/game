using UnityEngine;

public class TimeRiftRotate : MonoBehaviour
{
    public float rotateSpeed = 40f;

    private void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime, Space.Self);
    }
}