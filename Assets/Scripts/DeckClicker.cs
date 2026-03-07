using UnityEngine;
using UnityEngine.InputSystem;

public class DeckClicker : MonoBehaviour
{
    public UnoDeckManager deckManager;
    public PlayerHand localPlayerHand;
    public Camera mainCam;

    void Update()
    {
        if (deckManager == null || localPlayerHand == null || mainCam == null) return;

        // Perform raycast on click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if the clicked object is THIS deck's collider
                if (hit.collider.gameObject == this.gameObject)
                {
                    deckManager.HandleDeckClick(localPlayerHand);
                }
            }
        }
    }
}
