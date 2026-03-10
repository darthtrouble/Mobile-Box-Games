using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class HumanNeckController : MonoBehaviour
{
    [Header("Anchors")]
    public Transform handAnchor; // The "camera player anchor" (35-degree tilt)
    public UnoDeckManager deckManager;

    [Header("Look Limits")]
    public float sensitivity = 0.15f;
    public Vector2 xLimit = new Vector2(-40f, 40f); // Up/Down
    public Vector2 yLimit = new Vector2(-70f, 70f); // Left/Right

    [Tooltip("Adjust this if your 'Forward' is facing the wrong way (e.g., 90 or -90)")]
    public float yawOffset = 0f; 

    [Header("Status")]
    public bool gameStarted = false;
    public bool isFreeLooking = true; // Start in human eye-level view

    private float pitch = 0f;
    private float yaw = 0f;

    // Call this when the "Start Game" paper is clicked
    public void OnGameStart() 
    {
        gameStarted = true;
        EnterFreeLook();
    }

    void Update()
    {
        if (!gameStarted) return;

        // Toggle state with Right Mouse Click
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isFreeLooking = !isFreeLooking;
            if (isFreeLooking) EnterFreeLook();
            else EnterHandView();
        }

        if (isFreeLooking)
        {
            HandleFreeLook();
        }
        else
        {
            HandleHandView();
        }
    }

    void HandleFreeLook()
    {
        // LOCK: If we are looking around, tell the DeckManager to ignore card clicks
        // (Assuming you have a 'canInteract' bool in your DeckManager)
        if(deckManager != null) deckManager.canInteract = false;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * sensitivity;
        pitch -= delta.y * sensitivity;
        yaw = Mathf.Clamp(yaw, yLimit.x, yLimit.y);
        pitch = Mathf.Clamp(pitch, xLimit.x, xLimit.y);

        // FIX: Use localRotation to prevent the camera from "flying away"
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(pitch, yaw + yawOffset, 0), Time.deltaTime * 15f);
        
        // Keep position locked to the handAnchor height
        if(handAnchor != null)
            transform.position = Vector3.Lerp(transform.position, handAnchor.position, Time.deltaTime * 10f);
    }

    void HandleHandView()
    {
        if (handAnchor == null) return;

        // Smoothly transition to the specific hand view (usually X: 35)
        transform.position = Vector3.Lerp(transform.position, handAnchor.position, Time.deltaTime * 8f);
        transform.rotation = Quaternion.Slerp(transform.rotation, handAnchor.rotation, Time.deltaTime * 8f);
        
        // Reset internal pitch/yaw so when we switch back to free-look, we start at 0
        pitch = 0;
        yaw = 0;
    }

    void EnterFreeLook()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void EnterHandView()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if(deckManager != null) deckManager.canInteract = true;
    }
}
