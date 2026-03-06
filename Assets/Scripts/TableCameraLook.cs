using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class TableCameraLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float lookSpeed = 0.15f;
    public float maxYaw = 45f;

    public float maxPitch = 20f;

    [Header("Game State")]
    public bool canLook = false; // Locked until dealing is done!

    private Quaternion baseRotation;
    private Vector2 currentLook;
    private bool isLooking = false;

    void Update()
    {
        if (!canLook || Keyboard.current == null || Mouse.current == null) return;

        if (Keyboard.current.altKey.wasPressedThisFrame)
        {
            baseRotation = transform.rotation;
            currentLook = Vector2.zero;
            isLooking = true;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            transform.DOKill(); 
        }

        if (Keyboard.current.altKey.isPressed && isLooking)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            currentLook.x += delta.x * lookSpeed;
            currentLook.y -= delta.y * lookSpeed; 

            currentLook.x = Mathf.Clamp(currentLook.x, -maxYaw, maxYaw);
            currentLook.y = Mathf.Clamp(currentLook.y, -maxPitch, maxPitch);

            transform.rotation = baseRotation * Quaternion.Euler(currentLook.y, currentLook.x, 0);
        }

        if (Keyboard.current.altKey.wasReleasedThisFrame && isLooking)
        {
            isLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            transform.DORotateQuaternion(baseRotation, 0.4f).SetEase(Ease.OutQuad);
        }
    }

}
