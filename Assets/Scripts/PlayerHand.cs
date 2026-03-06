using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.InputSystem;

public class PlayerHand : MonoBehaviour
{
    public UnoDeckManager deckManager;
    public List<UnoCard> cardsInHand = new List<UnoCard>();

    [Header("Fan Settings")]
    public float cardSpacing = 0.8f; 
    public float arcCurve = 0.15f;
    public float fanSpread = 25f; 
    public float inwardTilt = 20f; 
    public float depthOffset = 0.005f; 

    [Header("Hover Settings")]
    public float hoverHeight = 0.3f;
    public float neighborHoverHeight = 0.1f;
    public float hoverPullForward = -0.1f; 

    public bool isLocalPlayer = false;
    private int hoveredIndex = -1;

    void Update()
    {
        if (!isLocalPlayer || cardsInHand.Count == 0 || Camera.main == null || Mouse.current == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray);

        int newHoveredIndex = -1;
        bool currentStillHovered = false;

        if (hoveredIndex != -1)
        {
            foreach (RaycastHit hit in hits)
            {
                // Use GetComponentInParent so it detects the root collider OR the visual child collider!
                UnoCard hitCard = hit.collider.GetComponentInParent<UnoCard>();
                if (hitCard != null && cardsInHand.IndexOf(hitCard) == hoveredIndex)
                {
                    currentStillHovered = true;
                    newHoveredIndex = hoveredIndex;
                    break;
                }
            }
        }

        if (!currentStillHovered)
        {
            int highestIndexHit = -1;
            foreach (RaycastHit hit in hits)
            {
                UnoCard hitCard = hit.collider.GetComponentInParent<UnoCard>();
                if (hitCard != null && cardsInHand.Contains(hitCard))
                {
                    int idx = cardsInHand.IndexOf(hitCard);
                    if (idx > highestIndexHit) highestIndexHit = idx;
                }
            }
            newHoveredIndex = highestIndexHit;
        }

        if (newHoveredIndex != hoveredIndex)
        {
            hoveredIndex = newHoveredIndex;
            UpdateHandVisuals();
        }

        UnoCard hoveredCard = hoveredIndex != -1 && hoveredIndex < cardsInHand.Count ? cardsInHand[hoveredIndex] : null;

        // Left Click to Play Card
        if (Mouse.current.leftButton.wasPressedThisFrame && hoveredCard != null)
        {
            if (deckManager != null)
            {
                deckManager.PlayCard(hoveredCard, this);
                hoveredIndex = -1; // Clear the hover state so it doesn't get stuck
            }
            else
            {
                Debug.LogWarning("Deck Manager is not assigned to the PlayerHand!");
            }
        }
    }

    public void AddCard(UnoCard newCard)
    {
        cardsInHand.Add(newCard);
        newCard.isFaceUp = true;
        newCard.transform.SetParent(this.transform);

        // NEW FIX: Kill Deck Manager tweens ONLY when the card first enters the hand
        newCard.transform.DOKill(); 
        
        if (newCard.transform.childCount > 0)
        {
            newCard.transform.GetChild(0).localPosition = Vector3.zero;
        }
        
        UpdateHandVisuals();
    }

    public void UpdateHandVisuals()
    {
        // Dynamically grow the fan width and angle, but severely cap them so it doesn't escape the screen
        // Allows exactly enough space per additional card to see the top-left corner
        float currentSpacing = Mathf.Min(cardSpacing + (cardsInHand.Count * 0.12f), 2.1f); // Lower Max Width Cap
        float currentFanSpread = Mathf.Min(fanSpread + (cardsInHand.Count * 1.5f), 45f);   // Max Tilt Angle Cap
        float currentArcCurve = Mathf.Min(arcCurve + (cardsInHand.Count * 0.03f), 0.55f);  // Slightly higher Drop Cap

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            float normalizedPos = (cardsInHand.Count <= 1) ? 0f : ((float)i / (cardsInHand.Count - 1)) - 0.5f;
            
            // 1. Animate the ROOT object to restore the flying draw animation
            // X: Pushes the outer cards down to create the arc (formerly Y)
            float targetX = Mathf.Abs(normalizedPos) * -currentArcCurve;
            // Y: pulls each subsequent card slightly CLOSER to the camera (formerly Z)
            float targetY = i * -depthOffset;
            // Z: Left-to-right spread (formerly X)
            float targetZ = normalizedPos * currentSpacing;

            Vector3 basePos = new Vector3(targetX, targetY, targetZ);
                
            // Apply the new Inward Tilt on the Y-axis so they look at the camera
            // X-axis handles the new inward face tilt, Y handles the spread tilt.
            Vector3 baseRot = new Vector3(Mathf.Abs(normalizedPos) * inwardTilt, normalizedPos * -currentFanSpread, 0);

            cardsInHand[i].transform.DOLocalMove(basePos, 0.4f).SetEase(Ease.OutBack);
            cardsInHand[i].transform.DOLocalRotate(baseRot, 0.4f).SetEase(Ease.OutBack);

            // 2. Animate ONLY the visual child for the snappy hover effect
            if (cardsInHand[i].transform.childCount > 0)
            {
                Transform visual = cardsInHand[i].transform.GetChild(0);
                visual.DOKill(); // Kill visual tweens to prevent jitter
                
                Vector3 targetVisualPos = Vector3.zero;

                if (hoveredIndex != -1)
                {
                    int distance = Mathf.Abs(i - hoveredIndex);
                    if (distance == 0) 
                    {
                        // Strictly pop UP (local X) and FORWARD (local Y). Z remains 0 so it stays on screen!
                        targetVisualPos = new Vector3(hoverHeight, hoverPullForward, 0); 
                    }
                    else if (distance == 1) 
                    {
                        // Neighbors do a mini-pop straight up
                        targetVisualPos = new Vector3(neighborHoverHeight, hoverPullForward * 0.5f, 0);
                    }
                }

                visual.DOLocalMove(targetVisualPos, 0.2f).SetEase(Ease.OutQuad);
            }
        }
    }
}
