using System.Collections.Generic;
using UnityEngine;

public enum MenuState
{
    Idle,
    Settings,
    Shop,
    GameSelection,
    Lobby
}

public class MenuStateManager : MonoBehaviour
{
    [Header("Core Dependencies")]
    public MenuAnimator animator;
    public CameraBob cameraBob;

    [Header("3D UI Elements")]
    public Transform tabletObject;      // Settings
    public Transform magazineObject;    // Shop
    
    [Header("Game Selection Carousel")]
    [Tooltip("List of 3D Game Boxes on the table.")]
    public List<Transform> gameBoxes;
    
    [Header("Lobby Elements")]
    public Transform paperObject;       // Lobby UI
    public Transform paperTargetPoint;  // Where paper rests after coming out of box
    [Tooltip("Name of the child object representing the box lid.")]
    public string boxLidName = "Lid"; 

    private MenuState _currentState = MenuState.Idle;
    private Transform _currentActiveObject;
    private int _currentGameBoxIndex = 0;

    private void Start()
    {
        // Hide UI objects on start
        if (tabletObject) tabletObject.gameObject.SetActive(false);
        if (magazineObject) magazineObject.gameObject.SetActive(false);
        if (paperObject) paperObject.gameObject.SetActive(false);
        
        foreach (var box in gameBoxes) 
        {
            if (box) box.gameObject.SetActive(false);
        }

        // Initialize Idle state (Empty table)
        ChangeState(MenuState.Idle);
    }

    public void ChangeState(MenuState newState)
    {
        if (_currentState == newState) return;

        Transform nextObject = GetTargetObjectForState(newState);
        
        // 1. Handle Camera Zoom
        if (cameraBob != null)
        {
            bool shouldZoom = (newState == MenuState.GameSelection || newState == MenuState.Lobby);
            cameraBob.SetZoom(shouldZoom);
        }

        // 2. Handle specific transitions
        if (newState == MenuState.Lobby && _currentState == MenuState.GameSelection)
        {
            // Transition: Game Selection -> Lobby (Open box, slide out paper)
            Transform currentBox = GetCurrentGameBox();
            if (currentBox != null)
            {
                Transform lid = currentBox.Find(boxLidName);
                if (lid != null)
                {
                    animator.OpenBoxAndShowPaper(lid, paperObject, paperTargetPoint);
                }
                else
                {
                    Debug.LogWarning($"Lid not found on {currentBox.name}. Ensure it has a child named {boxLidName}");
                }
            }
            _currentActiveObject = paperObject;
        }
        else if (_currentState == MenuState.Lobby && newState == MenuState.GameSelection)
        {
            // Transition: Lobby -> Game Selection (Close box, hide paper)
            Transform currentBox = GetCurrentGameBox();
            if (currentBox != null)
            {
                Transform lid = currentBox.Find(boxLidName);
                animator.CloseBoxAndHidePaper(lid, paperObject);
            }
            _currentActiveObject = currentBox;
        }
        else
        {
            // Standard generic sliding transition
            animator.SwitchObjects(_currentActiveObject, nextObject);
            _currentActiveObject = nextObject;
        }

        _currentState = newState;
    }

    private Transform GetTargetObjectForState(MenuState state)
    {
        switch (state)
        {
            case MenuState.Settings: 
                return tabletObject;
            case MenuState.Shop: 
                return magazineObject;
            case MenuState.GameSelection: 
                return GetCurrentGameBox();
            case MenuState.Lobby: 
                return paperObject;
            case MenuState.Idle:
            default: 
                return null;
        }
    }

    private Transform GetCurrentGameBox()
    {
        if (gameBoxes == null || gameBoxes.Count == 0) return null;
        return gameBoxes[_currentGameBoxIndex];
    }

    #region Carousel Controls
    public void NextGameBox()
    {
        if (_currentState != MenuState.GameSelection || gameBoxes.Count <= 1) return;

        Transform outBox = gameBoxes[_currentGameBoxIndex];
        _currentGameBoxIndex = (_currentGameBoxIndex + 1) % gameBoxes.Count;
        Transform inBox = gameBoxes[_currentGameBoxIndex];

        animator.SwitchObjects(outBox, inBox);
        _currentActiveObject = inBox;
    }

    public void PreviousGameBox()
    {
        if (_currentState != MenuState.GameSelection || gameBoxes.Count <= 1) return;

        Transform outBox = gameBoxes[_currentGameBoxIndex];
        _currentGameBoxIndex--;
        if (_currentGameBoxIndex < 0) _currentGameBoxIndex = gameBoxes.Count - 1;
        Transform inBox = gameBoxes[_currentGameBoxIndex];

        animator.SwitchObjects(outBox, inBox);
        _currentActiveObject = inBox;
    }
    #endregion

    #region UGUI Button Hooks
    public void GoToIdle() => ChangeState(MenuState.Idle);
    public void GoToSettings() => ChangeState(MenuState.Settings);
    public void GoToShop() => ChangeState(MenuState.Shop);
    public void GoToGameSelection() => ChangeState(MenuState.GameSelection);
    public void GoToLobby() => ChangeState(MenuState.Lobby);
    #endregion
}
