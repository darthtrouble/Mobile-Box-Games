using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class CameraBob : MonoBehaviour
{
    [Header("Bob Settings")]
    [Tooltip("Speed of the breathing sway.")]
    public float bobSpeed = 0.5f;
    [Tooltip("Intensity of the positional sway.")]
    public float positionalIntensity = 0.05f;
    [Tooltip("Intensity of the rotational sway.")]
    public float rotationalIntensity = 0.5f;

    [Header("Zoom Settings")]
    [Tooltip("Default Field of View.")]
    public float defaultFOV = 60f;
    [Tooltip("Field of View when zoomed in (Game Selection / Lobby).")]
    public float zoomedFOV = 50f;
    [Tooltip("Duration of the zoom transition.")]
    public float zoomDuration = 0.8f;

    private Camera _cam;
    [HideInInspector] public Vector3 initialPosition;
    [HideInInspector] public Vector3 initialRotation;
    public bool isEnabled = true;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        initialPosition = transform.localPosition;
        initialRotation = transform.localEulerAngles;
    }

    private void Update()
    {
        if (!isEnabled) return;

        float time = Time.time * bobSpeed;
        float pX = (Mathf.PerlinNoise(time, 1f) - 0.5f) * 2f;
        float pY = (Mathf.PerlinNoise(1f, time) - 0.5f) * 2f;
        float rX = (Mathf.PerlinNoise(time + 10f, 1f) - 0.5f) * 2f;
        float rY = (Mathf.PerlinNoise(1f, time + 10f) - 0.5f) * 2f;

        // CHANGE: Instead of "InitialPosition + offset", we just set the local offset.
        // This allows the Parent (Neck) to move the camera freely.
        transform.localPosition = new Vector3(pX, pY, 0) * positionalIntensity;
        transform.localRotation = Quaternion.Euler(new Vector3(rX, rY, 0) * rotationalIntensity);
    }

    /// <summary>
    /// Smoothly transitions the camera's FOV.
    /// </summary>
    public void SetZoom(bool isZoomed)
    {
        if (_cam == null) return;
        
        float targetFOV = isZoomed ? zoomedFOV : defaultFOV;
        
        // Kill existing FOV tweens to prevent overlapping stutters
        _cam.DOKill(); 
        _cam.DOFieldOfView(targetFOV, zoomDuration).SetEase(Ease.InOutSine);
    }
}
