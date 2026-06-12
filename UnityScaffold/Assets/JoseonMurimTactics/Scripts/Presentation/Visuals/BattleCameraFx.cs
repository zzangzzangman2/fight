using System.Collections;
using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleCameraFx : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float focusLerpSeconds = 0.16f;

    private Coroutine shakeRoutine;
    private Coroutine zoomRoutine;

    private Camera CameraTarget => targetCamera != null ? targetCamera : Camera.main;

    public Coroutine FocusWorldPosition(Vector3 worldPosition)
    {
        Camera cam = CameraTarget;
        return cam == null ? null : StartCoroutine(FocusRoutine(cam, worldPosition));
    }

    public Coroutine Shake(float amplitude = 0.08f, float seconds = 0.12f)
    {
        Camera cam = CameraTarget;
        if (cam == null)
        {
            return null;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine(cam, Mathf.Max(0f, amplitude), Mathf.Max(0.01f, seconds)));
        return shakeRoutine;
    }

    public Coroutine PulseZoom(float delta = -0.12f, float seconds = 0.14f)
    {
        Camera cam = CameraTarget;
        if (cam == null || !cam.orthographic)
        {
            return null;
        }

        if (zoomRoutine != null)
        {
            StopCoroutine(zoomRoutine);
        }

        zoomRoutine = StartCoroutine(PulseZoomRoutine(cam, delta, Mathf.Max(0.01f, seconds)));
        return zoomRoutine;
    }

    private IEnumerator FocusRoutine(Camera cam, Vector3 worldPosition)
    {
        Vector3 start = cam.transform.position;
        Vector3 end = new Vector3(worldPosition.x, worldPosition.y, start.z);
        float elapsed = 0f;
        while (elapsed < focusLerpSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, focusLerpSeconds));
            cam.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }

    private IEnumerator ShakeRoutine(Camera cam, float amplitude, float seconds)
    {
        Vector3 origin = cam.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / seconds);
            cam.transform.localPosition = origin + new Vector3(Random.Range(-amplitude, amplitude) * fade,
                                                               Random.Range(-amplitude, amplitude) * fade,
                                                               0f);
            yield return null;
        }

        cam.transform.localPosition = origin;
        shakeRoutine = null;
    }

    private IEnumerator PulseZoomRoutine(Camera cam, float delta, float seconds)
    {
        float start = cam.orthographicSize;
        float target = Mathf.Max(0.1f, start + delta);
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float phase = Mathf.Sin(Mathf.Clamp01(elapsed / seconds) * Mathf.PI);
            cam.orthographicSize = Mathf.Lerp(start, target, phase);
            yield return null;
        }

        cam.orthographicSize = start;
        zoomRoutine = null;
    }
}
}
