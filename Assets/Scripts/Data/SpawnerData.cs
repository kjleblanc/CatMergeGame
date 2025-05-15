using System.Collections.Generic;
using UnityEngine;

// Define the menu item for creating instances of this ScriptableObject
[CreateAssetMenu(fileName = "New Spawner Data", menuName = "Merge Game/Spawner Data")]
public class SpawnerData : CommonItemData
{
    [Header("Spawning Behavior")]
    // Reference to the ScriptableObject of the item this spawner produces
    public List<SpawnableItemChance> possibleSpawns; // List of items this spawner can produce
    // How many items can be spawned before a cooldown starts?
    public int maxSpawnsBeforeCooldown = 5;
    // How long does the cooldown last in seconds?
    public float cooldownDuration = 3.0f;

}