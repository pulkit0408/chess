using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private static SaveManager instance;
    private string saveFilePath;

    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject saveManagerObj = new GameObject("SaveManager");
                instance = saveManagerObj.AddComponent<SaveManager>();
                DontDestroyOnLoad(saveManagerObj);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Application.persistentDataPath + "/chessgame.save";
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SaveGame(GameController gameController)
    {
        SaveData saveData = new SaveData();
        saveData.whiteTurn = gameController.WhiteTurn;
        saveData.gameEnded = gameController.gameEnded;

        // Save all pieces
        SavePieces(gameController.WhitePieces, saveData);
        SavePieces(gameController.BlackPieces, saveData);

        // Save move history with captured pieces info
        foreach (var move in gameController.moveHistory)
        {
            MoveData moveData = new MoveData();
            moveData.movedPieceName = move.movedPiece.name;
            moveData.fromX = move.fromPosition.x;
            moveData.fromY = move.fromPosition.y;
            moveData.toX = move.toPosition.x;
            moveData.toY = move.toPosition.y;
            moveData.capturedPieceName = move.capturedPiece != null ? move.capturedPiece.name : "";

            // ADD THIS: Store captured piece tag for proper display restoration
            moveData.capturedPieceTag = move.capturedPiece != null ? move.capturedPiece.tag : "";

            moveData.wasFirstMove = move.wasFirstMove;
            moveData.wasPawnDoubleStep = move.wasPawnDoubleStep;
            moveData.wasCastling = move.wasCastling;
            moveData.wasPromotion = move.wasPromotion;
            moveData.originalPieceName = move.originalPieceName;
            moveData.whitesTurn = move.whitesTurn;

            saveData.moveHistory.Add(moveData);
        }

        string jsonData = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(saveFilePath, jsonData);
        Debug.Log("Game saved successfully!");
    }

    void SavePieces(GameObject piecesContainer, SaveData saveData)
    {
        foreach (Transform piece in piecesContainer.transform)
        {
            PieceController pieceController = piece.GetComponent<PieceController>();

            PieceData pieceData = new PieceData();
            pieceData.pieceName = piece.name;
            pieceData.pieceTag = piece.tag;
            pieceData.posX = piece.position.x;
            pieceData.posY = piece.position.y;
            pieceData.hasMoved = pieceController.HasMoved();
            pieceData.doubleStep = pieceController.DoubleStep;
            pieceData.isActive = piece.gameObject.activeInHierarchy;

            saveData.pieces.Add(pieceData);
        }
    }

    public SaveData LoadGame()
    {
        if (!HasSaveFile()) return null;

        try
        {
            string jsonData = File.ReadAllText(saveFilePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            // Check if the saved game was already completed
            if (saveData.gameEnded)
            {
                Debug.Log("Saved game was already completed, deleting save file");
                DeleteSaveFile();
                return null;
            }

            Debug.Log("Game loaded successfully!");
            return saveData;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load game: " + e.Message);
            return null;
        }
    }

    public void ApplyLoadedData(GameController gameController, SaveData saveData)
    {
        if (saveData == null) return;

        // Apply game state
        gameController.WhiteTurn = saveData.whiteTurn;
        gameController.gameEnded = saveData.gameEnded;

        // Apply piece positions and states
        ApplyPieceData(gameController.WhitePieces, saveData);
        ApplyPieceData(gameController.BlackPieces, saveData);

        // ADD THIS: Restore captured pieces display
        if (gameController.capturedPiecesDisplay != null)
        {
            gameController.capturedPiecesDisplay.ClearAllCapturedPieces();

            // Go through move history to rebuild captured pieces display
            foreach (var moveData in saveData.moveHistory)
            {
                if (!string.IsNullOrEmpty(moveData.capturedPieceName))
                {
                    // Find the captured piece (it should be inactive)
                    GameObject capturedPiece = FindPieceByNameAndTag(
                        gameController.WhitePieces,
                        gameController.BlackPieces,
                        moveData.capturedPieceName,
                        moveData.capturedPieceTag
                    );

                    if (capturedPiece != null && !capturedPiece.activeInHierarchy)
                    {
                        gameController.capturedPiecesDisplay.AddCapturedPiece(capturedPiece);
                    }
                }
            }
        }

        gameController.moveHistory.Clear();
        gameController.UpdateUndoButton();
        Debug.Log("Loaded game state applied!");
    }

    void ApplyPieceData(GameObject piecesContainer, SaveData saveData)
    {
        foreach (Transform piece in piecesContainer.transform)
        {
            PieceData pieceData = saveData.pieces.Find(p => p.pieceName == piece.name && p.pieceTag == piece.tag);

            if (pieceData != null)
            {
                piece.position = new Vector3(pieceData.posX, pieceData.posY, piece.position.z);
                piece.gameObject.SetActive(pieceData.isActive);

                PieceController pieceController = piece.GetComponent<PieceController>();
                pieceController.SetMoved(pieceData.hasMoved);
                pieceController.DoubleStep = pieceData.doubleStep;

                // Handle promoted pieces
                if (pieceData.pieceName.Contains("Queen") && piece.name.Contains("Pawn"))
                {
                    piece.name = pieceData.pieceName;
                    piece.GetComponent<SpriteRenderer>().sprite = pieceController.QueenSprite;
                }
            }
        }
    }

    public bool HasSaveFile()
    {
        return File.Exists(saveFilePath);
    }

    public void DeleteSaveFile()
    {
        if (HasSaveFile())
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file deleted!");
        }
    }
    private GameObject FindPieceByNameAndTag(GameObject whitePieces, GameObject blackPieces, string pieceName, string pieceTag)
    {
        GameObject searchContainer = pieceTag == "White" ? whitePieces : blackPieces;

        foreach (Transform piece in searchContainer.transform)
        {
            if (piece.name == pieceName)
            {
                return piece.gameObject;
            }
        }

        return null;
    }
}