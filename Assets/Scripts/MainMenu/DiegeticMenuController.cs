using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[System.Serializable]
public class InteractableItem
{
    public GameObject itemModel;
    [HideInInspector] public Vector3 originalTablePos;
    [HideInInspector] public Vector3 originalTableRot;

    public void SaveOriginalPose()
    {
        if (itemModel != null)
        {
            originalTablePos = itemModel.transform.position;
            originalTableRot = itemModel.transform.eulerAngles;
        }
    }
}

// NEW: This holds all the specific pieces for each game
[System.Serializable]
public class GameBoxData
{
    public string gameID;
    public GameObject boxRoot;      // The main parent box
    public Transform boxLid;        // The lid
    public Transform lobbyPaper;    // The paper nested inside

    [HideInInspector] public Vector3 lidOriginalLocalPos;
    [HideInInspector] public Vector3 paperOriginalLocalPos;
    [HideInInspector] public Vector3 paperOriginalLocalRot;

    [HideInInspector] public Vector3 boxOriginalPos;
    [HideInInspector] public Vector3 boxOriginalRot;

    public void SaveOriginalPoses()
    {
        if (boxRoot != null)
        {
            boxOriginalPos = boxRoot.transform.position;
            boxOriginalRot = boxRoot.transform.eulerAngles;
        }

        if (boxLid != null) lidOriginalLocalPos = boxLid.localPosition;
        if (lobbyPaper != null)
        {
            paperOriginalLocalPos = lobbyPaper.localPosition;
            paperOriginalLocalRot = lobbyPaper.localEulerAngles;
        }
    }
}

public class DiegeticMenuController : MonoBehaviour
{
    [Header("Camera Intro")]
    public Transform cameraStartPoint;
    public Transform cameraTablePoint;
    public Camera mainCam;

    [Header("Pick Up / Put Down (Settings, Shop, Paper)")]
    public Transform inspectPoint; 
    public Transform paperInspectPoint;
    private InteractableItem currentInspectedItem; // Tracks tablet/magazine
    private bool isLobbyPaperInspected = false;    // Tracks if a paper is at the camera

    [Header("Table Items")]
    public InteractableItem tabletItem;
    public InteractableItem magazineItem;

    [Header("Play Transition Transitions")]
    public Transform tabletHiddenPoint;
    public Transform magazineHiddenPoint;
    public Transform tableCenterPoint;
    
    [Header("Game Selection Carousel")]
    // UPDATED: Now uses our new custom class!
    public List<GameBoxData> gameBoxes; 
    public Transform offCenterLeftPoint;
    public Transform offCenterRightPoint;
    public Transform cornerPilePoint; 
    private int currentGameIndex = 0;
    private bool inPlayMode = false; // Tracks if we are looking at boxes
    private bool isAtTable = false;  // Tracks if the intro has finished
    private bool isAnimatingIntro = false; // Locks input during the intro sequence
    private bool isPlayingUno = false;     // Locks input during Uno game

    [Header("Lid Animation Offsets")]
    [Tooltip("How high the lid lifts up first")]
    public float lidLiftHeight = 0.5f;
    [Tooltip("Where the lid rests relative to the box (e.g., negative X for left)")]
    public Vector3 lidRestOffset = new Vector3(-1.2f, 0f, 0f);

    [Header("Uno Transition Extras")]
    public UnoDeckManager unoDeckManager;
    public PlayerHand player1Hand;
    public Transform sideTableTabletPoint;
    public Transform sideTableMagazinePoint;
    public Transform sideTableGameBoxesPoint;
    public Transform unoCameraPoint;
    public List<GameObject> unoSeatModels;
    public List<Transform> unoSeatAnchors;
    public float undergroundOffset = 2f;

    private void Start()
    {
        // Camera Intro Initial Setup
        if (mainCam != null && cameraStartPoint != null)
        {
            CameraBob camBob = mainCam.GetComponent<CameraBob>();
            if (camBob != null)
            {
                camBob.initialPosition = cameraStartPoint.position;
                camBob.initialRotation = cameraStartPoint.eulerAngles;
            }
        }

        // Save original poses
        tabletItem?.SaveOriginalPose();
        magazineItem?.SaveOriginalPose();

        // Save box internals original poses
        foreach (var box in gameBoxes)
        {
            box.SaveOriginalPoses();
        }
    }

