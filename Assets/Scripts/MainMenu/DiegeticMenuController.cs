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

[System.Serializable]
public class GameBoxData
{
    public string gameID;
    public GameObject boxRoot;
    public Transform boxLid;
    public Transform lobbyPaper;

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
    public Transform cameraRig; // NEW: The Neck that moves around the room!
    public Camera mainCam;      // The Camera inside the Neck!

    [Header("Pick Up / Put Down (Settings, Shop, Paper)")]
    public Transform inspectPoint; 
    public Transform paperInspectPoint;
    private InteractableItem currentInspectedItem; 
    private bool isLobbyPaperInspected = false;    

    [Header("Table Items")]
    public InteractableItem tabletItem;
    public InteractableItem magazineItem;

    [Header("Play Transition Transitions")]
    public Transform tabletHiddenPoint;
    public Transform magazineHiddenPoint;
    public Transform tableCenterPoint;
    
    [Header("Game Selection Carousel")]
    public List<GameBoxData> gameBoxes;
    public Transform offCenterLeftPoint;
    public Transform offCenterRightPoint;
    public Transform cornerPilePoint;
    private int currentGameIndex = 0;
    private bool inPlayMode = false;
    private bool isAtTable = false;

    private bool isAnimatingIntro = false;
    private bool isPlayingUno = false;

    private bool isAnimating = false;

    [Header("Lid Animation Offsets")]
    public float lidLiftHeight = 0.5f;
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
        if (cameraRig != null && cameraStartPoint != null)
        {
            cameraRig.position = cameraStartPoint.position;
            cameraRig.rotation = cameraStartPoint.rotation;
        }

        if (mainCam != null)
        {
            CameraBob camBob = mainCam.GetComponent<CameraBob>();
            if (camBob != null)
            {
                camBob.initialPosition = mainCam.transform.localPosition;
                camBob.initialRotation = mainCam.transform.localEulerAngles;
            }
        }

        tabletItem?.SaveOriginalPose();
        magazineItem?.SaveOriginalPose();

