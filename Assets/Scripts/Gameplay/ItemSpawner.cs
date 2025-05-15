using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;


[RequireComponent(typeof(Image))] 
[RequireComponent(typeof(CanvasGroup))] 

public class ItemSpawner : InteractiveGridEntity, IPointerClickHandler
{
    [Header("Data")]
    public SpawnerData spawnerData;
    // --- Cooldown State (Specific to Spawner) ---
    private int itemsSpawnedCount = 0;
    private bool isCoolingDown = false;
    private float currentCooldownTime = 0f;
    protected override void Awake()
    {
        base.Awake(); // Calls InteractiveGridEntity.Awake()

        // Assign the Image component on this GameObject to the base class's entityImage
        this.entityImage = GetComponent<Image>();
        if (this.entityImage == null)
        {
            Debug.LogError($"{gameObject.name} (ItemSpawner) is missing an Image component for its entityImage!");
        }
        // mainCanvas is found by base.Awake()
    }
    protected override void Update()
    {
        base.Update(); // Handles long press lock, wobble

        if (isCoolingDown)
        {
            currentCooldownTime -= Time.deltaTime;
            if (currentCooldownTime <= 0)
            {
                isCoolingDown = false;
                currentCooldownTime = 0;
                itemsSpawnedCount = 0;
            }
        }
    }
    public void InitializeSpawner(SpawnerData data, GridSlot slot, GridManager manager)
    {
        this.spawnerData = data;
        if (data == null)
        {
            Debug.LogError($"InitializeSpawner called on {gameObject.name} with null SpawnerData!");
            // Ensure entityImage is fetched before trying to use it for error color
            if (this.entityImage == null) this.entityImage = GetComponent<Image>(); // <<< ADD THIS CHECK
            if(this.entityImage != null) this.entityImage.color = Color.red;
            base.InitializeBaseParameters(slot, manager, 1f, "Error Spawner");
            return;
        }

        // Call base initialization FIRST
        base.InitializeBaseParameters(slot, manager, data.visualScaleMultiplier, data.displayName);


        if (this.entityImage == null)
        {
            this.entityImage = GetComponent<Image>();
            if (this.entityImage == null) // If still null, the prefab is fundamentally broken
            {
                Debug.LogError($"{data.displayName} (ItemSpawner): CRITICAL - Image component missing from prefab and cannot be fetched! Visuals will fail.");
                return; // Can't proceed with visual setup
            }
        }

        // This is the crucial part for the visual:
        Debug.Log($"ItemSpawner {data.displayName}: Initializing visual. DisplayIcon on SO is {(data.displayIcon == null ? "NULL" : "ASSIGNED")}. EntityImage component is {(this.entityImage == null ? "NULL" : "PRESENT")}");

        if (data.displayIcon != null)
        {
            this.entityImage.sprite = data.displayIcon;
            this.entityImage.color = Color.white;
        }
        else
        {
            this.entityImage.sprite = null;
            this.entityImage.color = Color.yellow;
            Debug.LogWarning($"{data.displayName} (SpawnerData) is missing its displayIcon. Displaying as yellow square.");
        }

        isCoolingDown = false;
        currentCooldownTime = 0f;
        itemsSpawnedCount = 0;
        // isLocked and lock icon are handled by base.InitializeBaseParameters and UpdateLockVisual
    }
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        // Use flags from InteractiveGridEntity base class
        if (base.justFinishedDrag || base.lockStateJustChangedByHold) 
        {
            // Flags are reset on the next OnPointerDown in the base class.
            return;
        }

