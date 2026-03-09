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

    [Header("Text References")]
    public TMPro.TMP_Text centerText;
    public TMPro.TMP_Text topLeftText;
    public TMPro.TMP_Text botRightText;

    [Header("Icon References")]
    public SpriteRenderer centerIcon;
    public SpriteRenderer topLeftIcon;
    public SpriteRenderer botRightIcon;

    [Header("Action Sprites")]
    public Sprite skipSprite;
    public Sprite reverseSprite;
    public Sprite drawTwoSprite;
    public Sprite wildSprite;
    public Sprite wildDrawFourSprite;

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

        // 2. Toggle Text vs Icons
        bool isNumber = (cardType == CardType.Number);
        
        // Toggle GameObjects
        if (centerText != null) centerText.gameObject.SetActive(isNumber);
        if (topLeftText != null) topLeftText.gameObject.SetActive(isNumber);
        if (botRightText != null) botRightText.gameObject.SetActive(isNumber);
        
        if (centerIcon != null) centerIcon.gameObject.SetActive(!isNumber);
        if (topLeftIcon != null) topLeftIcon.gameObject.SetActive(!isNumber);
        if (botRightIcon != null) botRightIcon.gameObject.SetActive(!isNumber);

        // 3. Assign Content
        if (isNumber)
        {
            string val = cardValue.ToString();
            if (centerText != null) centerText.text = val;
            if (topLeftText != null) topLeftText.text = val;
            if (botRightText != null) botRightText.text = val;
        }
        else
        {
            Sprite chosenSprite = null;
            switch (cardType)
            {
                case CardType.Skip: chosenSprite = skipSprite; break;
                case CardType.Reverse: chosenSprite = reverseSprite; break;
                case CardType.DrawTwo: chosenSprite = drawTwoSprite; break;
                case CardType.Wild: chosenSprite = wildSprite; break;
                case CardType.WildDrawFour: chosenSprite = wildDrawFourSprite; break;
            }

            if (centerIcon != null) centerIcon.sprite = chosenSprite;
            if (topLeftIcon != null) topLeftIcon.sprite = chosenSprite;
            if (botRightIcon != null) botRightIcon.sprite = chosenSprite;
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

            // Hide the text and icons
            if (centerText != null) centerText.enabled = false;
            if (topLeftText != null) topLeftText.enabled = false;
            if (botRightText != null) botRightText.enabled = false;

            if (centerIcon != null) centerIcon.enabled = false;
            if (topLeftIcon != null) topLeftIcon.enabled = false;
            if (botRightIcon != null) botRightIcon.enabled = false;
        }
        else
        {
            // Restore text and icons
            if (centerText != null) centerText.enabled = true;
            if (topLeftText != null) topLeftText.enabled = true;
            if (botRightText != null) botRightText.enabled = true;

            if (centerIcon != null) centerIcon.enabled = true;
            if (topLeftIcon != null) topLeftIcon.enabled = true;
            if (botRightIcon != null) botRightIcon.enabled = true;

            UpdateVisuals();
        }
    }
}
