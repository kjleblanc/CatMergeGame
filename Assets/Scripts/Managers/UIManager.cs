using UnityEngine;
using UnityEngine.UI; // Required for Button and Text components
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Inventory Panel References")]
    [Tooltip("Assign the parent GameObject of your merge grid (e.g., GridPanel)")]
    public GameObject inventoryPanelObject; // This is your current 'gridPanelObject' from GridManager

    [Tooltip("Assign the UI Button that will open/close the inventory panel")]
    public Button toggleInventoryButton;

    [Tooltip("Optional: Text on the button to show Open/Close status")]
    public TMP_Text toggleButtonText; // Or use TextMeshProUGUI if you prefer

    private bool isInventoryPanelOpen = false; // Default state, adjust as needed

    void Start()
    {
        // Ensure references are set
        if (inventoryPanelObject == null)
        {
            Debug.LogError("UIManager: Inventory Panel Object is not assigned in the Inspector!");
        }
        if (toggleInventoryButton == null)
        {
            Debug.LogError("UIManager: Toggle Inventory Button is not assigned in the Inspector!");
        }
        else
        {
            // Add a listener to the button's click event
            toggleInventoryButton.onClick.AddListener(ToggleInventoryPanel);
        }

        // Set the initial state of the panel and button text
        SetInventoryPanelActive(isInventoryPanelOpen);
    }
    public void ToggleInventoryPanel()
    {
        isInventoryPanelOpen = !isInventoryPanelOpen;
        SetInventoryPanelActive(isInventoryPanelOpen);
    }
    private void SetInventoryPanelActive(bool isActive)
    {
        if (inventoryPanelObject != null)
        {
            inventoryPanelObject.SetActive(isActive);
        }

        if (toggleButtonText != null)
        {
            toggleButtonText.text = isActive ? "Close Board" : "Open Board";
        }
        // You could also change the button sprite or other visual cues here
    }
    // Public method to check if the panel is open (other scripts might need this)
    public bool IsInventoryPanelOpen()
    {
        return isInventoryPanelOpen;
    }
    // Optional: Methods to force open or close
    public void ShowInventoryPanel()
    {
        if (!isInventoryPanelOpen) // Only act if it's not already in the desired state
        {
            ToggleInventoryPanel();
        }
    }
    public void HideInventoryPanel()
    {
        if (isInventoryPanelOpen) // Only act if it's not already in the desired state
        {
            ToggleInventoryPanel();
        }
    }
}