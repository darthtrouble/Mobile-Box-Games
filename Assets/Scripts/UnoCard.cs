using UnityEngine;

public enum CardColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Wild
}

public enum CardType
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Skip,
    Reverse,
    DrawTwo,
    Wild,
    WildDrawFour
}

public class UnoCard : MonoBehaviour
{
    public CardColor cardColor;
    public CardType cardType;
    public bool isFaceUp = false;

    public string GetCardName()
    {
        return cardColor.ToString() + " " + cardType.ToString();
    }
}
