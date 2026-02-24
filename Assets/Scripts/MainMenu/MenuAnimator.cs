using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MenuAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Duration for the slide in and out animations.")]
    public float duration = 0.6f;
    [Tooltip("Ease type for sliding in from the right.")]
    public Ease easeIn = Ease.OutBack;
    [Tooltip("Ease type for sliding out to the right.")]
    public Ease easeOut = Ease.InBack;
    [Tooltip("The offset applied to move the object offscreen to the right.")]
    public Vector3 offscreenOffset = new Vector3(6f, 0f, 0f);

    private Dictionary<Transform, Vector3> _originalPositions = new Dictionary<Transform, Vector3>();

    private void EnsureOriginalPositionSaved(Transform t)
    {
        if (!_originalPositions.ContainsKey(t))
        {
            _originalPositions[t] = t.localPosition;
        }
    }

    /// <summary>
    /// Slides the current object out to the right, and the new object in from the right.
    /// Both animations happen simultaneously.
    /// </summary>
    public void SwitchObjects(Transform outObj, Transform inObj, Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();

        if (outObj != null)
        {
            EnsureOriginalPositionSaved(outObj);
            Vector3 outTarget = _originalPositions[outObj] + offscreenOffset;
            
            // Slide out
            seq.Append(outObj.DOLocalMove(outTarget, duration)
                .SetEase(easeOut)
                .OnComplete(() => outObj.gameObject.SetActive(false)));
        }

        if (inObj != null)
        {
            EnsureOriginalPositionSaved(inObj);
            Vector3 centerPos = _originalPositions[inObj];
            
            inObj.gameObject.SetActive(true);
            // Start offscreen
            inObj.localPosition = centerPos + offscreenOffset;
            
            // Slide in (join so it plays concurrently with outObj's slide out)
            if (outObj != null)
                seq.Join(inObj.DOLocalMove(centerPos, duration).SetEase(easeIn));
            else
                seq.Append(inObj.DOLocalMove(centerPos, duration).SetEase(easeIn));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// Plays the Lobby transition: Opens the game box lid and slides the paper out onto the table.
    /// </summary>
    public void OpenBoxAndShowPaper(Transform boxLid, Transform paper, Transform paperTargetPoint, Action onComplete = null)
    {
        if (paper == null || boxLid == null || paperTargetPoint == null) return;

        EnsureOriginalPositionSaved(paper);
        
        paper.gameObject.SetActive(true);
        // Start the paper slightly inside/under the box lid
        paper.position = boxLid.position; 
        
        Sequence seq = DOTween.Sequence();
        
        // 1. Open the lid (adjust the rotation axis based on your 3D model, using X axis here)
        seq.Append(boxLid.DOLocalRotate(new Vector3(-110f, 0, 0), 0.5f).SetEase(Ease.OutQuad));
        
        // 2. Slide the paper out to its resting point on the table
        seq.Append(paper.DOMove(paperTargetPoint.position, 0.6f).SetEase(Ease.OutBack));
        
        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// Reverts the box and paper back to the Game Selection state.
    /// </summary>
    public void CloseBoxAndHidePaper(Transform boxLid, Transform paper)
    {
        if (paper != null && _originalPositions.ContainsKey(paper))
        {
            paper.localPosition = _originalPositions[paper];
            paper.gameObject.SetActive(false);
        }

        if (boxLid != null)
        {
            // Reset lid rotation
            boxLid.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InQuad);
        }
    }
}
