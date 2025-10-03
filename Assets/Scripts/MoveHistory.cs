using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoveHistory
{
    public GameObject movedPiece;
    public Vector3 fromPosition;
    public Vector3 toPosition;
    public GameObject capturedPiece;
    public bool wasFirstMove;
    public bool wasPawnDoubleStep;
    public bool wasCastling;
    public GameObject castlingRook;
    public Vector3 rookFromPosition;
    public Vector3 rookToPosition;
    public bool wasPromotion;
    public string originalPieceName;
    public Sprite originalSprite;
    public bool whitesTurn;

    public MoveHistory(GameObject piece, Vector3 from, Vector3 to, GameObject captured = null)
    {
        movedPiece = piece;
        fromPosition = from;
        toPosition = to;
        capturedPiece = captured;

        // Store piece state
        PieceController pieceController = piece.GetComponent<PieceController>();
        wasFirstMove = !pieceController.HasMoved();
        wasPawnDoubleStep = pieceController.DoubleStep;

        // Store original piece info for promotion undo
        originalPieceName = piece.name;
        originalSprite = piece.GetComponent<SpriteRenderer>().sprite;

        // Detect castling
        if (piece.name.Contains("King") && Mathf.Abs(from.x - to.x) == 2)
        {
            wasCastling = true;
            // Find the rook that moved
            if (from.x - to.x == 2) // Queenside
            {
                castlingRook = pieceController.GetPieceOnPosition(from.x - 4, from.y, piece.tag);
                if (castlingRook != null)
                {
                    rookFromPosition = new Vector3(from.x - 4, from.y, 0);
                    rookToPosition = new Vector3(from.x - 1, from.y, 0);
                }
            }
            else // Kingside
            {
                castlingRook = pieceController.GetPieceOnPosition(from.x + 3, from.y, piece.tag);
                if (castlingRook != null)
                {
                    rookFromPosition = new Vector3(from.x + 3, from.y, 0);
                    rookToPosition = new Vector3(from.x + 1, from.y, 0);
                }
            }
        }

        // Detect promotion - check if the piece name changed from containing "Pawn" to containing "Queen"
        wasPromotion = false; // We'll set this in the PieceController when promotion happens
    }
}



/*public void OpenGame()
{
    Time.timeScale = 2f;
    // Load main menu scene (replace "MainMenu" with your actual main menu scene name)
    UnityEngine.SceneManagement.SceneManager.LoadScene("start");
}
public void ExitGame()
{

    Application.Quit();

    // For editor testing
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
}*/