using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // This is for the slot's background image itself
public class GridSlot : MonoBehaviour
{
    [Header("UI Elements")]
    public Image lockIconImage; // Assign a child Image component here in the prefab
    public Image highlightBorderImage;

    void Awake()
    {
        // Ensure the lock icon is hidden initially by default
        if (lockIconImage != null)
        {
            lockIconImage.gameObject.SetActive(false);
        }
        // Initialize highlight border to be hidden
        if (highlightBorderImage != null)
        {
            highlightBorderImage.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"HighlightBorderImage not assigned on slot: {gameObject.name}. Highlighting will not work.");
        }
    }
    public void SetLockIconVisibility(bool isVisible)
    {
        if (lockIconImage != null)
        {
            lockIconImage.gameObject.SetActive(isVisible);
        }
        else
        {
            // Debug.LogWarning($"LockIconImage not assigned on slot: {gameObject.name}");
        }
    }
    public void SetHighlight(bool isHighlighted)
{
    if (highlightBorderImage != null)
    {
        highlightBorderImage.gameObject.SetActive(isHighlighted);
    }
}
}