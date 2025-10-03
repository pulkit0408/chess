using UnityEngine;

public class PanelGameObjectManager : MonoBehaviour
{
    [Header("Panels to Monitor")]
    public GameObject Panel1;
    public GameObject Panel2;

    [Header("GameObjects to Toggle")]
    public GameObject GameObject1;
    public GameObject GameObject2;

    void Start()
    {
        // Initialize: Ensure panels are disabled and GameObjects are enabled
        if (Panel1 != null)
            Panel1.SetActive(false);
        if (Panel2 != null)
            Panel2.SetActive(false);

        if (GameObject1 != null)
            GameObject1.SetActive(true);
        if (GameObject2 != null)
            GameObject2.SetActive(true);
    }

    void Update()
    {
        // Check if any panel is active
        bool anyPanelActive = false;

        if (Panel1 != null && Panel1.activeInHierarchy)
            anyPanelActive = true;
        if (Panel2 != null && Panel2.activeInHierarchy)
            anyPanelActive = true;

        // Toggle GameObjects based on panel state
        if (anyPanelActive)
        {
            // If any panel is active, disable GameObjects
            if (GameObject1 != null)
                GameObject1.SetActive(false);
            if (GameObject2 != null)
                GameObject2.SetActive(false);
        }
        else
        {
            // If no panel is active, enable GameObjects
            if (GameObject1 != null)
                GameObject1.SetActive(true);
            if (GameObject2 != null)
                GameObject2.SetActive(true);
        }
    }
}