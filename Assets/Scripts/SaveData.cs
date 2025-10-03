using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
    public bool whiteTurn;
    public bool gameEnded;
    public List<PieceData> pieces = new List<PieceData>();
    public List<MoveData> moveHistory = new List<MoveData>();
}

[System.Serializable]
public class PieceData
{
    public string pieceName;
    public string pieceTag;
    public float posX, posY;
    public bool hasMoved;
    public bool doubleStep;
    public bool isActive;
}

[System.Serializable]
public class MoveData
{
    public string movedPieceName;
    public float fromX, fromY, toX, toY;
    public string capturedPieceName;
    public string capturedPieceTag; // ADD THIS LINE
    public bool wasFirstMove;
    public bool wasPawnDoubleStep;
    public bool wasCastling;
    public bool wasPromotion;
    public string originalPieceName;
    public bool whitesTurn;
}