        foreach (var box in gameBoxes) box.SaveOriginalPoses();
    }

    void Update()
    {
        if (Keyboard.current == null || isAnimatingIntro) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isAtTable)
            StartIntroTransition();

        if (!isAtTable) return; 

        if (Keyboard.current.digit5Key.wasPressedThisFrame && isPlayingUno)
        {
            if (unoDeckManager != null && player1Hand != null)
            {
                UnoCard drawnCard = unoDeckManager.DrawTopCard();
                if (drawnCard != null) player1Hand.AddCard(drawnCard);
            }
        }

        if (isPlayingUno) return;

        if (Keyboard.current.digit3Key.wasPressedThisFrame && !inPlayMode && currentInspectedItem == null)
            TransitionToPlayMode(); 

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame && inPlayMode && !isLobbyPaperInspected)
        {
            if (isAnimating) return;
            NextGame(); 
        }
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame && inPlayMode && !isLobbyPaperInspected)
        {
            if (isAnimating) return;
            PreviousGame(); 
        }

        if (Keyboard.current.oKey.wasPressedThisFrame && inPlayMode && !isLobbyPaperInspected)
            OpenLobby(); 

        if (Keyboard.current.digit4Key.wasPressedThisFrame && inPlayMode)
        {
            if (gameBoxes != null && gameBoxes.Count > 0)
            {
                if (string.Equals(gameBoxes[currentGameIndex].gameID, "UNO", System.StringComparison.OrdinalIgnoreCase))
                    StartUnoGame();
            }
        }

        if (!inPlayMode)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) InspectItem(tabletItem); 
            if (Keyboard.current.digit2Key.wasPressedThisFrame) InspectItem(magazineItem); 
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentInspectedItem != null) CloseInspectedItem(); 
            else if (isLobbyPaperInspected) CloseLobby(); 
            else if (inPlayMode) BackToMainMenu(); 
            else BackToTitleScreen(); 
        }
    }

    private void StartIntroTransition()
    {
        if (cameraRig == null || mainCam == null || cameraTablePoint == null) return;

        isAtTable = true;
        isAnimatingIntro = true;
        CameraBob camBob = mainCam.GetComponent<CameraBob>();

        if (camBob != null) camBob.isEnabled = false; 

        Sequence introSeq = DOTween.Sequence();
        Vector3 approachRot = new Vector3(cameraStartPoint.eulerAngles.x, cameraTablePoint.eulerAngles.y, cameraTablePoint.eulerAngles.z);

        // Animate the RIG instead of the camera
        introSeq.Append(cameraRig.DOJump(cameraTablePoint.position, 0.15f, 4, 2f).SetEase(Ease.Linear));
        introSeq.Join(cameraRig.DORotate(approachRot, 2f).SetEase(Ease.InOutSine));
        introSeq.Append(cameraRig.DORotate(cameraTablePoint.eulerAngles, 1f).SetEase(Ease.InOutQuad));

        introSeq.OnComplete(() =>
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

    public void BackToTitleScreen()
    {
        if (cameraRig == null || mainCam == null || cameraStartPoint == null) return;

        isAtTable = false;
        isAnimatingIntro = true; 
        CameraBob camBob = mainCam.GetComponent<CameraBob>();

        if (camBob != null) camBob.isEnabled = false;

        Sequence outroSeq = DOTween.Sequence();
        
        outroSeq.Append(cameraRig.DOMove(cameraStartPoint.position, 1.5f).SetEase(Ease.InOutSine));
        outroSeq.Join(cameraRig.DORotate(cameraStartPoint.eulerAngles, 1.5f).SetEase(Ease.InOutSine));

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
        
        if (currentInspectedItem == item)
        {
            CloseInspectedItem();
            return;
        }

        if (currentInspectedItem != null)
        {
            Sequence swapSeq = DOTween.Sequence();
            swapSeq.Append(currentInspectedItem.itemModel.transform.DOMove(currentInspectedItem.originalTablePos, 0.4f).SetEase(Ease.OutQuad));
            swapSeq.Join(currentInspectedItem.itemModel.transform.DORotate(currentInspectedItem.originalTableRot, 0.4f).SetEase(Ease.OutQuad));

            swapSeq.AppendCallback(() => currentInspectedItem = item);
            swapSeq.Append(item.itemModel.transform.DOMove(inspectPoint.position, 0.5f).SetEase(Ease.OutQuad));
            swapSeq.Join(item.itemModel.transform.DORotate(inspectPoint.eulerAngles, 0.5f).SetEase(Ease.OutQuad));
        }
        else
        {
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
        isAnimating = true;
        inPlayMode = true;
        Sequence playSeq = DOTween.Sequence();

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

            if (gameBoxes.Count > 1 && offCenterRightPoint != null)
            {
                int nextIndex = (currentGameIndex + 1) % gameBoxes.Count;
                GameObject nextBox = gameBoxes[nextIndex].boxRoot;
                
                if (nextBox != null)
                {
                    playSeq.Join(nextBox.transform.DOMove(offCenterRightPoint.position, 0.5f).SetEase(Ease.OutQuad));
                    playSeq.Join(nextBox.transform.DORotate(offCenterRightPoint.eulerAngles, 0.5f).SetEase(Ease.OutQuad));
                }
            }
        }
        playSeq.OnComplete(() => isAnimating = false);
    }

    public void NextGame()
    {
        if (gameBoxes == null || gameBoxes.Count <= 1) return;
        isAnimating = true;
        Sequence nextSeq = DOTween.Sequence();
        GameObject currentBox = gameBoxes[currentGameIndex].boxRoot;
        
        if (currentBox != null && offCenterRightPoint != null)
        {
            nextSeq.Insert(0, currentBox.transform.DOMove(offCenterRightPoint.position, 0.5f).SetEase(Ease.InQuad));
            nextSeq.Insert(0, currentBox.transform.DORotate(offCenterRightPoint.eulerAngles, 0.5f).SetEase(Ease.InQuad));
        }

        currentGameIndex = (currentGameIndex + 1) % gameBoxes.Count;
        GameObject newBox = gameBoxes[currentGameIndex].boxRoot;

        if (newBox != null && offCenterLeftPoint != null && tableCenterPoint != null)
        {
            newBox.transform.position = offCenterLeftPoint.position;
            newBox.transform.rotation = offCenterLeftPoint.rotation;
            nextSeq.Insert(0.2f, newBox.transform.DOMove(tableCenterPoint.position, 0.5f).SetEase(Ease.OutQuad));
            nextSeq.Insert(0.2f, newBox.transform.DORotate(tableCenterPoint.eulerAngles, 0.5f).SetEase(Ease.OutQuad));
        }
        nextSeq.OnComplete(() => isAnimating = false);
    }

    public void PreviousGame()
    {
        if (gameBoxes == null || gameBoxes.Count <= 1) return;
        isAnimating = true;
        Sequence prevSeq = DOTween.Sequence();
        GameObject currentBox = gameBoxes[currentGameIndex].boxRoot;
        
        if (currentBox != null && offCenterLeftPoint != null)
        {
            prevSeq.Insert(0, currentBox.transform.DOMove(offCenterLeftPoint.position, 0.5f).SetEase(Ease.InQuad));
            prevSeq.Insert(0, currentBox.transform.DORotate(offCenterLeftPoint.eulerAngles, 0.5f).SetEase(Ease.InQuad));
        }

        currentGameIndex--;
        if (currentGameIndex < 0) currentGameIndex = gameBoxes.Count - 1;

        GameObject newBox = gameBoxes[currentGameIndex].boxRoot;

        if (newBox != null && offCenterRightPoint != null && tableCenterPoint != null)
        {
            newBox.transform.position = offCenterRightPoint.position;
            newBox.transform.rotation = offCenterRightPoint.rotation;
            prevSeq.Insert(0.2f, newBox.transform.DOMove(tableCenterPoint.position, 0.5f).SetEase(Ease.OutQuad));
            prevSeq.Insert(0.2f, newBox.transform.DORotate(tableCenterPoint.eulerAngles, 0.5f).SetEase(Ease.OutQuad));
        }
        prevSeq.OnComplete(() => isAnimating = false);
    }

    public void OpenLobby()
    {
        if (gameBoxes == null || gameBoxes.Count == 0 || inspectPoint == null) return;
        
        isLobbyPaperInspected = true;
        GameBoxData currentData = gameBoxes[currentGameIndex];
        Sequence openSeq = DOTween.Sequence();

        if (currentData.boxLid != null)
        {
            Vector3 upPos = currentData.lidOriginalLocalPos + (Vector3.up * lidLiftHeight);
            openSeq.Append(currentData.boxLid.DOLocalMove(upPos, 0.25f).SetEase(Ease.OutQuad));
            Vector3 restPos = upPos + lidRestOffset;
            openSeq.Append(currentData.boxLid.DOLocalMove(restPos, 0.3f).SetEase(Ease.InOutSine));
        }

        if (currentData.lobbyPaper != null && paperInspectPoint != null)
        {
            openSeq.Append(currentData.lobbyPaper.DOMove(paperInspectPoint.position, 0.5f).SetEase(Ease.OutBack));
            openSeq.Join(currentData.lobbyPaper.DORotate(paperInspectPoint.eulerAngles, 0.5f).SetEase(Ease.OutBack));
        }
    }

    public void CloseLobby()
    {
        if (gameBoxes == null || gameBoxes.Count == 0) return;
        
        GameBoxData currentData = gameBoxes[currentGameIndex];
        Sequence closeSeq = DOTween.Sequence();

        if (currentData.lobbyPaper != null)
        {
            closeSeq.Append(currentData.lobbyPaper.DOLocalMove(currentData.paperOriginalLocalPos, 0.5f).SetEase(Ease.InOutQuad));
            closeSeq.Join(currentData.lobbyPaper.DOLocalRotate(currentData.paperOriginalLocalRot, 0.5f).SetEase(Ease.InOutQuad));
        }

        if (currentData.boxLid != null)
        {
            Vector3 upPos = currentData.lidOriginalLocalPos + (Vector3.up * lidLiftHeight);
            closeSeq.Append(currentData.boxLid.DOLocalMove(upPos, 0.3f).SetEase(Ease.InOutSine));
            closeSeq.Append(currentData.boxLid.DOLocalMove(currentData.lidOriginalLocalPos, 0.25f).SetEase(Ease.InQuad));
        }

        closeSeq.OnComplete(() => isLobbyPaperInspected = false);
    }

    public void BackToMainMenu()
    {
        inPlayMode = false;
        Sequence backSeq = DOTween.Sequence();

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

        if (tabletItem?.itemModel != null)
        {
            backSeq.Join(tabletItem.itemModel.transform.DOMove(tabletItem.originalTablePos, 0.5f).SetEase(Ease.InOutQuad));
            backSeq.Join(tabletItem.itemModel.transform.DORotate(tabletItem.originalTableRot, 0.5f).SetEase(Ease.InOutQuad));
        }

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

        if (cameraRig != null && mainCam != null && unoCameraPoint != null)
        {
            CameraBob camBob = mainCam.GetComponent<CameraBob>();
            if (camBob != null) camBob.isEnabled = false;

            // Move the Rig instead of the Camera
            unoSeq.Insert(0, cameraRig.DOMove(unoCameraPoint.position, 1.5f).SetEase(Ease.InOutSine));
            unoSeq.Insert(0, cameraRig.DORotate(unoCameraPoint.eulerAngles, 1.5f).SetEase(Ease.InOutSine));
            
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

        if (unoSeatModels != null && unoSeatAnchors != null)
        {
            for (int i = 0; i < unoSeatModels.Count; i++)
            {
                if (i < unoSeatAnchors.Count && unoSeatModels[i] != null && unoSeatAnchors[i] != null)
                {
                    Vector3 anchorPos = unoSeatAnchors[i].position;
                    unoSeatModels[i].transform.position = anchorPos + (Vector3.down * undergroundOffset);
                    unoSeatModels[i].transform.rotation = unoSeatAnchors[i].rotation;
                    unoSeq.Insert(0.5f, unoSeatModels[i].transform.DOMove(anchorPos, 1f).SetEase(Ease.OutBack));
                }
            }
        }

        Sequence boxSeq = DOTween.Sequence();
        boxSeq.AppendInterval(1f);
        
        if (currentBox.boxLid != null)
            boxSeq.Append(currentBox.boxLid.DOLocalMove(currentBox.lidOriginalLocalPos + (Vector3.up * lidLiftHeight), 0.4f).SetEase(Ease.OutBack));

        boxSeq.AppendCallback(() => 
        { 
            if (unoDeckManager != null && currentBox.boxRoot != null) 
            {
                unoDeckManager.SpawnDeckFromBox(currentBox.boxRoot.transform);
                unoDeckManager.StartUnoGame();
            }
        });

        boxSeq.AppendInterval(2.5f);

        if (currentBox.boxLid != null)
            boxSeq.Append(currentBox.boxLid.DOLocalMove(currentBox.lidOriginalLocalPos, 0.4f).SetEase(Ease.InBack));

        if (currentBox.boxRoot != null && sideTableGameBoxesPoint != null)
        {
            boxSeq.Append(currentBox.boxRoot.transform.DOMove(sideTableGameBoxesPoint.position, 1f).SetEase(Ease.InOutBack));
            boxSeq.Join(currentBox.boxRoot.transform.DORotate(sideTableGameBoxesPoint.eulerAngles, 1f).SetEase(Ease.InOutBack));
        }
    }
}