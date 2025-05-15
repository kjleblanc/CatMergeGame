using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Common interfaces for all interactive grid entities
public abstract class InteractiveGridEntity : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Base Entity References")]
    [HideInInspector] public GridSlot currentSlot;
    [HideInInspector] public GridManager gridManager;
    protected Canvas mainCanvas; // The root canvas for dragging

    [Header("Base Entity State")]
    public bool isLocked = false;
    protected bool isBeingDragged = false;
    protected bool lockStateJustChangedByHold = false; // True if lock state was changed by the most recent hold action
    protected bool justFinishedDrag = false; // True if the entity was just dragged and dropped

    [Header("Base Visual & Interaction Settings")]
    [Tooltip("How long to hold down to toggle lock state")]
    public float requiredHoldTimeToLock = 2.0f;
    public float wobbleSpeed = 15f;
    public float wobbleAmplitude = 5f;

    // --- Components (Assign in derived class's Awake or make SerializeField and assign in Prefab) ---
    protected Image entityImage; // Primary visual - to be assigned by derived class
    protected CanvasGroup canvasGroup;
    protected RectTransform rectTransform;
    protected Transform originalParent;

    // --- Long Press Lock State ---
    protected float pointerDownTimer = 0f;
    protected bool isPointerDown = false;
    // protected bool lockStateJustChangedByHold = false; // Already declared under Base Entity State

    protected virtual void Awake()
    {
        // Get common components
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        // entityImage should be assigned by derived class's Awake
        // For example, in derived Awake: this.entityImage = GetComponent<Image>(); 

        mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError($"{gameObject.name}: Could not find a parent Canvas!");
        }

        if (canvasGroup == null) Debug.LogWarning($"{gameObject.name}: CanvasGroup component missing! Make sure it's on the prefab.");
        if (rectTransform == null) Debug.LogWarning($"{gameObject.name}: RectTransform component missing! Make sure it's on the prefab.");
    }
    protected virtual void Update()
    {
        // Long Press Lock Logic
        if (isPointerDown && !isBeingDragged)
        {
            pointerDownTimer += Time.deltaTime;
            if (pointerDownTimer >= requiredHoldTimeToLock)
            {
                ToggleLockState(); // This will set lockStateJustChangedByHold = true
                isPointerDown = false; // Reset to prevent multiple locks from one hold
                pointerDownTimer = 0f; // Reset timer
            }
        }
        // Wobble Effect
        if (isBeingDragged && rectTransform != null)
        {
            float wobbleAngle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmplitude;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, wobbleAngle);
        }
    }
    public virtual void InitializeBaseParameters(GridSlot slot, GridManager manager, float visualScale, string entityName)
    {
        this.currentSlot = slot;
        this.gridManager = manager;

        // Ensure rectTransform is fetched if it wasn't by Awake
        if (this.rectTransform == null)
        {
            this.rectTransform = GetComponent<RectTransform>();
            if (this.rectTransform == null) // Still null? This is a more serious prefab issue.
            {
                Debug.LogError($"{entityName}: RectTransform component is missing from the prefab and could not be fetched! Scale cannot be set.");
                // Optionally handle this more gracefully, e.g., don't try to set scale.
            }
        }

        if (this.rectTransform != null) // Check again after attempting to fetch
        {
            this.rectTransform.localScale = Vector3.one * visualScale;
        }
        else
        {

        }

        this.gameObject.name = entityName;
        this.isLocked = false;

        // Ensure canvasGroup is fetched if it wasn't by Awake
        if (this.canvasGroup == null)
        {
            this.canvasGroup = GetComponent<CanvasGroup>();
            if (this.canvasGroup == null) // Still null?
            {
                Debug.LogWarning($"{entityName}: CanvasGroup component is missing from the prefab and could not be fetched!");
            }
        }

        if (this.canvasGroup != null) // Check again
        {
            this.canvasGroup.alpha = 1f;
            this.canvasGroup.blocksRaycasts = true;
        }
        else
        {
            
        }

        UpdateLockVisual();
    }
    public virtual void ToggleLockState() // This method is called by the long press logic in Update
    {
        isLocked = !isLocked;
        Debug.Log($"{gameObject.name} is now {(isLocked ? "LOCKED" : "UNLOCKED")} by hold action.");
        lockStateJustChangedByHold = true; // Set the flag
        UpdateLockVisual();
    }

    public void UpdateLockVisual()
    {
        if (currentSlot != null)
        {
            currentSlot.SetLockIconVisibility(isLocked);
        }
    }
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        // Reset flags when a new press starts
        lockStateJustChangedByHold = false; 
        justFinishedDrag = false;

        if (isBeingDragged) return; // Should not happen if drag ended correctly
        isPointerDown = true;
        pointerDownTimer = 0f;
        // Debug.Log($"{gameObject.name} OnPointerDown. Timer started.");
    }
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        // Debug.Log($"{gameObject.name} OnPointerUp. Timer was {pointerDownTimer}.");
        // If it was a short tap, derived classes might handle via IPointerClickHandler
    }
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        justFinishedDrag = false; // Reset if a drag is initiated

        if (isLocked)
        {
            Debug.Log($"{gameObject.name} is locked and cannot be dragged.");
            eventData.pointerDrag = null; // Cancel drag
            isPointerDown = false; // Ensure long press logic stops
            pointerDownTimer = 0f;
            return;
        }

        if (isPointerDown) // Cancel any pending long press if drag starts
        {
            // Debug.Log($"Drag started for {gameObject.name}, cancelling any pending long press. Timer was: {pointerDownTimer}");
            isPointerDown = false;
            pointerDownTimer = 0f;
        }

        if (gridManager != null)
        {
            gridManager.ClearSelectionBeforeDrag();
        }

        isBeingDragged = true;
        originalParent = transform.parent;

        if (rectTransform != null) rectTransform.localRotation = Quaternion.identity; // Reset wobble before picking up


        if (mainCanvas != null) 
        {
            transform.SetParent(mainCanvas.transform, true);
            transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError($"{gameObject.name}: MainCanvas is null. Cannot reparent for drag.");
            isBeingDragged = false; // Critical error, stop drag
            eventData.pointerDrag = null;
            return;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }

        if (gridManager != null && currentSlot != null)
        {
            gridManager.ClearSlot(currentSlot); // Slot is cleared, so lock icon on slot should be off (handled by ClearSlot)
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: GridManager or CurrentSlot is null on BeginDrag.");
        }
    }
    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!isBeingDragged) return;
        if (mainCanvas == null || rectTransform == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform,
            eventData.position,
            mainCanvas.worldCamera,
            out Vector2 localPoint);
        rectTransform.localPosition = localPoint;
    }
    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!isBeingDragged) return;        
        // Important: Set justFinishedDrag true *before* any potential destruction or early exit
        justFinishedDrag = true; 
        isBeingDragged = false; // Reset drag state first

        if (rectTransform != null) rectTransform.localRotation = Quaternion.identity; // Reset wobble

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
        else // If canvasGroup is somehow null now (e.g. object destroyed prematurely), log and potentially exit
        {
            Debug.LogWarning($"{gameObject.name} OnEndDrag: CanvasGroup became null. Item might have been destroyed.");
            // If the item was destroyed (e.g., by a merge), originalParent might also be problematic.
            // The `this == null` check later will catch destroyed GameObjects.
        }


        GameObject dropTargetObject = eventData.pointerCurrentRaycast.gameObject;
        GridSlot targetSlotComponent = null; // Renamed to avoid conflict with local var 'targetSlot' transform
        bool successfullyPlacedOrHandled = false; // True if moved to an empty slot, or if an IDropHandler consumed/handled it

        if (dropTargetObject != null)
        {
            targetSlotComponent = dropTargetObject.GetComponent<GridSlot>() ?? dropTargetObject.GetComponentInParent<GridSlot>();
        }
        
        // This check is crucial if the item was destroyed by an OnDrop handler (e.g., during a merge)
        if (this == null || this.gameObject == null) {
            // Debug.Log($"{gameObject.name} was destroyed during drag (e.g. by merge), OnEndDrag cleanup skipped for this instance.");
            return;
        }

        // Scenario 1: Dropped onto a valid, empty, different GridSlot
        if (targetSlotComponent != null && gridManager != null && !gridManager.IsSlotOccupied(targetSlotComponent) && targetSlotComponent.transform != originalParent)
        {
            // Debug.Log($"Moving {gameObject.name} to empty slot: {targetSlotComponent.name}");
            MoveToSlot(targetSlotComponent);
            successfullyPlacedOrHandled = true;
        }
        // Scenario 2: Dropped back onto its original slot (or something within it that isn't another empty slot)
        else if (targetSlotComponent != null && targetSlotComponent.transform == originalParent)
        {
            // Debug.Log($"{gameObject.name} dropped back onto original slot: {targetSlotComponent.name}");
            // currentSlot should still be the original slot if it wasn't successfully placed elsewhere.
            MoveToSlot(currentSlot); 
            successfullyPlacedOrHandled = true;
        }
        // Scenario 3: Item was not placed in an empty slot, and not snapped back to original explicitly.
        // It might have been handled by an IDropHandler on another item (e.g., for merging).
        // If transform.parent is NOT mainCanvas, it means something (MoveToSlot or an IDropHandler that reparented then destroyed this) took care of it.
        else if (transform.parent != mainCanvas.transform)
        {
            // This case implies the item was successfully re-parented by some logic (e.g., MoveToSlot, or even an external handler if it reparented before destroying this)
            // Or, it was destroyed and this OnEndDrag is on a "ghost". The null check above should catch destroyed items.
            // If it reached here and wasn't destroyed, it means it was re-parented to a slot.
            successfullyPlacedOrHandled = true; // Assume it was handled if parent changed
        }


        // Scenario 4: Invalid drop, not handled by a target, still child of canvas. Snap back to original slot.
        if (!successfullyPlacedOrHandled && transform.parent == mainCanvas.transform)
        {
            // Debug.Log($"{gameObject.name}: Invalid drop or not handled by target. Returning to original slot.");
            if (originalParent != null && currentSlot != null && gridManager != null)
            {
                // currentSlot should still hold the original slot reference here if it wasn't successfully moved
                MoveToSlot(currentSlot); 
            }
            else
            {
                Debug.LogError($"{gameObject.name}: Cannot snap back. Original parent, current slot, or GridManager is missing. Item might be lost or in an invalid state.");
                // Potentially destroy the item if it's in an invalid state to prevent issues.
                // Destroy(gameObject); 
            }
        }
        
        UpdateLockVisual(); // Ensure final slot shows correct lock state
        originalParent = null; // Clear original parent reference
    }
    protected virtual void MoveToSlot(GridSlot newSlot)
    {
        if (newSlot == null)
        {
            Debug.LogError($"{gameObject.name}: Attempted to move to a null slot.");
            // Snap back to original if possible, or handle error
            if (originalParent != null && currentSlot != null && gridManager != null) {
                 transform.SetParent(originalParent); // originalParent is a Transform
                 if (rectTransform) rectTransform.anchoredPosition = Vector2.zero;
                 // currentSlot is already correct (original)
                 if (gridManager) gridManager.PlaceItem(this, currentSlot);
            } else {
                Debug.LogError($"{gameObject.name}: Critical error - cannot move to null slot and cannot snap back to original.");
            }
            return;
        }

        transform.SetParent(newSlot.transform);
        if (rectTransform) rectTransform.anchoredPosition = Vector2.zero;
        
        GridSlot oldSlot = currentSlot; // Keep track of the old slot for debugging or other logic if needed
        currentSlot = newSlot; // Update internal reference

        if (gridManager) gridManager.PlaceItem(this, newSlot); // Update grid state

        // Debug.Log($"{gameObject.name} moved from slot {(oldSlot != null ? oldSlot.name : "N/A")} to {newSlot.name}");
    }
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // Prevent click actions if a drag just finished or a lock was just toggled by holding.
        // These flags (justFinishedDrag, lockStateJustChangedByHold) should be reset
        // in OnPointerDown of the next interaction.
        if (justFinishedDrag || lockStateJustChangedByHold)
        {
            return;
        }

        // Even if locked, we might want to select it to show info.
        // If you want to prevent selection of locked items, add: if (isLocked) return;

        // Notify GridManager about the selection
        if (gridManager != null && currentSlot != null) // Ensure currentSlot is valid
        {
            gridManager.HandleEntitySelected(this); 
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} clicked, but GridManager or currentSlot is null. Cannot process selection.");
        }
    }
}