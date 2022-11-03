using System.Collections.Generic;
using UnityEngine;

public class GeneralDeJade : ShogiPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        for (int rows = currentX - 1; rows <= currentX + 1; rows++)
            for (int cols = currentZ - 1; cols <= currentZ + 1; cols++)
                if (rows >= 0 && rows < tileCountX && cols >= 0 && cols < tileCountZ)
                    if (board[rows, cols] == null || board[rows, cols].team != team)
                        moves.Add(new Vector2Int(rows, cols));

        return moves;
    }
}
