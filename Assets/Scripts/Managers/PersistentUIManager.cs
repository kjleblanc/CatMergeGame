using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PersistentUIManager : MonoBehaviour
{
    public static PersistentUIManager Instance { get; private set; }

    [Header("Persistent UI Panels")]
    public GameObject topPanel;    
    public GameObject bottomPanel; 
    [Header("Selected Item Display (in Bottom Panel)")]
    public TextMeshProUGUI selectedItemNameText;  
    public TextMeshProUGUI selectedItemDescriptionText; 
    public Image selectedItemIconImage;  
    void Awake()
    {
        // Singleton pattern: Ensure only one instance of this manager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make this GameObject persist across scenes
            InitializePanels();

            // It's good practice to also make the panels themselves persist
            // if they are direct children of this GameObject.
            // If they are part of a larger Canvas that's also a child,
            // DontDestroyOnLoad(gameObject) will cover them.
            // If they are separate root objects in the scene, you might need to
            // call DontDestroyOnLoad on them too, or parent them to this object.
            // For simplicity, we'll assume they will be children of the GameObject
            // this script is on, or part of a Canvas that is a child.
        }
        else if (Instance != this)
        {
            // If an instance already exists and it's not this one, destroy this new one.
            Destroy(gameObject);
            return;
        }
    }
    void InitializePanels()
    {
        if (topPanel != null) topPanel.SetActive(true);
        if (bottomPanel != null) bottomPanel.SetActive(true);

        ClearEntityInformation();

        Debug.Log("PersistentUIManager Initialized and Persisting.");
    }
    public void DisplayEntityInformation(InteractiveGridEntity entity)
    {
        if (entity == null)
        {
            ClearEntityInformation();
            return;
        }

        string nameToDisplay = "Unknown";
        string descriptionToDisplay = "No description available.";
        Sprite iconToDisplay = null;

        if (entity is MergeItem mergeItem && mergeItem.itemData != null)
        {
            // itemData is of type MergeItemData, which inherits from CommonItemData
            nameToDisplay = mergeItem.itemData.displayName;
            descriptionToDisplay = mergeItem.itemData.description;
            iconToDisplay = mergeItem.itemData.displayIcon;
        }
        else if (entity is ItemSpawner spawner && spawner.spawnerData != null)
        {
            // spawnerData is of type SpawnerData, which inherits from CommonItemData
            nameToDisplay = spawner.spawnerData.displayName;
            descriptionToDisplay = spawner.spawnerData.description;
            iconToDisplay = spawner.spawnerData.displayIcon;
        }

        if (selectedItemNameText != null)
        {
            selectedItemNameText.text = nameToDisplay;
        }
        if (selectedItemDescriptionText != null)
        {
            selectedItemDescriptionText.text = descriptionToDisplay;
            // Optional: Adjust layout if description is long, or use a ScrollView for description
        }
        if (selectedItemIconImage != null)
        {
            if (iconToDisplay != null)
            {
                selectedItemIconImage.sprite = iconToDisplay;
                selectedItemIconImage.enabled = true;
            }
            else
            {
                selectedItemIconImage.enabled = false; // Hide if no icon
            }
        }
    }
    public void ClearEntityInformation()
    {
        if (selectedItemNameText != null)
        {
            selectedItemNameText.text = ""; // Or "Nothing Selected"
        }
        if (selectedItemDescriptionText != null)
        {
            selectedItemDescriptionText.text = "";
        }
        if (selectedItemIconImage != null)
        {
            selectedItemIconImage.sprite = null; // Clear sprite
            selectedItemIconImage.enabled = false; // Hide it
        }
    }
    // --- Example methods you might add later ---
    public void UpdateLevelText(int level)
    {
        // if (levelText != null) levelText.text = "Level: " + level;
    }
    public void UpdateCurrencyText(int currency)
    {
        // if (currencyText != null) currencyText.text = "Coins: " + currency;
    }
    public void GoToShopScene()
    {
        // SceneManager.LoadScene("ShopScene"); // Example
    }
    // Add other navigation methods here
}