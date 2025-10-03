using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public GameController GameController;
    public GameObject WhitePieces;
    public GameObject BlackPieces;
    public Sprite QueenSprite;

    public float MoveSpeed = 20;

    public float HighestRankY = 3.5f;
    public float LowestRankY = -3.5f;

    [HideInInspector]
    public bool DoubleStep = false;
    [HideInInspector]
    public bool MovingY = false;
    [HideInInspector]
    public bool MovingX = false;

    private Vector3 oldPosition;
    private Vector3 newPositionY;
    private Vector3 newPositionX;

    private bool moved = false;
    private GameObject capturedPiece = null; // Store captured piece for undo

    // Use this for initialization
    [System.Obsolete]
    void Start()
    {
        if (GameController == null) GameController = FindObjectOfType<GameController>();
        if (this.name.Contains("Knight")) MoveSpeed *= 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (MovingY || MovingX)
        {
            if (Mathf.Abs(oldPosition.x - newPositionX.x) == Mathf.Abs(oldPosition.y - newPositionX.y))
            {
                MoveDiagonally();
            }
            else
            {
                MoveSideBySide();
            }
        }
    }

    void OnMouseDown()
    {
        if (GameController.SelectedPiece != null && GameController.SelectedPiece.GetComponent<PieceController>().IsMoving() == true)
        {
            // Prevent clicks during movement
            return;
        }

        if (GameController.SelectedPiece == this.gameObject)
        {
            GameController.DeselectPiece();
        }
        else
        {
            if (GameController.SelectedPiece == null)
            {
                GameController.SelectPiece(this.gameObject);
            }
            else
            {
                if (this.tag == GameController.SelectedPiece.tag)
                {
                    GameController.SelectPiece(this.gameObject);
                }
                else if ((this.tag == "White" && GameController.SelectedPiece.tag == "Black") || (this.tag == "Black" && GameController.SelectedPiece.tag == "White"))
                {
                    GameController.SelectedPiece.GetComponent<PieceController>().MovePiece(this.transform.position);
                }
            }
        }
    }

    public bool MovePiece(Vector3 newPosition, bool castling = false)
    {
        GameObject encounteredEnemy = null;

        newPosition.z = this.transform.position.z;
        this.oldPosition = this.transform.position;

        if (castling || ValidateMovement(oldPosition, newPosition, out encounteredEnemy))
        {
            // Check if we're capturing a king - this should end the game immediately
            if (encounteredEnemy != null && encounteredEnemy.name.Contains("King"))
            {
                Debug.Log($"King captured! {this.tag} wins by capturing {encounteredEnemy.name}");

                // Hide the captured king
                encounteredEnemy.SetActive(false);

                // Move the capturing piece to the king's position
                this.transform.position = newPosition;

                // End the game immediately
                if (GameController != null)
                {
                    GameController.RecordMove(this.gameObject, oldPosition, newPosition, encounteredEnemy);
                    GameController.EndGameByCapture(this.tag);
                }

                return true;
            }

            // Store original piece info before any changes for undo system
            string originalName = this.name;
            Sprite originalSprite = this.GetComponent<SpriteRenderer>().sprite;
            bool wasPromotion = false;

            // Double-step
            if (this.name.Contains("Pawn") && Mathf.Abs(oldPosition.y - newPosition.y) == 2)
            {
                this.DoubleStep = true;
            }
            // Promotion
            else if (this.name.Contains("Pawn") && (newPosition.y == HighestRankY || newPosition.y == LowestRankY))
            {
                wasPromotion = true;
                this.Promote();
            }
            // Castling
            else if (this.name.Contains("King") && Mathf.Abs(oldPosition.x - newPosition.x) == 2)
            {
                if (oldPosition.x - newPosition.x == 2) // queenside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x - 4, oldPosition.y, this.tag);
                    if (rook != null)
                    {
                        Vector3 newRookPosition = oldPosition;
                        newRookPosition.x -= 1;
                        rook.GetComponent<PieceController>().MovePiece(newRookPosition, true);
                    }
                }
                else if (oldPosition.x - newPosition.x == -2) // kingside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x + 3, oldPosition.y, this.tag);
                    if (rook != null)
                    {
                        Vector3 newRookPosition = oldPosition;
                        newRookPosition.x += 1;
                        rook.GetComponent<PieceController>().MovePiece(newRookPosition, true);
                    }
                }
            }

            // Record move in history (only for main moves, not castling rook moves)
            if (!castling && GameController != null)
            {
                GameController.RecordMove(this.gameObject, oldPosition, newPosition, encounteredEnemy);

                // Handle promotion info update
                if (wasPromotion)
                {
                    GameController.UpdateLastMoveForPromotion(originalName, originalSprite);
                }
            }

            this.moved = true;
            capturedPiece = encounteredEnemy;

            this.newPositionY = newPosition;
            this.newPositionY.x = this.transform.position.x;
            this.newPositionX = newPosition;
            MovingY = true; // Start movement

            // Hide captured piece instead of destroying it (for undo functionality)
            if (encounteredEnemy != null)
            {
                encounteredEnemy.SetActive(false);
            }
            return true;
        }
        else
        {
            if (GameController != null)
            {
                AudioSource audioSource = GameController.GetComponent<AudioSource>();
                if (audioSource != null && GameController.WrongMoveSound != null)
                {
                    audioSource.PlayOneShot(GameController.WrongMoveSound);
                }
            }
            return false;
        }
    }

    public bool ValidateMovement(Vector3 oldPosition, Vector3 newPosition, out GameObject encounteredEnemy, bool excludeCheck = false)
    {
        bool isValid = false;
        encounteredEnemy = GetPieceOnPosition(newPosition.x, newPosition.y);

        if ((oldPosition.x == newPosition.x && oldPosition.y == newPosition.y) || encounteredEnemy != null && encounteredEnemy.tag == this.tag)
        {
            return false;
        }

        if (this.name.Contains("King"))
        {
            // If the path is 1 square away in any direction
            if (Mathf.Abs(oldPosition.x - newPosition.x) <= 1 && Mathf.Abs(oldPosition.y - newPosition.y) <= 1)
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
            // Check for castling
            else if (Mathf.Abs(oldPosition.x - newPosition.x) == 2 && oldPosition.y == newPosition.y && this.moved == false)
            {
                if (oldPosition.x - newPosition.x == 2) // queenside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x - 4, oldPosition.y, this.tag);
                    if (rook != null && rook.name.Contains("Rook") && rook.GetComponent<PieceController>().moved == false &&
                        CountPiecesBetweenPoints(oldPosition, rook.transform.position, Direction.Horizontal) == 0)
                    {
                        if (excludeCheck == true ||
                            (excludeCheck == false &&
                             IsInCheck(new Vector3(oldPosition.x - 0, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x - 1, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x - 2, oldPosition.y)) == false))
                        {
                            isValid = true;
                        }
                    }
                }
                else if (oldPosition.x - newPosition.x == -2) // kingside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x + 3, oldPosition.y, this.tag);
                    if (rook != null && rook.name.Contains("Rook") && rook.GetComponent<PieceController>().moved == false &&
                        CountPiecesBetweenPoints(oldPosition, rook.transform.position, Direction.Horizontal) == 0)
                    {
                        if (excludeCheck == true ||
                            (excludeCheck == false &&
                             IsInCheck(new Vector3(oldPosition.x + 0, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x + 1, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x + 2, oldPosition.y)) == false))
                        {
                            isValid = true;
                        }
                    }
                }
            }
        }

        if (this.name.Contains("Rook") || this.name.Contains("Queen"))
        {
            // If the path is a straight horizontal or vertical line
            if ((oldPosition.x == newPosition.x && CountPiecesBetweenPoints(oldPosition, newPosition, Direction.Vertical) == 0) ||
                (oldPosition.y == newPosition.y && CountPiecesBetweenPoints(oldPosition, newPosition, Direction.Horizontal) == 0))
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
        }

        if (this.name.Contains("Bishop") || this.name.Contains("Queen"))
        {
            // If the path is a straight diagonal line
            if (Mathf.Abs(oldPosition.x - newPosition.x) == Mathf.Abs(oldPosition.y - newPosition.y) &&
                CountPiecesBetweenPoints(oldPosition, newPosition, Direction.Diagonal) == 0)
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
        }

        if (this.name.Contains("Knight"))
        {
            // If the path is an 'L' shape
            if ((Mathf.Abs(oldPosition.x - newPosition.x) == 1 && Mathf.Abs(oldPosition.y - newPosition.y) == 2) ||
                (Mathf.Abs(oldPosition.x - newPosition.x) == 2 && Mathf.Abs(oldPosition.y - newPosition.y) == 1))
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
        }

        if (this.name.Contains("Pawn"))
        {
            // If the new position is on the rank above (White) or below (Black)
            if ((this.tag == "White" && oldPosition.y + 1 == newPosition.y) ||
               (this.tag == "Black" && oldPosition.y - 1 == newPosition.y))
            {
                GameObject otherPiece = GetPieceOnPosition(newPosition.x, newPosition.y);

                // If moving forward
                if (oldPosition.x == newPosition.x && otherPiece == null)
                {
                    if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                    {
                        isValid = true;
                    }
                }
                // If moving diagonally
                else if (oldPosition.x == newPosition.x - 1 || oldPosition.x == newPosition.x + 1)
                {
                    // Check if en passant is available
                    if (otherPiece == null)
                    {
                        otherPiece = GetPieceOnPosition(newPosition.x, oldPosition.y);
                        if (otherPiece != null && otherPiece.GetComponent<PieceController>().DoubleStep == false)
                        {
                            otherPiece = null;
                        }
                    }
                    // If an enemy piece is encountered
                    if (otherPiece != null && otherPiece.tag != this.tag)
                    {
                        if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                        {
                            isValid = true;
                        }
                    }
                }

                encounteredEnemy = otherPiece;
            }
            // Double-step
            else if ((this.tag == "White" && oldPosition.x == newPosition.x && oldPosition.y + 2 == newPosition.y) ||
                     (this.tag == "Black" && oldPosition.x == newPosition.x && oldPosition.y - 2 == newPosition.y))
            {
                if (this.moved == false && GetPieceOnPosition(newPosition.x, newPosition.y) == null)
                {
                    // Check if the square in between is also empty
                    float middleY = (this.tag == "White") ? oldPosition.y + 1 : oldPosition.y - 1;
                    if (GetPieceOnPosition(newPosition.x, middleY) == null)
                    {
                        if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                        {
                            isValid = true;
                        }
                    }
                }
            }
        }

        return isValid;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="positionX"></param>
    /// <param name="positionY"></param>
    /// <param name="color">"White" or "Black" for specific color, null for any color</param>
    /// <returns>Returns the piece on a given position on the board, null if the square is empty</returns>
    public GameObject GetPieceOnPosition(float positionX, float positionY, string color = null)
    {
        if (color == null || color.ToLower() == "white")
        {
            foreach (Transform piece in WhitePieces.transform)
            {
                if (piece.gameObject.activeInHierarchy && piece.position.x == positionX && piece.position.y == positionY)
                {
                    return piece.gameObject;
                }
            }
        }
        if (color == null || color.ToLower() == "black")
        {
            foreach (Transform piece in BlackPieces.transform)
            {
                if (piece.gameObject.activeInHierarchy && piece.position.x == positionX && piece.position.y == positionY)
                {
                    return piece.gameObject;
                }
            }
        }

        return null;
    }

    int CountPiecesBetweenPoints(Vector3 pointA, Vector3 pointB, Direction direction)
    {
        int count = 0;

        foreach (Transform piece in WhitePieces.transform)
        {
            if (!piece.gameObject.activeInHierarchy) continue;

            if ((direction == Direction.Horizontal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) && piece.position.y == pointA.y) ||
                (direction == Direction.Vertical && piece.position.y > Mathf.Min(pointA.y, pointB.y) && piece.position.y < Mathf.Max(pointA.y, pointB.y) && piece.position.x == pointA.x))
            {
                count++;
            }
            else if (direction == Direction.Diagonal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) &&
                     ((pointA.y - pointA.x == pointB.y - pointB.x && piece.position.y - piece.position.x == pointA.y - pointA.x) ||
                      (pointA.y + pointA.x == pointB.y + pointB.x && piece.position.y + piece.position.x == pointA.y + pointA.x)))
            {
                count++;
            }
        }
        foreach (Transform piece in BlackPieces.transform)
        {
            if (!piece.gameObject.activeInHierarchy) continue;

            if ((direction == Direction.Horizontal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) && piece.position.y == pointA.y) ||
                (direction == Direction.Vertical && piece.position.y > Mathf.Min(pointA.y, pointB.y) && piece.position.y < Mathf.Max(pointA.y, pointB.y) && piece.position.x == pointA.x))
            {
                count++;
            }
            else if (direction == Direction.Diagonal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) &&
                     ((pointA.y - pointA.x == pointB.y - pointB.x && piece.position.y - piece.position.x == pointA.y - pointA.x) ||
                      (pointA.y + pointA.x == pointB.y + pointB.x && piece.position.y + piece.position.x == pointA.y + pointA.x)))
            {
                count++;
            }
        }

        return count;
    }

    // Helper method to find the king of a specific color
    private GameObject FindKing(string playerColor)
    {
        GameObject pieces = (playerColor.ToLower() == "white") ? WhitePieces : BlackPieces;

        foreach (Transform piece in pieces.transform)
        {
            if (piece.gameObject.activeInHierarchy && piece.name.Contains("King"))
            {
                return piece.gameObject;
            }
        }

        return null;
    }

    public bool IsInCheck(Vector3 potentialPosition)
    {
        bool isInCheck = false;

        // Temporarily move piece to the wanted position
        Vector3 currentPosition = this.transform.position;
        this.transform.SetPositionAndRotation(potentialPosition, this.transform.rotation);

        GameObject encounteredEnemy;

        // Find the king of the current piece's color
        GameObject king = FindKing(this.tag);
        if (king == null)
        {
            Debug.LogError($"Could not find {this.tag} king!");
            this.transform.SetPositionAndRotation(currentPosition, this.transform.rotation);
            return false;
        }

        Vector3 kingPosition = king.transform.position;

        // Check if any enemy piece can attack the king
        GameObject enemyPieces = (this.tag == "White") ? BlackPieces : WhitePieces;

        foreach (Transform piece in enemyPieces.transform)
        {
            // If piece is not potentially captured and is active
            if (piece.gameObject.activeInHierarchy &&
                (piece.position.x != potentialPosition.x || piece.position.y != potentialPosition.y))
            {
                if (piece.GetComponent<PieceController>().ValidateMovement(piece.position, kingPosition, out encounteredEnemy, true))
                {
                    Debug.Log($"{this.tag} King is in check by: {piece.name} at {piece.position}");
                    isInCheck = true;
                    break;
                }
            }
        }

        // Move back to the real position
        this.transform.SetPositionAndRotation(currentPosition, this.transform.rotation);
        return isInCheck;
    }

    void MoveSideBySide()
    {
        if (MovingY == true)
        {
            this.transform.SetPositionAndRotation(Vector3.Lerp(this.transform.position, newPositionY, Time.deltaTime * MoveSpeed), this.transform.rotation);
            if (this.transform.position == newPositionY)
            {
                MovingY = false;
                MovingX = true;
            }
        }
        if (MovingX == true)
        {
            this.transform.SetPositionAndRotation(Vector3.Lerp(this.transform.position, newPositionX, Time.deltaTime * MoveSpeed), this.transform.rotation);
            if (this.transform.position == newPositionX)
            {
                this.transform.SetPositionAndRotation(newPositionX, this.transform.rotation);
                MovingX = false;
                if (GameController.SelectedPiece != null)
                {
                    GameController.DeselectPiece();
                    GameController.EndTurn();
                }
            }
        }
    }

    void MoveDiagonally()
    {
        if (MovingY == true)
        {
            this.transform.SetPositionAndRotation(Vector3.Lerp(this.transform.position, newPositionX, Time.deltaTime * MoveSpeed), this.transform.rotation);
            if (this.transform.position == newPositionX)
            {
                this.transform.SetPositionAndRotation(newPositionX, this.transform.rotation);
                MovingY = false;
                MovingX = false;
                if (GameController.SelectedPiece != null)
                {
                    GameController.DeselectPiece();
                    GameController.EndTurn();
                }
            }
        }
    }

    void Promote()
    {
        this.name = this.name.Replace("Pawn", "Queen");
        this.GetComponent<SpriteRenderer>().sprite = QueenSprite;
    }

    public bool IsMoving()
    {
        return MovingX || MovingY;
    }

    // Methods for undo functionality
    public bool HasMoved()
    {
        return moved;
    }

    public void SetMoved(bool hasMoved)
    {
        moved = hasMoved;
    }

    enum Direction
    {
        Horizontal,
        Vertical,
        Diagonal
    }
}

