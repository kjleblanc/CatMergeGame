using UnityEngine;

[System.Serializable] // Makes this visible and editable in the Inspector when used in a List
public class SpawnableItemChance
{
    public MergeItemData itemData; // The item that can be spawned
    [Range(0f, 100f)] // Makes it a slider in the Inspector from 0 to 100
    public float spawnChancePercentage; // The chance (out of 100) for this item to spawn
}
