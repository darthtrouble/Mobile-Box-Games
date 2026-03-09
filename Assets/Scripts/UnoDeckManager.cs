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
    public WildColorPicker wildColorPicker;

    [Header("Physical Settings")]
    public float cardThickness = 0.016f;

    [Header("Deck State")]
    public int playDirection = 1; // 1 = clockwise, -1 = counter-clockwise
    public List<UnoCard> currentDeck = new List<UnoCard>();
    public List<UnoCard> discardPile = new List<UnoCard>(); // NEW
    public bool isDeckReady = false;
    public CardColor activeColor;
    public CardType activeType;
    public int activeValue;
    
    public bool isWaitingForColorPicker = false;
    public int pendingDrawCount = 0; // Tracks if the player owes 2 or 4 cards

    public bool isGameActive = false; // Locks the game during dealing
    public int currentPlayerIndex = 0; // Tracks whose turn it is
    public bool hasDrawnThisTurn = false; // Prevents spam-drawing

    [Header("Multiplayer Settings")]
    [Range(2, 6)]
    public int playerCount = 2; // Default for testing
    
    [Tooltip("Assign in this priority: 1(Me), 2(Top), 3(TopRight), 4(TopLeft), 5(BotRight), 6(BotLeft)")]
    public List<PlayerHand> seatingPriority = new List<PlayerHand>();
    
    public List<PlayerHand> activePlayers = new List<PlayerHand>();

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

        GenerateDeck(boxTransform);

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
            UnoCard card = currentDeck[i];
            card.SetBlackoutMode(true); // Hide the card face immediately
            Transform cardTrans = card.transform;

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
        isGameActive = false;
        
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
            for (int p = 0; p < activePlayers.Count; p++)
            {
                PlayerHand player = activePlayers[p];
                UnoCard c = DrawTopCard();
                if (c != null) 
                {
                    c.SetBlackoutMode(true); // Ensure it stays black during flight
                    player.AddCard(c); 
                    
                    // We need to reveal the card ONLY if it belongs to the Local Player (Player 0)
                    // and only AFTER it arrives.
                    if (p == 0) 
                    {
                        StartCoroutine(RevealCardAfterDelay(c, 0.5f));
                    }
                }
                yield return new WaitForSeconds(0.1f); // Faster deal since there are more players
            }
        }

        yield return new WaitForSeconds(0.5f);
        
        UnoCard discardCard = DrawTopCard();
        if (discardCard != null)
        {
            discardCard.SetBlackoutMode(false); // The first card MUST be visible!
            discardPile.Add(discardCard);
            discardCard.isFaceUp = true;
            UpdateActiveState(discardCard);
            discardCard.transform.SetParent(discardPileAnchor);
            discardCard.transform.DOKill();
            
            // Disable the collider so the PlayerHand raycast ignores it forever
            Collider col = discardCard.GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            float randomX = UnityEngine.Random.Range(-0.015f, 0.015f);
            float randomZ = UnityEngine.Random.Range(-0.015f, 0.015f);
            float heightOffset = discardPile.Count * 0.0002f; 
            
            // Forced Vector3.up so it stacks perfectly even if the anchor is rotated wrong
            Vector3 worldPos = discardPileAnchor.position 
                + (Vector3.up * heightOffset) 
                + (discardPileAnchor.right * randomX) 
                + (discardPileAnchor.forward * randomZ);

            discardCard.transform.DOMove(worldPos, 0.4f).SetEase(Ease.OutQuad);
            discardCard.transform.DORotate(discardPileAnchor.eulerAngles, 0.4f).SetEase(Ease.OutQuad);
            
            // If the very first card is a Wild or Wild Draw 4, trigger the UI!
            if (discardCard.cardColor == CardColor.Wild)
            {
                if (wildColorPicker != null)
                {
                    wildColorPicker.ShowPicker();
                }
            }
        }

        if (tableCameraLook != null) tableCameraLook.canLook = true;
        
        currentPlayerIndex = 0; // Local player goes first!
        hasDrawnThisTurn = false;
        isGameActive = true; 
        Debug.Log("<color=green>Dealing Finished! Game Started. Player 0's turn.</color>");
    }

    public void GenerateDeck(Transform boxTransform)
    {
        currentDeck.Clear();
        
        // Standard UNO Colors
        CardColor[] colors = { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };

        foreach (CardColor color in colors)
        {
            // 1. Create the single '0' card
            GenerateSingleCard(color, CardType.Number, 0, boxTransform);

            // 2. Create two of each number from 1 to 9
            for (int i = 1; i <= 9; i++)
            {
                GenerateSingleCard(color, CardType.Number, i, boxTransform);
                GenerateSingleCard(color, CardType.Number, i, boxTransform);
            }

            // 3. Create two of each action card
            for (int i = 0; i < 2; i++)
            {
                GenerateSingleCard(color, CardType.Skip, -1, boxTransform);
                GenerateSingleCard(color, CardType.Reverse, -1, boxTransform);
                GenerateSingleCard(color, CardType.DrawTwo, -1, boxTransform);
            }
        }

        // 4. Create the 8 Wild Cards (4 regular, 4 draw-four)
        for (int i = 0; i < 4; i++)
        {
            GenerateSingleCard(CardColor.Wild, CardType.Wild, -1, boxTransform);
            GenerateSingleCard(CardColor.Wild, CardType.WildDrawFour, -1, boxTransform);
        }
    }

    private void GenerateSingleCard(CardColor color, CardType type, int value, Transform spawnPoint)
    {
        GameObject cardObj = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation);
        cardObj.name = $"Card_{color}_{type}{(value >= 0 ? "_" + value : "")}";
        UnoCard cardScript = cardObj.GetComponent<UnoCard>();
        
        if (cardScript != null)
        {
            cardScript.SetupCard(color, type, value);
            cardScript.isFaceUp = false;
            currentDeck.Add(cardScript);
        }
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

    public bool TryPlayCard(UnoCard cardToPlay, PlayerHand sourceHand)
    {
        // 1. Is the game actually running?
        if (!isGameActive) return false;

        // 2. Is it actually this player's turn?
        if (activePlayers.Count > 0 && activePlayers[currentPlayerIndex] != sourceHand)
        {
            Debug.LogWarning("Hold up! It is not your turn!");
            return false;
        }

        if (!IsValidMove(cardToPlay)) 
        {
            Debug.Log("Invalid Move! Does not match color or number.");
            return false;
        }
        UpdateActiveState(cardToPlay);
        
        // If it's a Wild or Wild Draw 4, trigger the UI!
        if (cardToPlay.cardColor == CardColor.Wild)
        {
            if (wildColorPicker != null)
            {
                isWaitingForColorPicker = true;
                wildColorPicker.ShowPicker();
            }
        }

        // 1. Remove from hand and fix the fan visually
        sourceHand.cardsInHand.Remove(cardToPlay);
        sourceHand.UpdateHandVisuals();

        // 2. Add to discard pile
        discardPile.Add(cardToPlay);
        // Reveal the card immediately when played to the center
        cardToPlay.SetBlackoutMode(false); 
        cardToPlay.transform.SetParent(discardPileAnchor);
        cardToPlay.isFaceUp = true;

        // 3. Animate the throw using World Space and OutQuad
        cardToPlay.transform.DOKill(); 
        
        // Fix: Also kill any hover animations on the visual child and reset its local offset!
        if (cardToPlay.transform.childCount > 0)
        {
            Transform visual = cardToPlay.transform.GetChild(0);
            visual.DOKill();
            visual.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
            visual.localRotation = Quaternion.identity;
        }
        
        // Disable the collider so the PlayerHand raycast ignores it forever
        Collider col = cardToPlay.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        float randomX = UnityEngine.Random.Range(-0.015f, 0.015f);
        float randomZ = UnityEngine.Random.Range(-0.015f, 0.015f);
        float heightOffset = discardPile.Count * 0.0002f; 
        
        // Forced Vector3.up so it stacks perfectly even if the anchor is rotated wrong
        Vector3 worldPos = discardPileAnchor.position 
            + (Vector3.up * heightOffset) 
            + (discardPileAnchor.right * randomX) 
            + (discardPileAnchor.forward * randomZ);

        cardToPlay.transform.DOMove(worldPos, 0.4f).SetEase(Ease.OutQuad);
        cardToPlay.transform.DORotate(discardPileAnchor.eulerAngles, 0.4f).SetEase(Ease.OutQuad);

        // Apply any special action card effects right before passing the turn
        ApplyCardEffect(cardToPlay);
        
        NextTurn();

        return true;
    }

    public void NextTurn()
    {
        // The extra + activePlayers.Count prevents negative index errors when going backwards!
        currentPlayerIndex = (currentPlayerIndex + playDirection + activePlayers.Count) % activePlayers.Count;
        hasDrawnThisTurn = false;
        Debug.Log($"<color=yellow>Turn Ended. Now Player {currentPlayerIndex}'s Turn! (Direction: {playDirection})</color>");
    }

    private void ApplyCardEffect(UnoCard card)
    {
        if (card.cardType == CardType.Reverse)
        {
            playDirection *= -1;
            Debug.Log("<color=magenta>UNO REVERSE! Direction changed.</color>");
            
            // Official Rule: In a 2-player game, Reverse acts exactly like a Skip!
            if (activePlayers.Count == 2)
            {
                NextTurn(); 
            }
        }
        else if (card.cardType == CardType.Skip)
        {
            Debug.Log("<color=orange>PLAYER SKIPPED!</color>");
            // We call NextTurn() once here. The TryPlayCard method will call it a SECOND time at the end, effectively leapfrogging the next player!
            NextTurn(); 
        }
        else if (card.cardType == CardType.DrawTwo)
        {
            Debug.Log("<color=red>DRAW TWO PENDING!</color>");
            pendingDrawCount += 2;
        }
        else if (card.cardType == CardType.WildDrawFour)
        {
            Debug.Log("<color=red>WILD DRAW FOUR PENDING!</color>");
            pendingDrawCount += 4;
        }
    }

    public void UpdateActiveState(UnoCard card)
    {
        activeColor = card.cardColor;
        activeType = card.cardType;
        activeValue = card.cardValue;
        Debug.Log($"<color=cyan>Active State Changed: {activeColor} | {activeType} | {activeValue}</color>");
    }

    public bool IsValidMove(UnoCard card)
    {
        // 1. STACKING RULE: If there is a pending penalty, you MUST play a matching draw card or draw.
        if (pendingDrawCount > 0)
        {
            // If it's a +2, you can only play another +2 or a +4
            if (activeType == CardType.DrawTwo)
            {
                return (card.cardType == CardType.DrawTwo || card.cardType == CardType.WildDrawFour);
            }
            // If it's a +4, you can only play another +4 (Hardcore mode!)
            if (activeType == CardType.WildDrawFour)
            {
                return (card.cardType == CardType.WildDrawFour);
            }
        }

        // 2. NORMAL RULES: Only if no penalty is pending
        if (card.cardColor == CardColor.Wild) return true;
        if (card.cardColor == activeColor) return true;
        if (card.cardType != CardType.Number && card.cardType == activeType) return true;
        if (card.cardType == CardType.Number && activeType == CardType.Number && card.cardValue == activeValue) return true;
        
        return false;
    }

    public void HandleDeckClick(PlayerHand clickingPlayer)
    {
        if (!isGameActive) return;
        
        if (activePlayers.Count > 0 && activePlayers[currentPlayerIndex] != clickingPlayer)
        {
            Debug.LogWarning("You cannot draw, it is not your turn!");
            return;
        }
        
        if (hasDrawnThisTurn)
        {
            Debug.LogWarning("You already drew this turn!");
            return;
        }
        
        hasDrawnThisTurn = true;

        // "Draw 2" House Rule: If no pending penalty, draw up to 2 cards!
        int amountToDraw = pendingDrawCount > 0 ? pendingDrawCount : 2; 

        StartCoroutine(DrawMultipleSequence(clickingPlayer, amountToDraw));
        
        // Reset after paying the debt
        if (pendingDrawCount > 0)
        {
            pendingDrawCount = 0; 
        }
    }

    private System.Collections.IEnumerator DrawMultipleSequence(PlayerHand player, int amountToDraw)
    {
        for (int i = 0; i < amountToDraw; i++)
        {
            UnoCard drawnCard = DrawTopCard();
            if (drawnCard != null)
            {
                // Disable collider and blackout visual during flight to prevent auto-playing!
                Collider col = drawnCard.GetComponent<Collider>();
                if (col != null) col.enabled = false;
                drawnCard.SetBlackoutMode(true);

                player.AddCard(drawnCard);
                
                // Start revealing routine
                StartCoroutine(RevealCardAfterFlight(drawnCard));
            }
            yield return new WaitForSeconds(0.15f); // Nice rapid-fire draw animation!
        }
        
        NextTurn();
    }

    private System.Collections.IEnumerator RevealCardAfterFlight(UnoCard card)
    {
        // Wait for the DOTween flight animation to finish (UpdateHandVisuals takes 0.4 seconds)
        yield return new WaitForSeconds(0.45f);
        
        card.SetBlackoutMode(false); // Restore colors and text
        
        // Reactivate colliders so the hand hover raycast works again
        Collider col = card.GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    private System.Collections.IEnumerator RevealCardAfterDelay(UnoCard card, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (card != null) card.SetBlackoutMode(false);
    }
}