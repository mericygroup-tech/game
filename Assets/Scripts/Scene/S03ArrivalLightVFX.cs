using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class S03ArrivalLightVFX : MonoBehaviour
{
    [Header("Beam")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField, Min(0.02f)] private float beamWidth = 0.16f;
    [SerializeField] private Color beamColor = new Color(0.85f, 0.96f, 1f, 0.92f);

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float travelDuration = 0.55f;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.32f;

    [Header("Impact")]
    [SerializeField] private Light impactLight;
    [SerializeField, Min(0f)] private float impactLightIntensity = 5.5f;
    [SerializeField, Min(0f)] private float impactLightRange = 7f;
    [SerializeField] private ParticleSystem impactParticles;

    private Material runtimeLineMaterial;

    private void Awake()
    {
        EnsureLineRenderer();
        ClearEffect();
    }

    private void OnDisable()
    {
        ClearEffect();
    }

    private void OnDestroy()
    {
        if (runtimeLineMaterial != null)
            Destroy(runtimeLineMaterial);
    }

    public IEnumerator PlayArrival(Vector3 startPosition, Vector3 impactPosition)
    {
        yield return PlayArrival(startPosition, impactPosition, null);
    }

    public IEnumerator PlayArrival(Vector3 startPosition, Vector3 impactPosition, Action onImpact)
    {
        EnsureLineRenderer();

        if (lineRenderer != null)
        {
            lineRenderer.gameObject.SetActive(true);
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth * 0.65f;
        }

        if (impactLight != null)
        {
            impactLight.enabled = false;
            impactLight.intensity = 0f;
            impactLight.range = impactLightRange;
        }

        float elapsed = 0f;
        float safeTravelDuration = Mathf.Max(0.05f, travelDuration);
        while (elapsed < safeTravelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeTravelDuration));
            Vector3 beamEnd = Vector3.Lerp(startPosition, impactPosition, t);
            SetBeam(startPosition, beamEnd, beamColor);
            yield return null;
        }

        SetBeam(startPosition, impactPosition, beamColor);
        onImpact?.Invoke();

        if (impactParticles != null)
        {
            impactParticles.transform.position = impactPosition;
            impactParticles.Play(true);
        }

        if (impactLight != null)
        {
            impactLight.transform.position = impactPosition + Vector3.up * 0.6f;
            impactLight.enabled = true;
        }

        elapsed = 0f;
        float safeFlashDuration = Mathf.Max(0.05f, flashDuration);
        while (elapsed < safeFlashDuration)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - Mathf.Clamp01(elapsed / safeFlashDuration);
            Color fadedColor = new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a * remaining);
            SetBeam(startPosition, impactPosition, fadedColor);

            if (impactLight != null)
                impactLight.intensity = impactLightIntensity * remaining;

            yield return null;
        }

        ClearEffect();
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.numCapVertices = 4;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            runtimeLineMaterial = new Material(shader)
            {
                name = "Runtime_S03_ArrivalBeam"
            };

            if (runtimeLineMaterial.HasProperty("_BaseColor"))
                runtimeLineMaterial.SetColor("_BaseColor", beamColor);
            if (runtimeLineMaterial.HasProperty("_Color"))
                runtimeLineMaterial.SetColor("_Color", beamColor);

            lineRenderer.sharedMaterial = runtimeLineMaterial;
        }
    }

    private void SetBeam(Vector3 startPosition, Vector3 endPosition, Color color)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private void ClearEffect()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        if (impactLight != null)
        {
            impactLight.intensity = 0f;
            impactLight.enabled = false;
        }
    }
}
