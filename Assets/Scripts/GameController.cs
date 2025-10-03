using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject Board;
    public GameObject WhitePieces;
    public GameObject BlackPieces;
    public GameObject SelectedPiece;

    [Header("UI Elements")]
    public GameObject GameOverPanel;
    public GameObject MainMenuConfirmPanel;
    public GameObject SaveGamePanel;  // NEW: Save confirmation panel
    public TextMeshProUGUI WinnerText;
    public Button PlayAgainButton;
    public Button MainMenuButton;
    public Button UndoButton;
    public Button ConfirmMainMenuButton;
    public Button CancelMainMenuButton;

    // NEW: Save panel buttons
    public Button SaveAndExitButton;
    public Button ExitWithoutSavingButton;
    public Button CancelSaveButton;

    [Header("Captured Pieces Display")]
    public CapturedPiecesDisplay capturedPiecesDisplay;

    [Header("Audio Clips")]
    public AudioClip WinSound;
    public AudioClip DrawSound;
    public AudioClip UndoSound;
    public AudioClip MoveSound;
    public AudioClip CaptureSound;
    public AudioClip WrongMoveSound;

    [Header("Highlight Colors")]
    public Color ValidMoveColor = Color.green;
    public Color NormalSquareColor = Color.white;

    public bool WhiteTurn = true;

    [HideInInspector] public List<MoveHistory> moveHistory = new List<MoveHistory>();
    [HideInInspector] public bool gameEnded = false;

    private List<GameObject> highlightedSquares = new List<GameObject>();

    void Start()
    {
        SetupButtonListeners();

        if (GameOverPanel != null)
            GameOverPanel.SetActive(false);
        if (MainMenuConfirmPanel != null)
            MainMenuConfirmPanel.SetActive(false);
        if (SaveGamePanel != null)
            SaveGamePanel.SetActive(false);

        // Check if this is a loaded game
        CheckForLoadedGame();

        UpdateUndoButton();
    }

    void SetupButtonListeners()
    {
        if (PlayAgainButton != null)
            PlayAgainButton.onClick.AddListener(PlayAgain);
        if (MainMenuButton != null)
            MainMenuButton.onClick.AddListener(ShowMainMenuConfirmation);
        if (UndoButton != null)
            UndoButton.onClick.AddListener(UndoLastMove);
        if (ConfirmMainMenuButton != null)
            ConfirmMainMenuButton.onClick.AddListener(ShowSaveGamePanel);
        if (CancelMainMenuButton != null)
            CancelMainMenuButton.onClick.AddListener(HideMainMenuConfirmation);

        // NEW: Save panel listeners
        if (SaveAndExitButton != null)
            SaveAndExitButton.onClick.AddListener(SaveAndExit);
        if (ExitWithoutSavingButton != null)
            ExitWithoutSavingButton.onClick.AddListener(ExitWithoutSaving);
        if (CancelSaveButton != null)
            CancelSaveButton.onClick.AddListener(HideSaveGamePanel);
    }

    void CheckForLoadedGame()
    {
        // Check if we need to load a saved game (set by StartMenuController)
        if (PlayerPrefs.GetInt("LoadSavedGame", 0) == 1)
        {
            PlayerPrefs.DeleteKey("LoadSavedGame");
            LoadGame();
        }
    }

    // NEW: Save game functionality
    void ShowSaveGamePanel()
    {
        HideMainMenuConfirmation();
        if (SaveGamePanel != null)
        {
            SaveGamePanel.SetActive(true);
        }
    }

    void HideSaveGamePanel()
    {
        if (SaveGamePanel != null)
        {
            SaveGamePanel.SetActive(false);
        }
        ShowMainMenuConfirmation(); // Go back to main menu confirmation
    }

    void SaveAndExit()
    {
        SaveManager.Instance.SaveGame(this);
        ExitToMainMenu();
    }

    void ExitWithoutSaving()
    {
        ExitToMainMenu();
    }

    void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("start");
    }

    void LoadGame()
    {
        SaveData saveData = SaveManager.Instance.LoadGame();
        if (saveData != null)
        {
            SaveManager.Instance.ApplyLoadedData(this, saveData);
        }
    }

    // NEW: Public method to update undo button
    public void UpdateUndoButton()
    {
        if (UndoButton != null)
        {
            UndoButton.interactable = moveHistory.Count > 0 && !gameEnded;
        }
    }

    // --- Original methods remain the same ---
    public void SelectPiece(GameObject piece)
    {
        if (piece.tag == "White" && WhiteTurn == true || piece.tag == "Black" && WhiteTurn == false)
        {
            DeselectPiece();
            SelectedPiece = piece;

            SelectedPiece.GetComponent<SpriteRenderer>().color = Color.yellow;

            Vector3 newPosition = SelectedPiece.transform.position;
            newPosition.z = -1;
            SelectedPiece.transform.SetPositionAndRotation(newPosition, SelectedPiece.transform.rotation);

            HighlightValidMoves(SelectedPiece);
        }
    }

    public void DeselectPiece()
    {
        if (SelectedPiece != null)
        {
            SelectedPiece.GetComponent<SpriteRenderer>().color = Color.white;

            Vector3 newPosition = SelectedPiece.transform.position;
            newPosition.z = 0;
            SelectedPiece.transform.SetPositionAndRotation(newPosition, SelectedPiece.transform.rotation);

            SelectedPiece = null;
        }

        ClearMoveHighlights();
    }

    void HighlightValidMoves(GameObject piece)
    {
        PieceController pieceController = piece.GetComponent<PieceController>();
        GameObject encounteredEnemy;

        foreach (Transform square in Board.transform)
        {
            Vector3 targetPosition = new Vector3(square.position.x, square.position.y, piece.transform.position.z);

            if (pieceController.ValidateMovement(piece.transform.position, targetPosition, out encounteredEnemy))
            {
                BoxController boxController = square.GetComponentInChildren<BoxController>();
                if (boxController != null)
                {
                    SpriteRenderer squareRenderer = boxController.GetComponent<SpriteRenderer>();
                    if (squareRenderer != null)
                    {
                        squareRenderer.color = ValidMoveColor;
                        highlightedSquares.Add(boxController.gameObject);
                    }
                }
            }
        }
    }

    void ClearMoveHighlights()
    {
        foreach (GameObject square in highlightedSquares)
        {
            if (square != null)
            {
                SpriteRenderer squareRenderer = square.GetComponent<SpriteRenderer>();
                if (squareRenderer != null)
                {
                    squareRenderer.color = NormalSquareColor;
                }
            }
        }
        highlightedSquares.Clear();
    }

    public void RecordMove(GameObject piece, Vector3 fromPos, Vector3 toPos, GameObject capturedPiece = null)
    {
        if (gameEnded) return;

        MoveHistory move = new MoveHistory(piece, fromPos, toPos, capturedPiece);
        move.whitesTurn = WhiteTurn;
        moveHistory.Add(move);

        // ADD THIS: Display captured piece
        if (capturedPiece != null && capturedPiecesDisplay != null)
        {
            capturedPiecesDisplay.AddCapturedPiece(capturedPiece);
        }

        PlayMoveSound(capturedPiece != null);
        UpdateUndoButton();
    }

    public void PlayMoveSound(bool wasCapture)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            AudioClip clipToPlay = null;

            if (wasCapture && CaptureSound != null)
            {
                clipToPlay = CaptureSound;
            }
            else if (MoveSound != null)
            {
                clipToPlay = MoveSound;
            }

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }
    }

    public void UpdateLastMoveForPromotion(string originalName, Sprite originalSprite)
    {
        if (moveHistory.Count > 0)
        {
            int lastIndex = moveHistory.Count - 1;
            moveHistory[lastIndex].wasPromotion = true;
            moveHistory[lastIndex].originalPieceName = originalName;
            moveHistory[lastIndex].originalSprite = originalSprite;
        }
    }

    public void UndoLastMove()
    {
        if (moveHistory.Count == 0 || gameEnded) return;

        MoveHistory lastMove = moveHistory[moveHistory.Count - 1];
        moveHistory.RemoveAt(moveHistory.Count - 1);

        DeselectPiece();
        WhiteTurn = lastMove.whitesTurn;

        lastMove.movedPiece.transform.position = lastMove.fromPosition;

        PieceController pieceController = lastMove.movedPiece.GetComponent<PieceController>();

        if (lastMove.wasFirstMove)
        {
            pieceController.SetMoved(false);
        }

        if (lastMove.wasPawnDoubleStep)
        {
            pieceController.DoubleStep = false;
        }

        if (lastMove.wasPromotion)
        {
            lastMove.movedPiece.name = lastMove.originalPieceName;
            lastMove.movedPiece.GetComponent<SpriteRenderer>().sprite = lastMove.originalSprite;
        }

        if (lastMove.wasCastling && lastMove.castlingRook != null)
        {
            lastMove.castlingRook.transform.position = lastMove.rookFromPosition;
            lastMove.castlingRook.GetComponent<PieceController>().SetMoved(false);
        }

        if (lastMove.capturedPiece != null)
        {
            lastMove.capturedPiece.SetActive(true);
            lastMove.capturedPiece.transform.position = lastMove.toPosition;

            // ADD THIS: Remove from captured pieces display
            if (capturedPiecesDisplay != null)
            {
                capturedPiecesDisplay.RemoveCapturedPiece(lastMove.capturedPiece);
            }
        }

        GameObject currentPlayerPieces = WhiteTurn ? WhitePieces : BlackPieces;
        foreach (Transform piece in currentPlayerPieces.transform)
        {
            if (piece.name.Contains("Pawn"))
            {
                piece.GetComponent<PieceController>().DoubleStep = false;
            }
        }

        if (UndoSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(UndoSound);
            }
        }

        UpdateUndoButton();
    }


    public void EndGameByCapture(string winnerColor)
    {
        if (gameEnded) return;

        gameEnded = true;
        UpdateUndoButton();
        DeselectPiece();

        // Delete save file when game ends
        SaveManager.Instance.DeleteSaveFile();

        ShowGameOver("King Captured!", $"{winnerColor} Player Wins!", WinSound);
    }

    public void EndTurn()
    {
        bool kingIsInCheck = false;
        bool hasValidMoves = false;
        GameObject currentKing = null;

        WhiteTurn = !WhiteTurn;

        GameObject currentPlayerPieces = WhiteTurn ? WhitePieces : BlackPieces;

        foreach (Transform piece in currentPlayerPieces.transform)
        {
            if (piece.gameObject.activeInHierarchy)
            {
                if (piece.name.Contains("King"))
                {
                    currentKing = piece.gameObject;
                    kingIsInCheck = piece.GetComponent<PieceController>().IsInCheck(piece.position);
                }

                if (piece.name.Contains("Pawn"))
                {
                    piece.GetComponent<PieceController>().DoubleStep = false;
                }

                if (!hasValidMoves && HasValidMoves(piece.gameObject))
                {
                    hasValidMoves = true;
                }
            }
        }

        if (!hasValidMoves)
        {
            if (kingIsInCheck)
            {
                Checkmate();
            }
            else
            {
                Stalemate();
            }
        }
    }

    bool HasValidMoves(GameObject piece)
    {
        PieceController pieceController = piece.GetComponent<PieceController>();
        GameObject encounteredEnemy;

        foreach (Transform square in Board.transform)
        {
            Vector3 targetPosition = new Vector3(square.position.x, square.position.y, piece.transform.position.z);

            if (targetPosition.x == piece.transform.position.x && targetPosition.y == piece.transform.position.y)
                continue;

            if (pieceController.ValidateMovement(piece.transform.position, targetPosition, out encounteredEnemy))
            {
                if (IsMoveLegal(piece, piece.transform.position, targetPosition))
                {
                    return true;
                }
            }
        }
        return false;
    }

    bool IsMoveLegal(GameObject piece, Vector3 fromPos, Vector3 toPos)
    {
        Vector3 originalPos = piece.transform.position;
        GameObject capturedPiece = null;

        PieceController pieceController = piece.GetComponent<PieceController>();
        capturedPiece = pieceController.GetPieceOnPosition(toPos.x, toPos.y);

        piece.transform.position = toPos;

        if (capturedPiece != null)
        {
            capturedPiece.SetActive(false);
        }

        GameObject ourKing = FindKingForPlayer(piece.tag);
        bool kingInCheck = false;

        if (ourKing != null)
        {
            kingInCheck = ourKing.GetComponent<PieceController>().IsInCheck(ourKing.transform.position);
        }

        piece.transform.position = originalPos;

        if (capturedPiece != null)
        {
            capturedPiece.SetActive(true);
        }

        return !kingInCheck;
    }

    GameObject FindKingForPlayer(string playerTag)
    {
        GameObject pieces = (playerTag == "White") ? WhitePieces : BlackPieces;

        foreach (Transform piece in pieces.transform)
        {
            if (piece.gameObject.activeInHierarchy && piece.name.Contains("King"))
            {
                return piece.gameObject;
            }
        }

        return null;
    }

    void Stalemate()
    {
        gameEnded = true;
        UpdateUndoButton();

        // Delete save file when game ends
        SaveManager.Instance.DeleteSaveFile();

        ShowGameOver("Stalemate!", "It's a Draw!", DrawSound);
    }

    void Checkmate()
    {
        string winner = WhiteTurn ? "Black" : "White";
        gameEnded = true;
        UpdateUndoButton();

        // Delete save file when game ends
        SaveManager.Instance.DeleteSaveFile();

        ShowGameOver("Checkmate!", $"{winner} Player Wins!", WinSound);
    }

    void ShowGameOver(string gameResult, string winnerMessage, AudioClip soundToPlay)
    {
        Time.timeScale = 0f;

        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);
        }

        if (WinnerText != null)
        {
            WinnerText.text = $"{gameResult}\n{winnerMessage}";
        }

        if (soundToPlay != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
        }

        StartCoroutine(CelebrationEffect());
    }

    IEnumerator CelebrationEffect()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        foreach (Transform square in Board.transform)
        {
            SpriteRenderer sr = square.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                StartCoroutine(FlashSquare(sr));
            }
        }
    }

    IEnumerator FlashSquare(SpriteRenderer spriteRenderer)
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.yellow;
        yield return new WaitForSecondsRealtime(0.2f);
        spriteRenderer.color = originalColor;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        moveHistory.Clear();
        gameEnded = false;

        // ADD THIS: Clear captured pieces display
        if (capturedPiecesDisplay != null)
        {
            capturedPiecesDisplay.ClearAllCapturedPieces();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ShowMainMenuConfirmation()
    {
        if (MainMenuConfirmPanel != null)
        {
            MainMenuConfirmPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void HideMainMenuConfirmation()
    {
        if (MainMenuConfirmPanel != null)
        {
            MainMenuConfirmPanel.SetActive(false);
            if (!gameEnded)
            {
                Time.timeScale = 1f;
            }
        }
    }

    public void mainmenuDirect()
    {
        Time.timeScale = 2f;
        // Load main menu scene (replace "MainMenu" with your actual main menu scene name)
        UnityEngine.SceneManagement.SceneManager.LoadScene("start");
    }
}