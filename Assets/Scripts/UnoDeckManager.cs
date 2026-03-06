using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class UnoDeckManager : MonoBehaviour
{
    [Header("Prefabs & Anchors")]
    public GameObject cardPrefab;
    public Transform drawPileAnchor;
    public Transform discardPileAnchor;
    public TableCameraLook tableCameraLook;

    [Header("Physical Settings")]
    public float cardThickness = 0.016f;

    [Header("Deck State")]
    public List<UnoCard> currentDeck = new List<UnoCard>();
    public List<UnoCard> discardPile = new List<UnoCard>(); // NEW
    public bool isDeckReady = false;

    [Header("Multiplayer Settings")]
    [Range(2, 6)]
    public int playerCount = 2; // Default for testing
    
    [Tooltip("Assign in this priority: 1(Me), 2(Top), 3(TopRight), 4(TopLeft), 5(BotRight), 6(BotLeft)")]
    public List<PlayerHand> seatingPriority = new List<PlayerHand>();
    
    private List<PlayerHand> activePlayers = new List<PlayerHand>();

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

    public void StartUnoGame()
    {
        // Setup the active players based on the player count
        activePlayers.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            if (i < seatingPriority.Count)
            {
                activePlayers.Add(seatingPriority[i]);
            }
        }
        
        StartCoroutine(DealStartingSequence());
    }

    private System.Collections.IEnumerator DealStartingSequence()
    {
        Debug.Log("Starting Deal for " + activePlayers.Count + " players.");
        yield return new WaitForSeconds(2.0f);

        // Deal 7 cards to each active player in a circle
        for (int round = 0; round < 7; round++)
        {
            foreach (PlayerHand player in activePlayers)
            {
                UnoCard c = DrawTopCard();
                if (c != null) 
                {
                    // If it's an opponent, we probably want the cards face DOWN. 
                    // We will temporarily leave them face UP for your testing so you can see the fans work!
                    player.AddCard(c); 
                }
                yield return new WaitForSeconds(0.1f); // Faster deal since there are more players
            }
        }

        yield return new WaitForSeconds(0.5f);
        
        UnoCard discardCard = DrawTopCard();
        if (discardCard != null)
        {
            discardPile.Add(discardCard);
            discardCard.isFaceUp = true;
            discardCard.transform.SetParent(discardPileAnchor);
            discardCard.transform.DOKill();
            discardCard.transform.DOLocalMove(Vector3.zero, 0.4f).SetEase(Ease.OutBack);
            float randomMessyTilt = UnityEngine.Random.Range(-15f, 15f);
            discardCard.transform.DOLocalRotate(new Vector3(0, 0, randomMessyTilt), 0.4f).SetEase(Ease.OutBack); 
        }

        if (tableCameraLook != null) tableCameraLook.canLook = true;
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
        // Keep digging through the deck until we find a real card or run out
        while (currentDeck.Count > 0)
        {
            int lastIndex = currentDeck.Count - 1;
            UnoCard topCard = currentDeck[lastIndex];
            
            // Remove the slot from the list regardless of what's inside
            currentDeck.RemoveAt(lastIndex);
            
            // If the card is real and hasn't been destroyed, return it!
            if (topCard != null)
            {
                return topCard;
            }
            else
            {
                Debug.LogWarning("Found a ghost card! Throwing it away and digging deeper...");
            }
        }
        
        // If we checked the whole list and found nothing
        return null; 
    }
}