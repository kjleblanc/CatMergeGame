using UnityEngine;

[CreateAssetMenu(fileName = "New Merge Item Data", menuName = "Merge Game/Merge Item Data")]
public class MergeItemData : CommonItemData
{
    [Header("Item Info")]
    public int level;               // The merge level (e.g., Kibble=1, Pile=2)
    
    [Header("Animation (Optional)")]
    public RuntimeAnimatorController itemAnimatorController; // Assign Controller asset here

    [Header("Merging")]
    public MergeItemData nextLevelItem; // Reference to the SO of the item it merges into (null if highest)

}