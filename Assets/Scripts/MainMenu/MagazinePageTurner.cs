using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class MagazinePageTurner : MonoBehaviour
{
    [Header("Page Setup")]
    [Tooltip("List of page planes. Index 0 is visually on top of the right stack.")]
    public List<GameObject> pages = new List<GameObject>();
    
    [Tooltip("Vertical spacing to prevent Z-fighting.")]
    public float pageThickness = 0.001f;

    [Header("Animation Setup")]
    public float turnDuration = 0.5f;

    // Track the index of the next page to be turned to the left.
    private int currentPageIndex = 0;
    
    // Prevent spamming
    private bool isTurning = false;

    private void Start()
    {
        // Initialization: Set initial local Y positions so they stack correctly on the right side.
        // Index 0 is the highest Y, last index is the lowest Y.
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] == null) continue;

            // Compute Y position for the right stack
            float startY = (pages.Count - 1 - i) * pageThickness;
            
            // Apply initial position
            Vector3 pos = pages[i].transform.localPosition;
            pos.y = startY;
            pages[i].transform.localPosition = pos;

            // Ensure initial rotation is completely flat on the right
            pages[i].transform.localEulerAngles = Vector3.zero;
        }
    }


    /// <summary>
    /// Turns the current page forward (from the right stack to the left stack).
    /// </summary>
    public void NextPage()
    {
        // Array bounds checking and spam prevention
        if (isTurning || currentPageIndex >= pages.Count) return;

        isTurning = true;
        
        // Identify the current page
        GameObject page = pages[currentPageIndex];
        
        // Calculate its new target Y-position on the left stack.
        // The first page turned (index 0) becomes the lowest Y (0 * thickness).
        // Subsequent pages stack on top of it.
        float targetY = currentPageIndex * pageThickness;

        // Simultaneously use sequence or individual tweens
        // Rotate 180 degrees around the Z-axis (Spine)
        // Use FastBeyond360 so DOTween strictly follows the negative math
        page.transform.DOLocalRotate(new Vector3(-180f, 0, 0), turnDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad);
        
        // Move to its new calculated left-stack height
        page.transform.DOLocalMoveY(targetY, turnDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                isTurning = false;
            });

        // Advance to the next page
        currentPageIndex++;
    }

    /// <summary>
    /// Turns the current page backward (from the left stack back to the right stack).
    /// </summary>
    public void PreviousPage()
    {
        // Array bounds checking and spam prevention
        if (isTurning || currentPageIndex <= 0) return;

        isTurning = true;
        
        // Identify the previous page on the left stack
        currentPageIndex--;
        GameObject page = pages[currentPageIndex];

        // Original right-stack height calculation
        float targetY = (pages.Count - 1 - currentPageIndex) * pageThickness;

        // Rotate back to 0 degrees
        // Use FastBeyond360 to force it to return the exact same way it came
        page.transform.DOLocalRotate(new Vector3(180f, 0, 0), turnDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.InOutQuad);

        // Return it to its original right-stack height
        page.transform.DOLocalMoveY(targetY, turnDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                isTurning = false;
            });
    }
}
