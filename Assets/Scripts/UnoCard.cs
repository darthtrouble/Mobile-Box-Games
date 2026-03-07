using UnityEngine;
using TMPro;

public enum CardColor { Red, Blue, Green, Yellow, Wild }
public enum CardType { Number, Skip, Reverse, DrawTwo, Wild, WildDrawFour }

public class UnoCard : MonoBehaviour
{
    [Header("Card Data")]
    public CardColor cardColor;
    public CardType cardType;
    public int cardValue = -1;
    public bool isFaceUp = false;

    [Header("Visual References")]
    public MeshRenderer cardMeshRenderer; 

    [Tooltip("Look at your MeshRenderer's Materials list. Which Element (0, 1, 2...) is the front face?")]
    public int frontMaterialIndex = 0;

    public TextMeshPro cardText;

    public void SetupCard(CardColor color, CardType type, int value)
    {
        cardColor = color;
        cardType = type;
        cardValue = value;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        // 1. Calculate the color
        Color faceColor = Color.white;
        switch (cardColor)
        {
            case CardColor.Red: faceColor = new Color(0.9f, 0.2f, 0.2f); break;
            case CardColor.Blue: faceColor = new Color(0.2f, 0.4f, 0.9f); break;
            case CardColor.Green: faceColor = new Color(0.2f, 0.8f, 0.3f); break;
            case CardColor.Yellow: faceColor = new Color(0.9f, 0.8f, 0.1f); break;
            case CardColor.Wild: faceColor = new Color(0.2f, 0.2f, 0.2f); break; 
        }
        
        // 2. Safely apply color ONLY to the targeted ProBuilder material index
        if (cardMeshRenderer != null)
        {
            Material[] cardMaterials = cardMeshRenderer.materials;
            if (frontMaterialIndex >= 0 && frontMaterialIndex < cardMaterials.Length)
            {
                cardMaterials[frontMaterialIndex].color = faceColor;
                cardMeshRenderer.materials = cardMaterials; // Reassign the array to apply the instance changes
            }
        }

        // 3. Set the text with Auto-Sizing
        if (cardText != null)
        {
            cardText.enableAutoSizing = true;
            cardText.fontSizeMin = 5f;  
            cardText.fontSizeMax = 72f; 

            switch (cardType)
            {
                case CardType.Number: cardText.text = cardValue.ToString(); break;
                case CardType.Skip: cardText.text = "SKIP"; break;
                case CardType.Reverse: cardText.text = "REV"; break;
                case CardType.DrawTwo: cardText.text = "+2"; break;
                case CardType.Wild: cardText.text = "WILD"; break;
                case CardType.WildDrawFour: cardText.text = "+4"; break;
            }
        }
    }

    public void SetBlackoutMode(bool isBlackout)
    {
        if (isBlackout)
        {
            // Black out the front face
            if (cardMeshRenderer != null)
            {
                Material[] cardMaterials = cardMeshRenderer.materials;
                if (frontMaterialIndex >= 0 && frontMaterialIndex < cardMaterials.Length)
                {
                    cardMaterials[frontMaterialIndex].color = Color.black;
                    cardMeshRenderer.materials = cardMaterials;
                }
            }

            // Hide the text
            if (cardText != null)
            {
                cardText.enabled = false;
            }
        }
        else
        {
            // Restore text and color
            if (cardText != null)
            {
                cardText.enabled = true;
            }
            UpdateVisuals();
        }
    }
}
