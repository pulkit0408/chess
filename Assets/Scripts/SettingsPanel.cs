using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel;
    public Button settingsButton;
    public Button audioToggleButton;
    public Button backButton;

    [Header("Audio Toggle Button Text")]
    public TextMeshProUGUI audioButtonText;

    [Header("Optional Audio Button Images")]
    public Sprite audioOnSprite;
    public Sprite audioOffSprite;
    public Image audioButtonImage;

    private bool isAudioOn = true; // Start with audio ON
    private float savedVolume = 1f;
    private static SettingsPanel instance; // Singleton instance

    void Awake()
    {
        // Singleton pattern - but allow multiple instances in different scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved audio preference or set default to ON
            LoadAudioSettings();
        }
        else if (instance != this)
        {
            // If we're in a new scene, transfer the settings and destroy the old instance
            if (instance.gameObject.scene != this.gameObject.scene)
            {
                // Copy settings from the persistent instance
                this.isAudioOn = instance.isAudioOn;
                this.savedVolume = instance.savedVolume;

                // Destroy the old instance
                Destroy(instance.gameObject);
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // We're in the same scene, just destroy this duplicate
                Destroy(gameObject);
                return;
            }
        }
    }

    void Start()
    {
        // Always setup button listeners when Start is called
        SetupButtonListeners();

        // Initialize UI
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Apply current audio settings and update UI
        ApplyAudioSettings();
        UpdateAudioButtonUI();
    }

    void SetupButtonListeners()
    {
        // Remove existing listeners to prevent duplicates
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(ShowSettingsPanel);
        }

        if (audioToggleButton != null)
        {
            audioToggleButton.onClick.RemoveAllListeners();
            audioToggleButton.onClick.AddListener(ToggleAudio);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(HideSettingsPanel);
        }
    }

    public void ShowSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Debug.Log("Settings panel opened");
        }
        else
        {
            Debug.LogWarning("Settings panel reference is null!");
        }
    }

    public void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("Settings panel closed");
        }
    }

    public void ToggleAudio()
    {
        isAudioOn = !isAudioOn;
        ApplyAudioSettings();
        SaveAudioSettings();
        UpdateAudioButtonUI();

        Debug.Log($"Audio toggled: {(isAudioOn ? "ON" : "OFF")}");
    }

    private void ApplyAudioSettings()
    {
        if (isAudioOn)
        {
            AudioListener.volume = savedVolume;
        }
        else
        {
            AudioListener.volume = 0f;
        }
    }

    private void UpdateAudioButtonUI()
    {
        // Update button text to show current state
        if (audioButtonText != null)
        {
            audioButtonText.text = isAudioOn ? "OFF" : "ON";
        }

        // Update button image if sprites are provided
        if (audioButtonImage != null)
        {
            if (isAudioOn && audioOnSprite != null)
            {
                audioButtonImage.sprite = audioOnSprite;
            }
            else if (!isAudioOn && audioOffSprite != null)
            {
                audioButtonImage.sprite = audioOffSprite;
            }
        }
    }

    private void SaveAudioSettings()
    {
        PlayerPrefs.SetInt("GameAudioOn", isAudioOn ? 1 : 0);
        PlayerPrefs.SetFloat("GameVolume", savedVolume);
        PlayerPrefs.Save();
        Debug.Log($"Audio settings saved: AudioOn={isAudioOn}");
    }

    private void LoadAudioSettings()
    {
        // Default to audio ON (1) - this ensures first time players have audio enabled
        isAudioOn = PlayerPrefs.GetInt("GameAudioOn", 1) == 1;
        savedVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        Debug.Log($"Audio settings loaded: AudioOn={isAudioOn}");
    }

    // Apply audio settings when scene loads
    [System.Obsolete]
    void OnLevelWasLoaded(int level)
    {
        ApplyAudioSettings();
        // Re-setup button listeners in case UI references changed
        Invoke("SetupButtonListeners", 0.1f); // Small delay to ensure UI is ready
    }

    // Alternative method for newer Unity versions
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Re-find UI references if they're null (happens when switching scenes)
        if (settingsPanel == null || settingsButton == null)
        {
            StartCoroutine(FindUIReferencesCoroutine());
        }

        ApplyAudioSettings();
    }

    System.Collections.IEnumerator FindUIReferencesCoroutine()
    {
        yield return new WaitForEndOfFrame(); // Wait for scene to fully load

        // Try to find UI references by name if they're missing
        if (settingsPanel == null)
        {
            GameObject foundPanel = GameObject.Find("SettingsPanel");
            if (foundPanel != null) settingsPanel = foundPanel;
        }

        if (settingsButton == null)
        {
            GameObject foundButton = GameObject.Find("SettingsButton");
            if (foundButton != null) settingsButton = foundButton.GetComponent<Button>();
        }

        if (audioToggleButton == null)
        {
            GameObject foundButton = GameObject.Find("AudioToggleButton");
            if (foundButton != null) audioToggleButton = foundButton.GetComponent<Button>();
        }

        if (backButton == null)
        {
            GameObject foundButton = GameObject.Find("BackButton");
            if (foundButton != null) backButton = foundButton.GetComponent<Button>();
        }

        if (audioButtonText == null)
        {
            GameObject foundText = GameObject.Find("AudioButtonText");
            if (foundText != null) audioButtonText = foundText.GetComponent<TextMeshProUGUI>();
        }

        // Re-setup listeners with new references
        SetupButtonListeners();
        UpdateAudioButtonUI();
    }

    // Public methods that can be called from other scripts
    public bool IsAudioOn()
    {
        return isAudioOn;
    }

    public void SetAudio(bool audioOn)
    {
        isAudioOn = audioOn;
        ApplyAudioSettings();
        SaveAudioSettings();
        UpdateAudioButtonUI();
    }

    // ESC key to close panel
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            HideSettingsPanel();
        }
    }
}