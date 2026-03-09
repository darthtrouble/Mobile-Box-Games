using UnityEngine;
using System.Collections.Generic;

public class OpponentAI : MonoBehaviour
{
    public UnoDeckManager deckManager;
    public PlayerHand myHand;
    public float thinkTime = 1.5f;

    private float currentThinkTimer = 0f;
    private bool isThinking = false;

    void Update()
    {
        if (deckManager == null || !deckManager.isGameActive) return;
        if (deckManager.isWaitingForColorPicker) return; // AI freezes while player is picking

        // Failsafe: Check if it is currently my turn
        bool isMyTurn = (deckManager.activePlayers.Count > 0 && deckManager.activePlayers[deckManager.currentPlayerIndex] == myHand);

        if (isMyTurn)
        {
            // Start thinking if we haven't already
            if (!isThinking && !deckManager.hasDrawnThisTurn)
            {
                isThinking = true;
                currentThinkTimer = 0f;
            }

            // Tick the stopwatch
            if (isThinking)
            {
                currentThinkTimer += Time.deltaTime;

                if (currentThinkTimer >= thinkTime)
                {
                    isThinking = false; // Stop timer
                    ExecuteMove();
                }
            }
        }
        else
        {
            // THE FIX: If it is NOT my turn, brutally reset the stopwatch. 
            // This prevents the AI from "pre-thinking" during a Skip/Reverse!
            isThinking = false;
            currentThinkTimer = 0f;
        }
    }

    private void ExecuteMove()
    {
        // 1. Scan hand for a valid move
        UnoCard cardToPlay = null;
        foreach (UnoCard card in myHand.cardsInHand)
        {
            if (deckManager.IsValidMove(card))
            {
                cardToPlay = card;
                break; 
            }
        }

        // 2. Play or Draw
        if (cardToPlay != null)
        {
            // Handle AI Wild Cards
            if (cardToPlay.cardColor == CardColor.Wild)
            {
                // Smarter AI Color Selection
                Dictionary<CardColor, int> colorCounts = new Dictionary<CardColor, int> {
                    { CardColor.Red, 0 }, { CardColor.Blue, 0 }, { CardColor.Green, 0 }, { CardColor.Yellow, 0 }
                };

                foreach (UnoCard c in myHand.cardsInHand) {
                    if (c.cardColor != CardColor.Wild) colorCounts[c.cardColor]++;
                }

                // Pick the color it has the most of
                CardColor bestColor = CardColor.Red;
                int maxCount = -1;
                foreach (var kvp in colorCounts) {
                    if (kvp.Value > maxCount) {
                        maxCount = kvp.Value;
                        bestColor = kvp.Key;
                    }
                }

                deckManager.activeColor = bestColor;
                cardToPlay.cardColor = bestColor;
                cardToPlay.UpdateVisuals();
                Debug.Log($"<color={bestColor}>AI chose {bestColor} because it has {maxCount} of them!</color>");
            }

            // Reveal the card the moment the AI decides to play it
            cardToPlay.SetBlackoutMode(false); 
            
            // Now play the card
            deckManager.TryPlayCard(cardToPlay, myHand);
        }
        else
        {
            Debug.Log($"AI (Player {deckManager.currentPlayerIndex}) has no match. Drawing cards...");
            deckManager.HandleDeckClick(myHand); // Fixed matching UnoDeckManager method name
        }
    }
}
