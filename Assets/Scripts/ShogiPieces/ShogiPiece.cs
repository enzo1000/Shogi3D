using System.Collections.Generic;
using UnityEngine;

public enum ShogiPieceType
{
    None = 0,
    Pion = 1,
    Tour = 2,
    Fou = 3,
    Lancier = 4,
    Cavalier = 5,
    GeneralDargent = 6,
    GeneralDor = 7,
    GeneralDeJade = 8,
    Roi = 9,
}
public enum ShogiPromuType
{
    None = 0,
    PionDor = 1,
    Dragon = 2,
    ChevalDragon = 3,
    LancierDor = 4,
    CavalierDor = 5,
    ArgentDor = 6,
}

public class ShogiPiece : MonoBehaviour
{
    public int team;    //Reignant = 0 / Opposant = 1
    public int currentX;
    public int currentZ;
    public ShogiPieceType type;
    public ShogiPromuType typeP;
    public bool isPromoted = false;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    //The pieces move by default like golden generals
    public virtual List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        for (int rows = currentX - 1; rows <= currentX + 1; rows++)
            for (int cols = currentZ - 1; cols <= currentZ + 1; cols++)
                if (rows >= 0 && rows < tileCountX && cols >= 0 && cols < tileCountZ)
                    if (board[rows, cols] == null || board[rows, cols].team != team)
                        moves.Add(new Vector2Int(rows, cols));

        if (moves.Contains(new Vector2Int(currentX - 1, currentZ - direction)))
            moves.Remove(new Vector2Int(currentX - 1, currentZ - direction));

        if (moves.Contains(new Vector2Int(currentX + 1, currentZ - direction)))
            moves.Remove(new Vector2Int(currentX + 1, currentZ - direction));

        return moves;
    }

    //moveList is a list of Vector2Int[] : moveList[0][0] contain the last position of the piece and moveList[0][1] the new one
    // we can access to the coordinate by the attribute .x and .y thanks to the vectors
    public virtual SpecialMove GetIfPromotion(ref ShogiPiece[,] board, ref List<Vector2Int[]> moveList) 
    {
        return SpecialMove.None;
    }
    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
    public virtual List<Vector2Int> isDropable(ref ShogiPiece[,] board, int TILE_COUNT_X, int TILE_COUNT_Z)
    {
        List<Vector2Int> dropList = new List<Vector2Int>();

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int z = 0; z < TILE_COUNT_Z; z++)
                if (board[x, z] == null)
                    dropList.Add(new Vector2Int(x, z));

        return dropList;
    }
}