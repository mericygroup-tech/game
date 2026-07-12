using UnityEngine;

public class TimeRiftPulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.15f;

    private Vector3 startScale;

    private void Start()
    {
        startScale = transform.localScale;
    }

    private void Update()
    {
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = startScale + Vector3.one * scaleOffset;
    }
}