    void Update()
    {
        if (Keyboard.current == null || isAnimatingIntro) return;

        // Title Screen -> Table Transition
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isAtTable)
        {
            StartIntroTransition();
        }

        if (!isAtTable) return; // Ignore other inputs if not at table

        // Uno Draw Card Test (Allow this during Uno Game)
        if (Keyboard.current.digit5Key.wasPressedThisFrame && isPlayingUno)
        {
            if (unoDeckManager != null && player1Hand != null)
            {
                UnoCard drawnCard = unoDeckManager.DrawTopCard();
                if (drawnCard != null)
                {
                    player1Hand.AddCard(drawnCard);
                }
            }
        }

        // Return if playing uno so we don't trigger carousel actions
        if (isPlayingUno) return;

        // Play Mode Transition
        if (Keyboard.current.digit3Key.wasPressedThisFrame && !inPlayMode && currentInspectedItem == null)
        {
            TransitionToPlayMode(); 
        }

        // Carousel 
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame && inPlayMode && !isLobbyPaperInspected)
        {
            NextGame(); 
        }
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame && inPlayMode && !isLobbyPaperInspected)
        {
            PreviousGame(); 
        }

        // Open specific box and inspect paper
        if (Keyboard.current.oKey.wasPressedThisFrame && inPlayMode && !isLobbyPaperInspected)
        {
            OpenLobby(); 
        }

        // Select Current Box
        if (Keyboard.current.digit4Key.wasPressedThisFrame && inPlayMode)
        {
            if (gameBoxes != null && gameBoxes.Count > 0)
            {
                if (string.Equals(gameBoxes[currentGameIndex].gameID, "UNO", System.StringComparison.OrdinalIgnoreCase))
                {
                    StartUnoGame();
                }
            }
        }


        // Pick up items (Only if not in play mode)
        if (!inPlayMode)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                InspectItem(tabletItem); 
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                InspectItem(magazineItem); 
            }
        }

        // Master Revert / Put Down
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentInspectedItem != null) 
                CloseInspectedItem(); 
            else if (isLobbyPaperInspected) 
                CloseLobby(); // If paper is up, just close the box first
            else if (inPlayMode) 
                BackToMainMenu(); // If boxes are in center, return to normal table
            else
                BackToTitleScreen(); // If at normal table, return to title screen
        }
    }

    private void StartIntroTransition()
    {
        if (mainCam == null || cameraTablePoint == null) return;

        isAtTable = true;
        isAnimatingIntro = true;
        CameraBob camBob = mainCam.GetComponent<CameraBob>();

        if (camBob != null)
        {
            Sequence introSeq = DOTween.Sequence();

            // Step 1: Walk to position (Jump to simulate steps), while matching Y and Z rotations
            Vector3 approachRot = new Vector3(cameraStartPoint.eulerAngles.x, cameraTablePoint.eulerAngles.y, cameraTablePoint.eulerAngles.z);

            // We do 4 "jumps" of 0.15f height each across the 2 second duration to simulate walking steps
            introSeq.Append(DOTween.To(() => camBob.initialPosition, x => camBob.initialPosition = x, cameraTablePoint.position, 2f)
                .SetEase(Ease.InOutSine)
                .SetOptions(AxisConstraint.None, true)); // Using SetOptions to trick DOTween into doing a path or standard jump, but simple jump is better:

            // Actually, DOJump isn't natively supported on float/custom setters like DOTween.To, 
            // so let's animate the Transform directly with DOJump and just tell CameraBob to ignore its offset completely during this specific sequence.
            
            camBob.isEnabled = false; 

            // Clear previous sequence and rebuilt it correctly using DOJump on the transform
            introSeq = DOTween.Sequence();
            
            // 4 jumps across 2 seconds, jumping 0.15 height each time.
            introSeq.Append(mainCam.transform.DOJump(cameraTablePoint.position, 0.15f, 4, 2f).SetEase(Ease.Linear));
            introSeq.Join(mainCam.transform.DORotate(approachRot, 2f).SetEase(Ease.InOutSine));

            // Step 2: Pitch the camera down (X rotation) to look at the table
            introSeq.Append(mainCam.transform.DORotate(cameraTablePoint.eulerAngles, 1f).SetEase(Ease.InOutQuad));

            // Done: Re-sync CameraBob and enable it
            introSeq.OnComplete(() =>
            {
                camBob.initialPosition = mainCam.transform.localPosition;
                camBob.initialRotation = mainCam.transform.localEulerAngles;
                camBob.isEnabled = true;
                isAnimatingIntro = false;
            });
        }
    }

    public void BackToTitleScreen()
    {
        if (mainCam == null || cameraStartPoint == null) return;

        isAtTable = false;
        isAnimatingIntro = true; // Lock input during animation
        CameraBob camBob = mainCam.GetComponent<CameraBob>();

        if (camBob != null) camBob.isEnabled = false;

        Sequence outroSeq = DOTween.Sequence();
        
        // Smoothly move and rotate back to the start point
        outroSeq.Append(mainCam.transform.DOMove(cameraStartPoint.position, 1.5f).SetEase(Ease.InOutSine));
        outroSeq.Join(mainCam.transform.DORotate(cameraStartPoint.eulerAngles, 1.5f).SetEase(Ease.InOutSine));

        outroSeq.OnComplete(() =>
        {
            if (camBob != null)
            {
                camBob.initialPosition = mainCam.transform.localPosition;
                camBob.initialRotation = mainCam.transform.localEulerAngles;
                camBob.isEnabled = true;
            }
            isAnimatingIntro = false;
        });
    }

    public void InspectItem(InteractableItem item)
    {
        if (item == null || item.itemModel == null || inspectPoint == null) return;
        
        // If we are already inspecting this exact item, we might want to just close it, 
        // essentially making the hotkey a toggle.
        if (currentInspectedItem == item)
        {
            CloseInspectedItem();
            return;
        }

        // If we are currently inspecting a DIFFERENT item, put it back first, then bring the new one
        if (currentInspectedItem != null)
        {
            Sequence swapSeq = DOTween.Sequence();
            
            // 1. Move old item back
            swapSeq.Append(currentInspectedItem.itemModel.transform.DOMove(currentInspectedItem.originalTablePos, 0.4f).SetEase(Ease.OutQuad));
            swapSeq.Join(currentInspectedItem.itemModel.transform.DORotate(currentInspectedItem.originalTableRot, 0.4f).SetEase(Ease.OutQuad));

            // 2. Assign and move new item in
            swapSeq.AppendCallback(() => currentInspectedItem = item);
            swapSeq.Append(item.itemModel.transform.DOMove(inspectPoint.position, 0.5f).SetEase(Ease.OutQuad));
            swapSeq.Join(item.itemModel.transform.DORotate(inspectPoint.eulerAngles, 0.5f).SetEase(Ease.OutQuad));
        }
        else
        {
            // Just bring it up normally
            currentInspectedItem = item;
            item.itemModel.transform.DOMove(inspectPoint.position, 0.5f).SetEase(Ease.OutQuad);
            item.itemModel.transform.DORotate(inspectPoint.eulerAngles, 0.5f).SetEase(Ease.OutQuad);
        }
    }

    public void CloseInspectedItem()
    {
        if (currentInspectedItem == null || currentInspectedItem.itemModel == null) return;
        currentInspectedItem.itemModel.transform.DOMove(currentInspectedItem.originalTablePos, 0.5f).SetEase(Ease.OutQuad);
        currentInspectedItem.itemModel.transform.DORotate(currentInspectedItem.originalTableRot, 0.5f).SetEase(Ease.OutQuad);
        currentInspectedItem = null;
    }

    public void TransitionToPlayMode()
    {
        inPlayMode = true;
        Sequence playSeq = DOTween.Sequence();

        // 1. Hide the tablet and magazine
        if (tabletItem?.itemModel != null && tabletHiddenPoint != null)
        {
            playSeq.Join(tabletItem.itemModel.transform.DOMove(tabletHiddenPoint.position, 0.5f).SetEase(Ease.InOutQuad));
            playSeq.Join(tabletItem.itemModel.transform.DORotate(tabletHiddenPoint.eulerAngles, 0.5f).SetEase(Ease.InOutQuad));
        }

        if (magazineItem?.itemModel != null && magazineHiddenPoint != null)
        {
            playSeq.Join(magazineItem.itemModel.transform.DOMove(magazineHiddenPoint.position, 0.5f).SetEase(Ease.InOutQuad));
            playSeq.Join(magazineItem.itemModel.transform.DORotate(magazineHiddenPoint.eulerAngles, 0.5f).SetEase(Ease.InOutQuad));
        }

        // 2. Bring current box to center and prep the next box on the right
        if (gameBoxes != null && gameBoxes.Count > 0 && tableCenterPoint != null)
        {
            GameObject currentBox = gameBoxes[currentGameIndex].boxRoot;
            if (currentBox != null)
            {
                if (cornerPilePoint != null)
                {
                    currentBox.transform.position = cornerPilePoint.position;
                    currentBox.transform.rotation = cornerPilePoint.rotation;
                }
                playSeq.Append(currentBox.transform.DOMove(tableCenterPoint.position, 0.5f).SetEase(Ease.OutBack));
                playSeq.Join(currentBox.transform.DORotate(tableCenterPoint.eulerAngles, 0.5f).SetEase(Ease.OutBack));
            }

            // --- NEW: Snap the NEXT box to the off-center right point ---
            if (gameBoxes.Count > 1 && offCenterRightPoint != null)
            {
                int nextIndex = (currentGameIndex + 1) % gameBoxes.Count;
                GameObject nextBox = gameBoxes[nextIndex].boxRoot;
                
                if (nextBox != null)
                {
                    nextBox.transform.position = offCenterRightPoint.position;
                    nextBox.transform.rotation = offCenterRightPoint.rotation;
                }
            }
        }
    }

    public void NextGame()
    {
        if (gameBoxes == null || gameBoxes.Count <= 1) return;
        GameObject currentBox = gameBoxes[currentGameIndex].boxRoot;
        
        if (currentBox != null && offCenterRightPoint != null)
            currentBox.transform.DOMove(offCenterRightPoint.position, 0.5f).SetEase(Ease.InQuad);

        currentGameIndex = (currentGameIndex + 1) % gameBoxes.Count;
        GameObject newBox = gameBoxes[currentGameIndex].boxRoot;

        if (newBox != null && offCenterLeftPoint != null && tableCenterPoint != null)
        {
            newBox.transform.position = offCenterLeftPoint.position;
            newBox.transform.rotation = offCenterLeftPoint.rotation;
            newBox.transform.DOMove(tableCenterPoint.position, 0.5f).SetDelay(0.2f).SetEase(Ease.OutQuad);
            newBox.transform.DORotate(tableCenterPoint.eulerAngles, 0.5f).SetDelay(0.2f).SetEase(Ease.OutQuad);
        }
    }

    public void PreviousGame()
    {
        if (gameBoxes == null || gameBoxes.Count <= 1) return;
        GameObject currentBox = gameBoxes[currentGameIndex].boxRoot;
        
        if (currentBox != null && offCenterLeftPoint != null)
            currentBox.transform.DOMove(offCenterLeftPoint.position, 0.5f).SetEase(Ease.InQuad);

        currentGameIndex--;
        if (currentGameIndex < 0) currentGameIndex = gameBoxes.Count - 1;

        GameObject newBox = gameBoxes[currentGameIndex].boxRoot;

        if (newBox != null && offCenterRightPoint != null && tableCenterPoint != null)
        {
            newBox.transform.position = offCenterRightPoint.position;
            newBox.transform.rotation = offCenterRightPoint.rotation;
            newBox.transform.DOMove(tableCenterPoint.position, 0.5f).SetDelay(0.2f).SetEase(Ease.OutQuad);
            newBox.transform.DORotate(tableCenterPoint.eulerAngles, 0.5f).SetDelay(0.2f).SetEase(Ease.OutQuad);
        }
    }

    // --- NEW: Multi-stage Open and Close Logic ---

    public void OpenLobby()
    {
        if (gameBoxes == null || gameBoxes.Count == 0 || inspectPoint == null) return;
        
        isLobbyPaperInspected = true;
        GameBoxData currentData = gameBoxes[currentGameIndex];
        Sequence openSeq = DOTween.Sequence();

        // 1. Lid goes straight UP
        if (currentData.boxLid != null)
        {
            Vector3 upPos = currentData.lidOriginalLocalPos + (Vector3.up * lidLiftHeight);
            openSeq.Append(currentData.boxLid.DOLocalMove(upPos, 0.25f).SetEase(Ease.OutQuad));
            
            // 2. Lid slides LEFT (applying the offset)
            Vector3 restPos = upPos + lidRestOffset;
            openSeq.Append(currentData.boxLid.DOLocalMove(restPos, 0.3f).SetEase(Ease.InOutSine));
        }

       // 3. Paper flies to the camera (World Space)
        if (currentData.lobbyPaper != null && paperInspectPoint != null) // <--- Updated check
        {
            // V--- Updated destinations here ---V
            openSeq.Append(currentData.lobbyPaper.DOMove(paperInspectPoint.position, 0.5f).SetEase(Ease.OutBack));
            openSeq.Join(currentData.lobbyPaper.DORotate(paperInspectPoint.eulerAngles, 0.5f).SetEase(Ease.OutBack));
        }
    }

    public void CloseLobby()
    {
        if (gameBoxes == null || gameBoxes.Count == 0) return;
        
        GameBoxData currentData = gameBoxes[currentGameIndex];
        Sequence closeSeq = DOTween.Sequence();

        // 1. Paper flies back into the box (Local Space)
        if (currentData.lobbyPaper != null)
        {
            closeSeq.Append(currentData.lobbyPaper.DOLocalMove(currentData.paperOriginalLocalPos, 0.5f).SetEase(Ease.InOutQuad));
            closeSeq.Join(currentData.lobbyPaper.DOLocalRotate(currentData.paperOriginalLocalRot, 0.5f).SetEase(Ease.InOutQuad));
        }

        // 2. Lid slides RIGHT (back to center, but still up)
        if (currentData.boxLid != null)
        {
            Vector3 upPos = currentData.lidOriginalLocalPos + (Vector3.up * lidLiftHeight);
            closeSeq.Append(currentData.boxLid.DOLocalMove(upPos, 0.3f).SetEase(Ease.InOutSine));
            
            // 3. Lid goes straight DOWN (closes)
            closeSeq.Append(currentData.boxLid.DOLocalMove(currentData.lidOriginalLocalPos, 0.25f).SetEase(Ease.InQuad));
        }

        closeSeq.OnComplete(() => isLobbyPaperInspected = false);
    }

    public void BackToMainMenu()
    {
        inPlayMode = false;
        Sequence backSeq = DOTween.Sequence();

        // --- NEW: Send ALL boxes back to their individual original positions ---
        if (gameBoxes != null && gameBoxes.Count > 0)
        {
            foreach (var boxData in gameBoxes)
            {
                GameObject box = boxData.boxRoot;
                if (box != null)
                {
                    backSeq.Join(box.transform.DOMove(boxData.boxOriginalPos, 0.5f).SetEase(Ease.InOutQuad));
                    backSeq.Join(box.transform.DORotate(boxData.boxOriginalRot, 0.5f).SetEase(Ease.InOutQuad));
                }
            }
        }

        // Restore Tablet
        if (tabletItem?.itemModel != null)
        {
            backSeq.Join(tabletItem.itemModel.transform.DOMove(tabletItem.originalTablePos, 0.5f).SetEase(Ease.InOutQuad));
            backSeq.Join(tabletItem.itemModel.transform.DORotate(tabletItem.originalTableRot, 0.5f).SetEase(Ease.InOutQuad));
        }

        // Restore Magazine
        if (magazineItem?.itemModel != null)
        {
            backSeq.Join(magazineItem.itemModel.transform.DOMove(magazineItem.originalTablePos, 0.5f).SetEase(Ease.InOutQuad));
            backSeq.Join(magazineItem.itemModel.transform.DORotate(magazineItem.originalTableRot, 0.5f).SetEase(Ease.InOutQuad));
        }
    }

    public void StartUnoGame()
    {
        isPlayingUno = true;
        Sequence unoSeq = DOTween.Sequence();

        GameBoxData currentBox = gameBoxes[currentGameIndex];

        // 1. Clear the Table (Tablet & Magazine to side table)
        if (tabletItem?.itemModel != null && sideTableTabletPoint != null)
        {
            unoSeq.Insert(0, tabletItem.itemModel.transform.DOMove(sideTableTabletPoint.position, 1f).SetEase(Ease.InOutBack));
            unoSeq.Insert(0, tabletItem.itemModel.transform.DORotate(sideTableTabletPoint.eulerAngles, 1f).SetEase(Ease.InOutBack));
        }

        if (magazineItem?.itemModel != null && sideTableMagazinePoint != null)
        {
            unoSeq.Insert(0, magazineItem.itemModel.transform.DOMove(sideTableMagazinePoint.position, 1f).SetEase(Ease.InOutBack));
            unoSeq.Insert(0, magazineItem.itemModel.transform.DORotate(sideTableMagazinePoint.eulerAngles, 1f).SetEase(Ease.InOutBack));
        }

        // 2. Clear Game Boxes to side table (EXCEPT currentBox)
        if (gameBoxes != null && sideTableGameBoxesPoint != null)
        {
            foreach (var boxData in gameBoxes)
            {
                if (boxData != currentBox && boxData.boxRoot != null)
                {
                    unoSeq.Insert(0, boxData.boxRoot.transform.DOMove(sideTableGameBoxesPoint.position, 1f).SetEase(Ease.InOutBack));
                    unoSeq.Insert(0, boxData.boxRoot.transform.DORotate(sideTableGameBoxesPoint.eulerAngles, 1f).SetEase(Ease.InOutBack));
                }
            }
        }

        // 3. Move the Camera
        if (mainCam != null && unoCameraPoint != null)
        {
            CameraBob camBob = mainCam.GetComponent<CameraBob>();
            if (camBob != null) camBob.isEnabled = false;

            unoSeq.Insert(0, mainCam.transform.DOMove(unoCameraPoint.position, 1.5f).SetEase(Ease.InOutSine));
            unoSeq.Insert(0, mainCam.transform.DORotate(unoCameraPoint.eulerAngles, 1.5f).SetEase(Ease.InOutSine));
            
            unoSeq.OnComplete(() =>
            {
                if (camBob != null)
                {
                    camBob.initialPosition = mainCam.transform.localPosition;
                    camBob.initialRotation = mainCam.transform.localEulerAngles;
                    camBob.isEnabled = true;
                }
            });
        }

        // 4. Seat Animation
        if (unoSeatModels != null && unoSeatAnchors != null)
        {
            for (int i = 0; i < unoSeatModels.Count; i++)
            {
                if (i < unoSeatAnchors.Count && unoSeatModels[i] != null && unoSeatAnchors[i] != null)
                {
                    // Snap to below table
                    Vector3 anchorPos = unoSeatAnchors[i].position;
                    unoSeatModels[i].transform.position = anchorPos + (Vector3.down * undergroundOffset);
                    unoSeatModels[i].transform.rotation = unoSeatAnchors[i].rotation;

                    // Tween up after delay
                    unoSeq.Insert(0.5f, unoSeatModels[i].transform.DOMove(anchorPos, 1f).SetEase(Ease.OutBack));
                }
            }
        }

        // 5. The Box Sequence
        Sequence boxSeq = DOTween.Sequence();
        
        // Wait for camera to settle
        boxSeq.AppendInterval(1f);
        
        // Open the lid
        if (currentBox.boxLid != null)
        {
            boxSeq.Append(currentBox.boxLid.DOLocalMove(currentBox.lidOriginalLocalPos + (Vector3.up * lidLiftHeight), 0.4f).SetEase(Ease.OutBack));
        }

        // Call the deck manager
        boxSeq.AppendCallback(() => 
        { 
            if (unoDeckManager != null && currentBox.boxRoot != null) 
            {
                unoDeckManager.SpawnDeckFromBox(currentBox.boxRoot.transform); 
            }
        });

        // Wait for cards to fly out
        boxSeq.AppendInterval(2.5f);

        // Close the lid
        if (currentBox.boxLid != null)
        {
            boxSeq.Append(currentBox.boxLid.DOLocalMove(currentBox.lidOriginalLocalPos, 0.4f).SetEase(Ease.InBack));
        }

        // Move the empty box to the side table
        if (currentBox.boxRoot != null && sideTableGameBoxesPoint != null)
        {
            boxSeq.Append(currentBox.boxRoot.transform.DOMove(sideTableGameBoxesPoint.position, 1f).SetEase(Ease.InOutBack));
            boxSeq.Join(currentBox.boxRoot.transform.DORotate(sideTableGameBoxesPoint.eulerAngles, 1f).SetEase(Ease.InOutBack));
        }
    }
}