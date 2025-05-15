using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [Tooltip("Assign the parent GameObject holding all GridSlot objects")]
    public GameObject gridPanelObject; // Assign the 'GridPanel' UI object here

    [Header("Starting Objects")]
    [Tooltip("Assign the ScriptableObject for the initial spawner")]
    public SpawnerData startingSpawnerData; // Assign 'Spawner_KibbleBag' SO here

    // --- Runtime Data ---
    private List<GridSlot> gridSlots = new List<GridSlot>();
    // Using MonoBehaviour allows storing MergeItem or ItemSpawner references
    private Dictionary<GridSlot, InteractiveGridEntity> slotOccupancy = new Dictionary<GridSlot, InteractiveGridEntity>();
    private InteractiveGridEntity currentlySelectedEntity = null;
    private GridSlot currentlySelectedSlot = null;
    private PersistentUIManager persistentUIManager;

    void Awake()
    {
        InitializeGrid();
    }
    void Start()
    {
        var saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
        if (saveLoadManager == null || (saveLoadManager != null && !saveLoadManager.HasSaveFile())) // Add HasSaveFile() to SaveLoadManager
        {
            if(!HasAnyOccupiedSlots()) // Check if grid is empty after potential load attempt
            {
                PlaceStartingSpawner();
            }
        }
    }
    void InitializeGrid()
    {
        if (gridPanelObject == null) { /* ... */ return; }
        gridSlots = new List<GridSlot>(gridPanelObject.GetComponentsInChildren<GridSlot>(true));
        if (gridSlots.Count == 0) { /* ... */ return; }

        slotOccupancy.Clear(); // Clear before repopulating
        foreach (GridSlot slot in gridSlots)
        {
            slotOccupancy.Add(slot, null); // Add with null, indicating empty
        }
        Debug.Log($"GridManager initialized/re-initialized with {gridSlots.Count} slots. Slot occupancy map reset.");
    }
    public void ClearAllGridItemsAndSelection()
    {
        Debug.Log("Clearing all grid items and selection. Marking slots as empty.");
        List<InteractiveGridEntity> entitiesToDestroy = new List<InteractiveGridEntity>();
        foreach (var pair in slotOccupancy)
        {
            if (pair.Value != null)
            {
                entitiesToDestroy.Add(pair.Value);
            }
        }

        foreach(InteractiveGridEntity entity in entitiesToDestroy)
        {
            Destroy(entity.gameObject);
        }

        // Now, mark all slots in the dictionary as empty (value = null)
        // The keys (GridSlot instances) should remain from InitializeGrid.
        List<GridSlot> currentKeys = new List<GridSlot>(slotOccupancy.Keys);
        foreach (GridSlot slotKey in currentKeys)
        {
            slotOccupancy[slotKey] = null;
        }

        // Visual reset for slots
        if (gridSlots != null) // gridSlots list is populated in Awake
        {
            foreach (GridSlot slot in gridSlots)
            {
                if (slot != null)
                {
                    slot.SetLockIconVisibility(false);
                    slot.SetHighlight(false);
                }
            }
        }
        ClearSelection(); // Clears currentlySelectedEntity/Slot and UI info
    }
    public void PlaceStartingSpawner()
    {
        if (startingSpawnerData == null)
        {
            Debug.LogWarning("Starting Spawner Data is not assigned in GridManager. Cannot place initial spawner.");
            return;
        }
        if (startingSpawnerData.prefab == null)
        {
            Debug.LogError($"Spawner Prefab is not assigned on the '{startingSpawnerData.displayName}' SpawnerData!");
            return;
        }
        if (gridSlots.Count == 0)
        {
             Debug.LogError("Grid not initialized, cannot place starting spawner.");
             return;
        }

        // --- Place in Bottom-Right Slot ---
        // Assuming a standard layout (e.g., GridLayoutGroup), the last slot in the list
        // from GetComponentsInChildren should correspond to the bottom-right visually.
        // If layout is complex or order isn't guaranteed, might need a more robust way
        // (e.g., finding the slot with highest X and Y position).
        int targetSlotIndex = gridSlots.Count - 1; // Index of the last slot

        if (targetSlotIndex < 0) return; // No slots found

        GridSlot targetSlot = gridSlots[targetSlotIndex];

        // Check if the target slot is currently empty (it should be)
        if (IsSlotOccupied(targetSlot))
        {
            Debug.LogWarning($"Bottom-right slot (Index {targetSlotIndex}) is already occupied. Cannot place starting spawner.");
            // Optional: Try finding the next available empty slot?
            return;
        }

        // Instantiate the spawner prefab as a child of the target slot
        GameObject spawnerGO = Instantiate(startingSpawnerData.prefab, targetSlot.transform);
        ItemSpawner spawnerInstance = spawnerGO.GetComponent<ItemSpawner>();

        if (spawnerInstance != null)
        {
            // Initialize the spawner instance
            spawnerInstance.InitializeSpawner(startingSpawnerData, targetSlot, this);
            // Update the occupancy state
            PlaceItem(spawnerInstance, targetSlot); // Use PlaceItem to update dictionary
            Debug.Log($"Placed starting spawner '{startingSpawnerData.displayName}' in slot {targetSlotIndex}.");
        }
        else
        {
            Debug.LogError($"Instantiated spawner prefab '{startingSpawnerData.prefab.name}' does not have an ItemSpawner component!");
            Destroy(spawnerGO); // Clean up
        }
    }
    public bool IsSlotOccupied(GridSlot slot)
    {
        return slotOccupancy.ContainsKey(slot) && slotOccupancy[slot] != null;
    }
    public InteractiveGridEntity GetItemInSlot(GridSlot slot) 
    {
        if (slotOccupancy.TryGetValue(slot, out InteractiveGridEntity entity)) // Use TryGetValue for safety
        {
            return entity;
        }
        return null;
    }
    public void PlaceItem(InteractiveGridEntity item, GridSlot slot) 
    {
        if (slotOccupancy.ContainsKey(slot))
        {
            if (slotOccupancy[slot] != null && slotOccupancy[slot] != item) {
                Debug.LogWarning($"Slot {gridSlots.IndexOf(slot)} was already occupied by {slotOccupancy[slot].name}. Overwriting with {item.name}.");
            }
            slotOccupancy[slot] = item;

            
            item.currentSlot = slot; 
        } else {
            Debug.LogError($"Attempted to place item in a slot not managed by GridManager: {slot.name}");
        }
    }
    public void ClearSlot(GridSlot slot)
    {
        if (slotOccupancy.ContainsKey(slot))
        {
            // Optional: Add logic if the item being cleared needs notification
            slotOccupancy[slot] = null;
            if (slot != null)
            {
                slot.SetLockIconVisibility(false); // Hide lock icon when slot is cleared
            }
        } else {
             Debug.LogError($"Attempted to clear a slot not managed by GridManager: {slot.name}");
        }
    }
    public GridSlot FindClosestEmptySlot(Vector3 referencePosition)
    {
        GridSlot closestSlot = null;
        float minDistance = float.MaxValue;

        foreach (GridSlot slot in gridSlots)
        {
            if (!IsSlotOccupied(slot))
            {
                float distance = Vector3.Distance(slot.transform.position, referencePosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSlot = slot;
                }
            }
        }

        // if (closestSlot == null) Debug.LogWarning("FindClosestEmptySlot: No empty slots found!");

        return closestSlot;
    }
    public void HandleEntitySelected(InteractiveGridEntity entity)
    {
        if (entity == null || entity.currentSlot == null)
        {
            Debug.LogWarning("HandleEntitySelected called with null entity or entity not in a slot.");
            ClearSelection(); // Clear any previous selection
            return;
        }

        // Deselect any previously selected slot
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.SetHighlight(false);
        }

        // Update selection
        currentlySelectedEntity = entity;
        currentlySelectedSlot = entity.currentSlot;

        // Highlight the new slot
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.SetHighlight(true);
        }

        // Notify PersistentUIManager to display information
        if (persistentUIManager != null)
        {
            persistentUIManager.DisplayEntityInformation(currentlySelectedEntity); // This method will be in PersistentUIManager
        }
    }
    public void ClearSelection()
    {
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.SetHighlight(false);
            // Debug.Log($"Slot {currentlySelectedSlot.name} unhighlighted.");
        }
        currentlySelectedEntity = null;
        currentlySelectedSlot = null;

        // Notify PersistentUIManager to clear information
        if (persistentUIManager != null)
        {
            persistentUIManager.ClearEntityInformation(); // This method will be in PersistentUIManager
        }
    }
    public void ClearSelectionBeforeDrag()
    {
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.SetHighlight(false);
        }
        // Don't null out currentlySelectedEntity/Slot here if the drag might be cancelled
        // and you want it to re-select. Or do, if dragging always means deselection.
        // For now, just ensure highlight is off. Information display will clear on drag end or new selection.

        if (persistentUIManager != null)
        {
            persistentUIManager.ClearEntityInformation();
        }
    }
    public Dictionary<GridSlot, InteractiveGridEntity> GetSlotOccupancyData()
    {
        return slotOccupancy;
    }
    public List<GridSlot> GetGridSlotsList()
    {
        return gridSlots; // Ensure gridSlots is populated correctly in InitializeGrid
    }
    public GridSlot GetGridSlotByIndex(int index)
    {
        if (gridSlots != null && index >= 0 && index < gridSlots.Count)
        {
            return gridSlots[index];
        }
        Debug.LogWarning($"GetGridSlotByIndex: Invalid index {index} or gridSlots not initialized.");
        return null;
    }
    public bool HasAnyOccupiedSlots()
    {
        // Check if any value in the dictionary is not null
        return slotOccupancy.Values.Any(entity => entity != null);
    }

}