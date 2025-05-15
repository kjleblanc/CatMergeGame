using UnityEngine;

// This base class will not have a [CreateAssetMenu] attribute.
public class CommonItemData : ScriptableObject
{
    [Header("Common Display Info")]
    [Tooltip("The name displayed for this item or entity.")]
    public string displayName;

    [Tooltip("The icon displayed for this item or entity.")]
    public Sprite displayIcon;

    [Tooltip("A brief description of this item or entity.")]
    [TextArea(3, 5)] // Min 3 lines, max 5 shown by default in Inspector
    public string description;

    [Header("Common Visuals")]
    [Tooltip("The prefab used to visually represent this item/entity on the grid.")]
    public GameObject prefab; // This will replace itemPrefab and spawnerPrefab

    [Tooltip("Visual scale multiplier for the item/entity on the grid.")]
    public float visualScaleMultiplier = 1.0f;
}