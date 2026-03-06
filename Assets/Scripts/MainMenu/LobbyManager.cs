using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("System References")]
    public UnoDeckManager deckManager;
    public DiegeticMenuController menuController;

    [Header("UI Elements")]
    public TextMeshProUGUI playerCountText;
    public Button btnMinus;
    public Button btnPlus;
    public Button btnStart;

    private int currentPlayerCount = 3;

    void Start()
    {
        // Hook up the buttons to their functions
        btnMinus.onClick.AddListener(DecreasePlayers);
        btnPlus.onClick.AddListener(IncreasePlayers);
        btnStart.onClick.AddListener(StartGame);

        UpdateUI();
    }

    void DecreasePlayers()
    {
        if (currentPlayerCount > 2)
        {
            currentPlayerCount--;
            UpdateUI();
        }
    }

    void IncreasePlayers()
    {
        if (currentPlayerCount < 6)
        {
            currentPlayerCount++;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (playerCountText != null)
        {
            playerCountText.text = "PLAYERS: " + currentPlayerCount;
        }

        if (deckManager != null)
        {
            deckManager.playerCount = currentPlayerCount;
        }
    }

    void StartGame()
    {
        if (menuController != null)
        {
            // Auto-close the paper and trigger the game sequence!
            menuController.CloseLobby();
            menuController.StartUnoGame();
        }
    }

}
