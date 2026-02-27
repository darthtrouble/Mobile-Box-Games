using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class UnoDeckManager : MonoBehaviour
{
    [Header("Prefabs & Anchors")]
    public GameObject cardPrefab;
    public Transform drawPileAnchor;
    public Transform discardPileAnchor;

    [Header("Physical Settings")]
    public float cardThickness = 0.016f;

    [Header("Deck State")]
    public List<UnoCard> currentDeck = new List<UnoCard>();
    public bool isDeckReady = false;

    void Awake()
    {
        // 108 cards * 2 tweens = 216 animations. This unlocks DOTween's speed limit!
        DOTween.SetTweensCapacity(1000, 125);
    }

    public void SpawnDeckFromBox(Transform boxTransform)
    {
        if (drawPileAnchor == null)
        {
            Debug.LogError("ERROR: Draw Pile Anchor is missing! Check the Inspector.");
            return;
        }

        isDeckReady = false;

        GenerateDeck();

        // Fisher-Yates Shuffle
        for (int i = currentDeck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            UnoCard temp = currentDeck[i];
            currentDeck[i] = currentDeck[randomIndex];
            currentDeck[randomIndex] = temp;
        }

        Sequence deckSeq = DOTween.Sequence();

        for (int i = 0; i < currentDeck.Count; i++)
        {
            Transform cardTrans = currentDeck[i].transform;

            // 1. Instantly snap to the box
            cardTrans.position = boxTransform.position;
            cardTrans.rotation = boxTransform.rotation;
            cardTrans.SetParent(this.transform);

            // 2. Calculate final resting place
            Vector3 targetPos = drawPileAnchor.position + (drawPileAnchor.up * (i * cardThickness));
            Vector3 targetRot = drawPileAnchor.eulerAngles + new Vector3(0, Random.Range(-2f, 2f), 180);
            
            // 3. Add flight animation (staggered by 0.02 seconds each)
            deckSeq.Insert(i * 0.02f, cardTrans.DOMove(targetPos, 0.6f).SetEase(Ease.OutQuad));
            deckSeq.Insert(i * 0.02f, cardTrans.DORotate(targetRot, 0.6f).SetEase(Ease.OutQuad));
        }

        deckSeq.OnComplete(() => { isDeckReady = true; });
    }

    public void GenerateDeck()
    {
        currentDeck.Clear();
        CardColor[] mainColors = { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };

        foreach (CardColor color in mainColors)
        {
            CreateCard(color, CardType.Zero);
            CardType[] doubles = { CardType.One, CardType.Two, CardType.Three, CardType.Four, CardType.Five, CardType.Six, CardType.Seven, CardType.Eight, CardType.Nine, CardType.Skip, CardType.Reverse, CardType.DrawTwo };
            
            foreach (CardType type in doubles)
            {
                CreateCard(color, type);
                CreateCard(color, type);
            }
        }

        for (int i = 0; i < 4; i++)
        {
            CreateCard(CardColor.Wild, CardType.Wild);
            CreateCard(CardColor.Wild, CardType.WildDrawFour);
        }
    }

    private void CreateCard(CardColor color, CardType type)
    {
        GameObject newCardObj = Instantiate(cardPrefab);
        newCardObj.name = $"Card_{color}_{type}";
        UnoCard cardScript = newCardObj.GetComponent<UnoCard>();
        
        if (cardScript != null)
        {
            cardScript.cardColor = color;
            cardScript.cardType = type;
            cardScript.isFaceUp = false;
        }
        currentDeck.Add(cardScript);
    }

    public UnoCard DrawTopCard()
    {
        if (!isDeckReady || currentDeck.Count == 0) return null;

        UnoCard topCard = currentDeck[currentDeck.Count - 1];
        currentDeck.RemoveAt(currentDeck.Count - 1);
        return topCard;
    }
}