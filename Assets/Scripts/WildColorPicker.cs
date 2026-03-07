using UnityEngine;
using UnityEngine.UI;

public class WildColorPicker : MonoBehaviour
{
    public UnoDeckManager deckManager;
    public GameObject pickerCanvas; // The whole canvas object

    [Header("Color Buttons")]
    public Button btnRed;
    public Button btnBlue;
    public Button btnGreen;
    public Button btnYellow;

    void Start()
    {
        // Wire up the buttons
        btnRed.onClick.AddListener(() => SelectColor(CardColor.Red));
        btnBlue.onClick.AddListener(() => SelectColor(CardColor.Blue));
        btnGreen.onClick.AddListener(() => SelectColor(CardColor.Green));
        btnYellow.onClick.AddListener(() => SelectColor(CardColor.Yellow));
        
        if (pickerCanvas != null) pickerCanvas.SetActive(false);
    }

    public void ShowPicker()
    {
        if (pickerCanvas != null) pickerCanvas.SetActive(true);
    }

    void SelectColor(CardColor chosenColor)
    {
        if (deckManager != null)
        {
            deckManager.activeColor = chosenColor;
            Debug.Log($"<color={chosenColor}>Wild Card Color Selected: {chosenColor}!</color>");
            
            // Visually change the topmost discarded card to the newly selected color!
            if (deckManager.discardPile.Count > 0)
            {
                UnoCard topCard = deckManager.discardPile[deckManager.discardPile.Count - 1];
                if (topCard != null && (topCard.cardType == CardType.Wild || topCard.cardType == CardType.WildDrawFour))
                {
                    topCard.cardColor = chosenColor;
                    topCard.UpdateVisuals();
                }
            }
        }
        
        if (pickerCanvas != null) pickerCanvas.SetActive(false);
    }

}
