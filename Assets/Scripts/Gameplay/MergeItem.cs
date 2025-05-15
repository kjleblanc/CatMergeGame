using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

[RequireComponent(typeof(Image))] 
[RequireComponent(typeof(CanvasGroup))] 
public class MergeItem : InteractiveGridEntity, IDropHandler 
{
    [Header("Data")]
    public MergeItemData itemData;
    // --- Components ---
    private Animator animator;

    protected override void Awake()
    {
        base.Awake(); // Calls InteractiveGridEntity.Awake()

        // Assign the Image component on this GameObject to the base class's entityImage
        this.entityImage = GetComponent<Image>();
        if (this.entityImage == null)
        {
            Debug.LogError($"{gameObject.name} (MergeItem) is missing an Image component for its entityImage!");
        }
        
        animator = GetComponent<Animator>();
        // mainCanvas is found by base.Awake()
    }
    public void InitializeItem(MergeItemData data, GridSlot slot, GridManager manager)
    {
        this.itemData = data;
        if (data == null)
        {
            Debug.LogError($"InitializeItem called on {gameObject.name} with null MergeItemData!");
            if (this.entityImage == null) this.entityImage = GetComponent<Image>();
            if (this.entityImage != null) this.entityImage.color = Color.magenta;
            base.InitializeBaseParameters(slot, manager, 1f, "Error Item");
            return;
        }

        base.InitializeBaseParameters(slot, manager, data.visualScaleMultiplier, data.displayName);

        if (this.entityImage == null)
        {
            this.entityImage = GetComponent<Image>();
            if (this.entityImage == null)
            {
                Debug.LogError($"{data.displayName} (MergeItem): CRITICAL - Image component missing from prefab and cannot be fetched! Visuals will fail.");
                return;
            }
        }

        if (animator == null) animator = GetComponent<Animator>();

        
        bool useAnimator = (itemData.itemAnimatorController != null && animator != null);

        if (useAnimator)
        {
            animator.runtimeAnimatorController = itemData.itemAnimatorController;
            this.entityImage.sprite = null; 
            this.entityImage.color = Color.white;
            animator.enabled = true; 
        }
        else
        {
            if (itemData.displayIcon != null)
            {
                this.entityImage.sprite = itemData.displayIcon;
                this.entityImage.color = Color.white;
            }
            else
            {
                this.entityImage.sprite = null;
                this.entityImage.color = Color.cyan;
                Debug.LogWarning($"{data.displayName} (MergeItem) is missing displayIcon and not using animator. Displaying as cyan square.");
            }
            if(animator != null) animator.enabled = false;
        }
    }
   
    // This method is called ON THE ITEM BEING DROPPED ONTO (the stationary one)
    public void OnDrop(PointerEventData eventData)
    {
        if (this.isLocked) // Check if THIS item (the target) is locked
        {
            Debug.Log($"{this.gameObject.name} is locked and cannot be used as a merge target.");
            // The dragged item will then snap back via its own OnEndDrag logic in InteractiveGridEntity
            return;
        }

        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        MergeItem draggedItem = droppedObject.GetComponent<MergeItem>();

        if (draggedItem == null || draggedItem == this || this.itemData == null || draggedItem.itemData == null)
        {
            return;
        }
        
        // Prevent merging with a locked dragged item (though its OnBeginDrag should prevent dragging if locked)
        if (draggedItem.isLocked) {
            Debug.Log($"OnDrop: Dragged item {draggedItem.itemData.displayName} is locked. Merge aborted.");
            return;
        }

        if (draggedItem.itemData.level == this.itemData.level)
        {
            MergeItemData nextLevelData = this.itemData.nextLevelItem;
            if (nextLevelData != null)
            {
                GameObject prefabToSpawn = nextLevelData.prefab;
                if (prefabToSpawn == null) {
                    Debug.LogError($"Merge Failed: Prefab not set for next level item '{nextLevelData.displayName}' in its ScriptableObject!");
                    return;
                }

                Debug.Log($"Merge Success: Merging {this.itemData.displayName} with {draggedItem.itemData.displayName} into {nextLevelData.displayName}");

                GridSlot targetSlotForNewItem = this.currentSlot; 

                // IMPORTANT: The GridManager needs to know the slot of the dragged item is now permanently free
                // *before* destroying it, if it wasn't already cleared by its OnBeginDrag for some reason
                // (though it should be).
                // If draggedItem.currentSlot was correctly cleared by its OnBeginDrag, this is just a precaution or for clarity.
                if (gridManager != null && draggedItem.currentSlot != null && gridManager.GetItemInSlot(draggedItem.currentSlot) == draggedItem) {
                     // This should not happen if OnBeginDrag worked, as its slot would be null in the dictionary
                     // Debug.LogWarning($"Dragged item {draggedItem.name} was still registered in its slot {draggedItem.currentSlot.name} before merge destruction. Clearing now.");
                     // gridManager.ClearSlot(draggedItem.currentSlot);
                }


                // 1. Destroy the item that was being dragged
                Destroy(draggedItem.gameObject);

                // 2. Destroy this item (the one that received the drop)
                // Its slot will be cleared by GridManager when this item is destroyed,
                // or we can explicitly clear it first.
                // gridManager.ClearSlot(targetSlotForNewItem); // No, PlaceItem will handle this.
                Destroy(this.gameObject);

                // 3. Instantiate the new merged item in the target slot
                GameObject newItemGO = Instantiate(prefabToSpawn, targetSlotForNewItem.transform);
                MergeItem newItem = newItemGO.GetComponent<MergeItem>();

                if (newItem != null)
                {
                    newItem.InitializeItem(nextLevelData, targetSlotForNewItem, gridManager); // Use InitializeItem
                    // GridManager.PlaceItem is called inside InitializeItem via base.InitializeBaseParameters -> MoveToSlot -> PlaceItem indirectly,
                    // OR more directly if InitializeItem calls gridManager.PlaceItem(this, newSlot)
                    // For clarity, ensure PlaceItem is called for the new item:
                    gridManager.PlaceItem(newItem, targetSlotForNewItem);
                }
                else
                {
                    Debug.LogError($"Merge Error: Instantiated prefab '{prefabToSpawn.name}' is missing MergeItem component!");
                    Destroy(newItemGO); 
                    gridManager.ClearSlot(targetSlotForNewItem); // Ensure slot is marked empty if spawn failed
                }
            }
            else
            {
                // Debug.Log("Merge Attempted: Items match level, but target is at max level for this chain.");
            }
        }
        else
        {
            // Debug.Log("Merge Attempted: Item levels do not match.");
        }
    }
}