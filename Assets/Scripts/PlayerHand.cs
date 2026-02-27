using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerHand : MonoBehaviour
{
    public List<UnoCard> cardsInHand = new List<UnoCard>();

    [Header("Fan Settings")]
    public float cardSpacing = 0.8f; 
    public float arcCurve = 0.15f;
    public float fanSpread = 25f; 

    [Header("Layering")]
    [Tooltip("How much each card steps forward to overlap the previous one")]
    public float depthOffset = 0.005f;

    public void AddCard(UnoCard newCard)
    {
        cardsInHand.Add(newCard);
        newCard.isFaceUp = true;
        newCard.transform.SetParent(this.transform);
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
            
            // X: Pushes the outer cards down to create the arc (formerly Y)
            // Y: i * -depthOffset pulls each subsequent card slightly CLOSER to the camera, overlapping the left cards (formerly Z)
            // Z: Left-to-right spread (formerly X)
            Vector3 targetLocalPos = new Vector3(
                Mathf.Abs(normalizedPos) * -currentArcCurve, 
                i * -depthOffset, 
                normalizedPos * currentSpacing);
                
            // Y-Axis Rotation: Tilts the cards like a steering wheel (formerly Z).
            Vector3 targetLocalRot = new Vector3(0, normalizedPos * -currentFanSpread, 0);

            cardsInHand[i].transform.DOLocalMove(targetLocalPos, 0.4f).SetEase(Ease.OutCubic);
            cardsInHand[i].transform.DOLocalRotate(targetLocalRot, 0.4f).SetEase(Ease.OutCubic);
        }
    }
}