        // Spawners can spawn items even if locked (lock only prevents movement)
        TrySpawnItem();
    }
    public void TrySpawnItem()
    {
        if (isCoolingDown) {
            return;
        }

        if (spawnerData == null || spawnerData.possibleSpawns == null || spawnerData.possibleSpawns.Count == 0)
        {
            Debug.LogError("SpawnerData or possibleSpawns list is not assigned or is empty!");
            return;
        }

        if (gridManager == null) {
            Debug.LogError("GridManager reference not set on Spawner!");
            return;
        }

        MergeItemData selectedItemDataToSpawn = null;
        float totalChance = spawnerData.possibleSpawns.Sum(s => s.spawnChancePercentage);

        if (totalChance <= 0) {
            Debug.LogError("Total spawn chance is 0. Cannot determine item to spawn. Check SpawnerData percentages.");
            // Try to spawn first item if chances are misconfigured but list exists
            if (spawnerData.possibleSpawns.Count > 0 && spawnerData.possibleSpawns[0].itemData != null) {
                Debug.LogWarning("Defaulting to first item in possibleSpawns due to zero total chance.");
                selectedItemDataToSpawn = spawnerData.possibleSpawns[0].itemData;
            } else {
                return;
            }
        } else {
            float randomValue = Random.Range(0f, totalChance); 
            float cumulativeChance = 0f;

            foreach (SpawnableItemChance spawnInfo in spawnerData.possibleSpawns)
            {
                cumulativeChance += spawnInfo.spawnChancePercentage;
                if (randomValue <= cumulativeChance)
                {
                    selectedItemDataToSpawn = spawnInfo.itemData;
                    break; 
                }
            }
        }
        
        if (selectedItemDataToSpawn == null) // Fallback if logic failed (shouldn't if totalChance > 0)
        {
             if (spawnerData.possibleSpawns.Count > 0 && spawnerData.possibleSpawns[0].itemData != null) {
                Debug.LogWarning("Could not select item based on chance, defaulting to first item in list.");
                selectedItemDataToSpawn = spawnerData.possibleSpawns[0].itemData;
             } else {
                Debug.LogError("Failed to select any item to spawn from possibleSpawns list, and no fallback available.");
                return;
             }
        }
        
        GridSlot targetSlot = gridManager.FindClosestEmptySlot(transform.position);

        if (targetSlot != null) {
            GameObject prefabToSpawn = selectedItemDataToSpawn.prefab;
            if (prefabToSpawn == null) {
                Debug.LogError($"ItemPrefab is not assigned on the '{selectedItemDataToSpawn.displayName}' MergeItemData!");
                return;
            }

            GameObject newItemGO = Instantiate(prefabToSpawn, targetSlot.transform);
            MergeItem newItem = newItemGO.GetComponent<MergeItem>();
            if (newItem != null) {
                newItem.InitializeItem(selectedItemDataToSpawn, targetSlot, gridManager); // Use InitializeItem
                gridManager.PlaceItem(newItem, targetSlot);

                itemsSpawnedCount++;
                
                if (itemsSpawnedCount >= spawnerData.maxSpawnsBeforeCooldown) 
                {
                    isCoolingDown = true;
                    currentCooldownTime = spawnerData.cooldownDuration;

                }
            } else {
                Debug.LogError($"Spawned prefab '{prefabToSpawn.name}' does not have a MergeItem component!");
                Destroy(newItemGO);
            }
        } else {
            // Debug.LogWarning("No empty grid slot found to spawn item!");
        }
    }
    public int GetItemsSpawnedCount()
    {
        return itemsSpawnedCount;
    }
    public float GetCurrentCooldownTime()
    {
        return currentCooldownTime;
    }
    public void SetCooldownState(int count, float timeRemaining)
    {
        itemsSpawnedCount = count;
        currentCooldownTime = timeRemaining;
        isCoolingDown = (timeRemaining > 0f && count >= (spawnerData != null ? spawnerData.maxSpawnsBeforeCooldown : int.MaxValue));
        // If spawnerData is null during load (should not happen if SO loading is correct),
        // avoid dividing by zero or incorrect cooldown logic.
        // A more robust check might be needed if count alone determines cooldown.
        // For now, if timeRemaining > 0, assume it was cooling down.
    }
}