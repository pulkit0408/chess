using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CapturedPiecesDisplay : MonoBehaviour
{
    [Header("Display Areas")]
    public Transform whiteCapturedArea; // Area where white's captured pieces are shown (bottom)
    public Transform blackCapturedArea; // Area where black's captured pieces are shown (top)

    [Header("Display Settings")]
    public GameObject capturedPiecePrefab; // Prefab for displaying captured pieces
    public float pieceSpacing = 0.5f; // Space between captured pieces
    public float pieceScale = 0.7f; // Scale of captured pieces

    private List<GameObject> whiteCapturedDisplay = new List<GameObject>();
    private List<GameObject> blackCapturedDisplay = new List<GameObject>();

    // Track the actual captured pieces for proper restoration
    private Dictionary<GameObject, GameObject> displayToOriginalMap = new Dictionary<GameObject, GameObject>();

    public void AddCapturedPiece(GameObject capturedPiece)
    {
        if (capturedPiece == null) return;

        // Determine which side captured this piece (opposite of the captured piece's color)
        bool capturedByWhite = capturedPiece.tag == "Black";

        // Create display object
        GameObject displayPiece = CreateDisplayPiece(capturedPiece, capturedByWhite);

        // Map display piece to original for undo functionality
        displayToOriginalMap[displayPiece] = capturedPiece;

        // Add to appropriate list and position
        if (capturedByWhite)
        {
            whiteCapturedDisplay.Add(displayPiece);
            RepositionCapturedPieces(whiteCapturedDisplay, whiteCapturedArea);
        }
        else
        {
            blackCapturedDisplay.Add(displayPiece);
            RepositionCapturedPieces(blackCapturedDisplay, blackCapturedArea);
        }
    }

    public void RemoveCapturedPiece(GameObject originalCapturedPiece)
    {
        if (originalCapturedPiece == null) return;

        // Find and remove the display piece
        GameObject displayToRemove = null;

        foreach (var kvp in displayToOriginalMap)
        {
            if (kvp.Value == originalCapturedPiece)
            {
                displayToRemove = kvp.Key;
                break;
            }
        }

        if (displayToRemove != null)
        {
            // Remove from appropriate list
            if (whiteCapturedDisplay.Contains(displayToRemove))
            {
                whiteCapturedDisplay.Remove(displayToRemove);
                RepositionCapturedPieces(whiteCapturedDisplay, whiteCapturedArea);
            }
            else if (blackCapturedDisplay.Contains(displayToRemove))
            {
                blackCapturedDisplay.Remove(displayToRemove);
                RepositionCapturedPieces(blackCapturedDisplay, blackCapturedArea);
            }

            // Clean up
            displayToOriginalMap.Remove(displayToRemove);
            Destroy(displayToRemove);
        }
    }

    private GameObject CreateDisplayPiece(GameObject originalPiece, bool capturedByWhite)
    {
        GameObject displayPiece;

        if (capturedPiecePrefab != null)
        {
            // Use prefab if provided
            displayPiece = Instantiate(capturedPiecePrefab);
        }
        else
        {
            // Create a simple sprite display
            displayPiece = new GameObject($"Captured_{originalPiece.name}");
            displayPiece.AddComponent<SpriteRenderer>();
        }

        // Copy sprite from original piece
        SpriteRenderer originalSR = originalPiece.GetComponent<SpriteRenderer>();
        SpriteRenderer displaySR = displayPiece.GetComponent<SpriteRenderer>();

        if (originalSR != null && displaySR != null)
        {
            displaySR.sprite = originalSR.sprite;
            displaySR.color = new Color(1f, 1f, 1f, 0.8f); // Slightly transparent
            displaySR.sortingOrder = 10; // Ensure it appears above the board
        }

        // Set parent
        Transform parentArea = capturedByWhite ? whiteCapturedArea : blackCapturedArea;
        if (parentArea != null)
        {
            displayPiece.transform.SetParent(parentArea);
        }

        // Apply scale
        displayPiece.transform.localScale = Vector3.one * pieceScale;

        return displayPiece;
    }

    private void RepositionCapturedPieces(List<GameObject> pieces, Transform area)
    {
        if (area == null) return;

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] != null)
            {
                // Position pieces horizontally with spacing
                Vector3 position = area.position;
                position.x += i * pieceSpacing;
                pieces[i].transform.position = position;
            }
        }
    }

    public void ClearAllCapturedPieces()
    {
        // Clear white captured pieces
        foreach (var piece in whiteCapturedDisplay)
        {
            if (piece != null) Destroy(piece);
        }
        whiteCapturedDisplay.Clear();

        // Clear black captured pieces
        foreach (var piece in blackCapturedDisplay)
        {
            if (piece != null) Destroy(piece);
        }
        blackCapturedDisplay.Clear();

        // Clear mapping
        displayToOriginalMap.Clear();
    }
}