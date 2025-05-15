using UnityEngine;
using System.IO; // For Path.Combine, File operations
using System.Collections.Generic; // For List
using System.Linq; // For LINQ operations like .Any()

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    private string saveFileName = "catMergeSaveData.json"; // Name of our save file
    private string saveFilePath;

    // References to other managers - these will be found or assigned
    private GridManager gridManager;
    private PersistentUIManager persistentUIManager;
    private UIManager uiManager; // For non-persistent UI states like the inventory panel

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
            Debug.Log($"Save file path will be: {saveFilePath}");

            // Find managers immediately. It's crucial they exist before Start/LoadGame.
            gridManager = FindFirstObjectByType<GridManager>();
            persistentUIManager = FindFirstObjectByType<PersistentUIManager>();
            uiManager = FindFirstObjectByType<UIManager>();

            if (gridManager == null) Debug.LogError("SaveLoadManager: GridManager not found in Awake!");
            // persistentUIManager can be null if not yet implemented fully for saving, handle gracefully.
            if (uiManager == null) Debug.LogError("SaveLoadManager: UIManager not found in Awake!");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        // Find managers
        gridManager = FindFirstObjectByType<GridManager>();
        persistentUIManager = FindFirstObjectByType<PersistentUIManager>();
        uiManager = FindFirstObjectByType<UIManager>();

        if (PlayerPrefs.HasKey("StartNewGameFlag") && PlayerPrefs.GetInt("StartNewGameFlag") == 1)
        {
            PlayerPrefs.DeleteKey("StartNewGameFlag"); // Clear the flag
            Debug.Log("SaveLoadManager: StartNewGameFlag detected. Setting up new game.");
            if (File.Exists(saveFilePath)) // Should have been deleted by BootManager, but double check
            {
                File.Delete(saveFilePath);
            }
            SetupNewGame();
        }
        else
        {
            LoadGame(); // Normal load attempt
        }
    }
    public void SaveGame()
    {
        if (gridManager == null)
        {
            Debug.LogError("SaveLoadManager: GridManager reference is null. Cannot save grid state.");
            // Optionally, still try to save other data if persistentUIManager or uiManager are valid
        }
        // We also need uiManager to save its state.
        if (uiManager == null)
        {
            Debug.LogWarning("SaveLoadManager: UIManager reference is null. Cannot save inventory panel state.");
        }

        GameState dataToSave = new GameState();

        // 1. Collect Grid Data
        if (gridManager != null)
        {
            foreach (var slotEntry in gridManager.GetSlotOccupancyData()) // Using the new method
            {
                GridSlot slot = slotEntry.Key;
                InteractiveGridEntity entity = slotEntry.Value;

                if (entity != null && slot != null) // Ensure both are valid
                {
                    SavedGridEntity savedEntity = new SavedGridEntity();
                    int slotIndex = gridManager.GetGridSlotsList().IndexOf(slot);

                    if (slotIndex == -1) // Should not happen if slot is from GetSlotOccupancyData
                    {
                        Debug.LogError($"Save Error: Could not find index for slot {slot.name}. Skipping entity {entity.name}.");
                        continue;
                    }
                    savedEntity.slotIndex = slotIndex;
                    savedEntity.isLocked = entity.isLocked;

                    if (entity is MergeItem mergeItem && mergeItem.itemData != null)
                    {
                        savedEntity.scriptableObjectName = mergeItem.itemData.name; // .name of the SO asset
                        savedEntity.isSpawnerType = false;
                        // spawner fields will remain default (0, 0f)
                    }
                    else if (entity is ItemSpawner spawner && spawner.spawnerData != null)
                    {
                        savedEntity.scriptableObjectName = spawner.spawnerData.name; // .name of the SO asset
                        savedEntity.isSpawnerType = true;
                        savedEntity.spawnerItemsSpawned = spawner.GetItemsSpawnedCount();
                        savedEntity.spawnerCooldownRemaining = spawner.GetCurrentCooldownTime();
                    }
                    else
                    {
                        Debug.LogWarning($"Save Warning: Entity {entity.name} in slot {slot.name} is of unknown type or has no data. Skipping.");
                        continue; // Skip if unknown type or missing data
                    }
                    dataToSave.gridEntities.Add(savedEntity);
                }
            }
        }

        // 2. Collect UIManager Data (Inventory Panel State)
        if (uiManager != null)
        {
            dataToSave.wasGridPanelOpen = uiManager.IsInventoryPanelOpen();
        }

        // 3. Collect PersistentUIManager Data (Future: currency, cat level/XP)
        if (persistentUIManager != null)
        {
            // Example for when you add currency:
            // dataToSave.currentCurrency = persistentUIManager.GetCurrentCurrency(); // (Requires this method in PersistentUIManager)
        }

        // Serialize to JSON and write to file
        string json = JsonUtility.ToJson(dataToSave, true); // 'true' for pretty print (good for debugging)
        try
        {
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"Game SAVED to: {saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game to {saveFilePath}: {e.Message}\n{e.StackTrace}");
        }
    }
    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("SaveLoadManager: No save file found. Setting up new game.");
            SetupNewGame(); // Call your new game setup
            return;
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            GameState loadedData = JsonUtility.FromJson<GameState>(json);

            if (loadedData == null) // JsonUtility can return null if JSON is malformed
            {
                Debug.LogError("SaveLoadManager: Failed to parse save data from JSON. Save file might be corrupted.");
                File.Delete(saveFilePath); // Optionally delete corrupted save
                SetupNewGame();
                return;
            }

            // --- Begin Restoration ---

            // 0. Ensure GridManager is ready
            if (gridManager == null)
            {
                Debug.LogError("SaveLoadManager: GridManager reference is null. Cannot load grid state.");
                return; // Critical error, cannot proceed with grid load
            }

            // 1. Clear current grid items and any selection before loading
            gridManager.ClearAllGridItemsAndSelection();

            // 2. Restore Grid Entities
            if (loadedData.gridEntities != null)
            {
                foreach (SavedGridEntity savedEntity in loadedData.gridEntities)
                {
                    GridSlot targetSlot = gridManager.GetGridSlotByIndex(savedEntity.slotIndex);
                    if (targetSlot == null)
                    {
                        Debug.LogError($"Load Error: Could not find GridSlot at index {savedEntity.slotIndex} for SO '{savedEntity.scriptableObjectName}'. Skipping entity.");
                        continue;
                    }

                    // Construct path for Resources.Load based on whether it's a spawner or merge item
                    string resourceSubFolder = savedEntity.isSpawnerType ? "Spawners" : "MergeItems";
                    string resourcePath = $"ScriptableObjects/{resourceSubFolder}/{savedEntity.scriptableObjectName}";
                    CommonItemData itemSO = Resources.Load<CommonItemData>(resourcePath);

                    if (itemSO == null)
                    {
                        Debug.LogError($"Load Error: Could not load ScriptableObject from Resources path: '{resourcePath}' (saved name: '{savedEntity.scriptableObjectName}'). Skipping entity.");
                        continue;
                    }

                    if (itemSO.prefab == null)
                    {
                        Debug.LogError($"Load Error: Prefab is null on loaded ScriptableObject '{itemSO.name}'. Skipping entity.");
                        continue;
                    }

                    GameObject entityGO = Instantiate(itemSO.prefab, targetSlot.transform);
                    InteractiveGridEntity newEntityInstance = entityGO.GetComponent<InteractiveGridEntity>();

                    if (newEntityInstance != null)
                    {
                        if (newEntityInstance is MergeItem mergeItem && itemSO is MergeItemData mid)
                        {
                            mergeItem.InitializeItem(mid, targetSlot, gridManager);
                            // isLocked is already set above. InitializeItem sets it to false by default,
                            // so ensure it's restored *after* InitializeItem if InitializeItem overwrites it,
                            // or ensure InitializeItem respects an initial isLocked state.
                            // For now, setting before Initialize is fine as InitializeBaseParameters sets it to false,
                            // but then UpdateLockVisual inside it would use the correct 'isLocked'.
                            // A safer bet is to set it *after* init, then call UpdateLockVisual.
                            // mergeItem.isLocked = savedEntity.isLocked; // If InitializeItem resets it
                            // mergeItem.UpdateLockVisual(); // If you create a public version or call from init
                        }
                        else if (newEntityInstance is ItemSpawner spawner && itemSO is SpawnerData sid)
                        {
                            spawner.InitializeSpawner(sid, targetSlot, gridManager);
                            // spawner.isLocked = savedEntity.isLocked; // If InitializeSpawner resets it
                            // spawner.UpdateLockVisual();
                            spawner.SetCooldownState(savedEntity.spawnerItemsSpawned, savedEntity.spawnerCooldownRemaining);
                        }
                        else
                        {
                            Debug.LogError($"Load Error: Mismatch between entity type and ScriptableObject data type for '{itemSO.name}'. Destroying instantiated GO.");
                            Destroy(entityGO);
                            continue;
                        }
                        gridManager.PlaceItem(newEntityInstance, targetSlot); // Ensure GridManager's dictionary is updated
                        newEntityInstance.isLocked = savedEntity.isLocked;
                        newEntityInstance.UpdateLockVisual(); // Explicitly call to ensure visuals match loaded lock state
                    }
                    else
                    {
                        Debug.LogError($"Load Error: Instantiated prefab from SO '{itemSO.name}' is missing InteractiveGridEntity script. Destroying GO.");
                        Destroy(entityGO);
                    }
                }
            }

            // 3. Restore UIManager Data (Inventory Panel State)
            if (uiManager != null)
            {
                if (loadedData.wasGridPanelOpen)
                {
                    // Check if inventoryPanelObject is active before trying to show (it might be if UIManager.Start already ran)
                    // Or just call it, UIManager.ShowInventoryPanel has a check.
                    uiManager.ShowInventoryPanel();
                }
                else
                {
                    uiManager.HideInventoryPanel();
                }
            }

            // 4. Restore PersistentUIManager Data (Future: currency, cat level/XP)
            if (persistentUIManager != null)
            {
                // Example:
                // persistentUIManager.SetCurrentCurrency(loadedData.currentCurrency);
            }

            Debug.Log($"Game LOADED successfully from: {saveFilePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load or parse game from {saveFilePath}: {e.Message}\n{e.StackTrace}. Save file might be corrupted. Starting new game.");
            // Optionally delete the corrupted save file to prevent repeated load errors
            if(File.Exists(saveFilePath)) File.Delete(saveFilePath);
            SetupNewGame();
        }
    }
    void SetupNewGame()
    {
        Debug.Log("No save file found or load failed. Setting up new game state.");
        if (uiManager != null)
        {
            uiManager.HideInventoryPanel(); // Or your default initial state
        }
        if (gridManager != null && !gridManager.HasAnyOccupiedSlots())
        {
            gridManager.PlaceStartingSpawner(); // GridManager will place its default spawner
        }
    }
    public bool HasSaveFile()
    {
        return File.Exists(saveFilePath);
    }
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) // True when game is paused/backgrounded (mobile) or loses focus (editor/desktop)
        {
            Debug.Log("Application is pausing. Attempting to save game...");
            SaveGame();
        }
    }
    void OnApplicationQuit()
    {
        Debug.Log("Application is quitting. Attempting to save game...");
        SaveGame();
    }
}