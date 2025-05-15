using System.Collections.Generic; // Required for List

// No 'using UnityEngine;' needed here if we're only storing basic C# types and our own serializable classes.

[System.Serializable] // Makes this class show up in the Inspector and be serializable by JsonUtility
public class SavedGridEntity
{
    public int slotIndex;
    public string scriptableObjectName; // The .name of the CommonItemData SO
    public bool isLocked;

    // Spawner-specific fields
    public bool isSpawnerType; // True if this entity was an ItemSpawner
    public int spawnerItemsSpawned;
    public float spawnerCooldownRemaining;

    // Note: For MergeItems, 'level' is part of their MergeItemData,
    // which we identify by scriptableObjectName. So, no need to save level separately here
    // unless your design changes significantly.
}

[System.Serializable]
public class GameState // This is the root object for our save file
{
    public List<SavedGridEntity> gridEntities = new List<SavedGridEntity>();

    // We'll add more here later (currency, cat progress, etc.)
    public bool wasGridPanelOpen = false; // Example: from UIManager
    // public int currentCurrency = 0;    // Example for later
}