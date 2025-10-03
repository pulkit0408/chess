using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject LoadGamePanel;

    [Header("Main Menu Buttons")]
    public Button StartButton;
    public Button ExitButton;

    [Header("Load Game Panel Buttons")]
    public Button ContinueSavedGameButton;
    public Button StartNewGameButton;
    public Button BackFromLoadButton;

    [Header("Optional UI Text")]
    public TextMeshProUGUI SaveFileInfoText;

    void Start()
    {
        SetupButtonListeners();

        if (LoadGamePanel != null)
            LoadGamePanel.SetActive(false);

        UpdateSaveFileInfo();
    }

    void SetupButtonListeners()
    {
        if (StartButton != null)
            StartButton.onClick.AddListener(OnStartButtonClicked);
        if (ExitButton != null)
            ExitButton.onClick.AddListener(ExitGame);

        // Load game panel buttons
        if (ContinueSavedGameButton != null)
            ContinueSavedGameButton.onClick.AddListener(ContinueSavedGame);
        if (StartNewGameButton != null)
            StartNewGameButton.onClick.AddListener(StartNewGame);
        if (BackFromLoadButton != null)
            BackFromLoadButton.onClick.AddListener(HideLoadGamePanel);
    }

    void OnStartButtonClicked()
    {
        // Check if save file exists and is valid (not a completed game)
        if (SaveManager.Instance.HasSaveFile())
        {
            // Quick check if saved game is completed
            SaveData saveData = SaveManager.Instance.LoadGame();
            if (saveData != null)
            {
                // Save file exists and game is not completed
                ShowLoadGamePanel();
            }
            else
            {
                // Save file was invalid or completed game, start new game
                StartNewGame();
            }
        }
        else
        {
            StartNewGame();
        }
    }

    void ShowLoadGamePanel()
    {
        if (LoadGamePanel != null)
        {
            LoadGamePanel.SetActive(true);
            UpdateSaveFileInfo();
        }
    }

    void HideLoadGamePanel()
    {
        if (LoadGamePanel != null)
        {
            LoadGamePanel.SetActive(false);
        }
    }

    void UpdateSaveFileInfo()
    {
        if (SaveFileInfoText != null)
        {
            if (SaveManager.Instance.HasSaveFile())
            {
                SaveFileInfoText.text = "Saved game found!";
            }
            else
            {
                SaveFileInfoText.text = "No saved game found.";
            }
        }
    }

    void ContinueSavedGame()
    {
        // Set flag to load saved game
        PlayerPrefs.SetInt("LoadSavedGame", 1);
        LoadGameScene();
    }

    void StartNewGame()
    {
        // Delete existing save file if starting new
        if (SaveManager.Instance.HasSaveFile())
        {
            SaveManager.Instance.DeleteSaveFile();
        }

        // Clear load flag
        PlayerPrefs.DeleteKey("LoadSavedGame");
        LoadGameScene();
    }

    void LoadGameScene()
    {
        // Load the chess game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("DefaultBoard");
    }

    void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Optional: Handle ESC key to close load panel
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && LoadGamePanel != null && LoadGamePanel.activeInHierarchy)
        {
            HideLoadGamePanel();
        }
    }
}