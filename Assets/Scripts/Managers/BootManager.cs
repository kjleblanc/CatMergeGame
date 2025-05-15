using UnityEngine;
using UnityEngine.UI; // For Button
using UnityEngine.SceneManagement; // For SceneManager
using TMPro; // For TextMeshProUGUI
using System.IO; // For Path.Combine, File.Exists

public class BootManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject splashImageObject; // Assign your SplashImage GameObject
    public TextMeshProUGUI statusText;   // Assign your StatusText TMP object
    public Button newGameButton;
    public Button continueButton;
    public Slider loadingBar; // Optional: if you add a loading bar

    [Header("Settings")]
    public string mainGameSceneName = "MainGameScene";
    public float splashScreenDuration = 2.0f; // How long to show splash before checking save

    private string saveFilePath; // To check for save file existence

    void Start()
    {
        // Initially hide buttons and loading bar
        if (newGameButton != null) newGameButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (loadingBar != null) loadingBar.gameObject.SetActive(false);
        if (statusText != null) statusText.text = ""; // Or your initial splash message

        // Define where the save file would be (consistent with SaveLoadManager)
        string saveFileName = "catMergeSaveData.json"; // Ensure this matches SaveLoadManager
        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (splashImageObject != null) splashImageObject.SetActive(true);

        // Start a coroutine to handle the splash and loading sequence
        StartCoroutine(SplashAndLoadSequence());
    }


    System.Collections.IEnumerator SplashAndLoadSequence()
    {
        Debug.Log("BootManager: SplashAndLoadSequence START");

        // 1. Show Splash
        if (splashImageObject != null)
        {
            Debug.Log("BootManager: Showing Splash Image.");
            if (statusText != null) statusText.text = "Paws & Pieces"; // Your game title
            else Debug.LogWarning("BootManager: statusText is NULL before splash title.");

            yield return new WaitForSeconds(splashScreenDuration);

            if (splashImageObject != null) splashImageObject.SetActive(false);
            Debug.Log("BootManager: Splash Image Hidden.");
        }
        else Debug.LogWarning("BootManager: splashImageObject is NULL.");

        // 2. Check for Save File and Show Buttons
        if (statusText != null) statusText.text = "Checking for saved progress...";
        else Debug.LogWarning("BootManager: statusText is NULL before checking save.");
        Debug.Log("BootManager: Yielding a frame after 'Checking for saved progress...'");
        yield return null; // Wait a frame for text to update

        Debug.Log($"BootManager: Checking for save file at: {saveFilePath}");
        bool saveFileExists = File.Exists(saveFilePath);
        Debug.Log($"BootManager: Save file exists? {saveFileExists}");

        if (saveFileExists)
        {
            Debug.Log("BootManager: Save file EXISTS. Preparing Continue/New Game buttons.");
            if (statusText != null) statusText.text = "Welcome back!";
            else Debug.LogWarning("BootManager: statusText is NULL for 'Welcome back!'.");

            if (continueButton != null)
            {
                Debug.Log("BootManager: Activating Continue Button.");
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.AddListener(LoadMainGame); // Ensure listener is added only once if coroutine could restart
            }
            else Debug.LogError("BootManager: continueButton IS NULL!");

            if (newGameButton != null) // Optional New Game button
            {
                Debug.Log("BootManager: Activating New Game Button (alongside Continue).");
                newGameButton.gameObject.SetActive(true);
                newGameButton.onClick.AddListener(StartNewGameAndLoadMain);
            }
            else Debug.LogWarning("BootManager: newGameButton IS NULL (optional path).");
        }
        else // No save file
        {
            Debug.Log("BootManager: Save file DOES NOT EXIST. Preparing New Game button.");
            if (statusText != null) statusText.text = "No saved game found.";
            else Debug.LogWarning("BootManager: statusText is NULL for 'No saved game found.'.");

            if (newGameButton != null)
            {
                Debug.Log("BootManager: Activating New Game Button.");
                newGameButton.gameObject.SetActive(true);
                newGameButton.onClick.AddListener(StartNewGameAndLoadMain);
            }
            else Debug.LogError("BootManager: newGameButton IS NULL!");
        }
        Debug.Log("BootManager: SplashAndLoadSequence END");
    }

    void StartNewGameAndLoadMain()
    {
        // If a save file exists and they choose "New Game", delete the old save.
        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                Debug.Log("Previous save file deleted for New Game.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deleting old save file: {e.Message}");
            }
        }
        PlayerPrefs.SetInt("StartNewGameFlag", 1); // Signal to SaveLoadManager to setup new game
        LoadMainGame();
    }

    void LoadMainGame()
    {
        if (statusText != null) statusText.text = "Loading your game...";
        if (newGameButton != null) newGameButton.gameObject.SetActive(false);
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (loadingBar != null) loadingBar.gameObject.SetActive(true);

        // Optionally, use SceneManager.LoadSceneAsync for a loading bar
        SceneManager.LoadScene(mainGameSceneName);
    }

    // If using LoadSceneAsync:
    // System.Collections.IEnumerator LoadMainGameAsync()
    // {
    //     AsyncOperation operation = SceneManager.LoadSceneAsync(mainGameSceneName);
    //     if (loadingBar != null) loadingBar.gameObject.SetActive(true);

    //     while (!operation.isDone)
    //     {
    //         float progress = Mathf.Clamp01(operation.progress / 0.9f); // operation.progress goes to 0.9 then jumps to 1
    //         if (loadingBar != null) loadingBar.value = progress;
    //         if (statusText != null) statusText.text = "Loading... " + (progress * 100f).ToString("F0") + "%";
    //         yield return null;
    //     }
    // }
}