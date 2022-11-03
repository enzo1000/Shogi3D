using System.Collections.Generic;
using UnityEngine;

public class Cavalier : ShogiPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        int x;
        int z = currentZ + direction * 2;

        // L
        x = currentX - 1;
        if (CalculMove(ref board, tileCountX, tileCountZ, x, z))
            moves.Add(new Vector2Int(x, z));

        // _|
        x = currentX + 1;
        if (CalculMove(ref board, tileCountX, tileCountZ, x, z))
            moves.Add(new Vector2Int(x, z));

        return moves;
    }

    private bool CalculMove(ref ShogiPiece[,] board, int tileCountX, int tileCountZ, int x, int z)
    {
        if (x >= 0 && x < tileCountX && z < tileCountZ && z >= 0)
            if (board[x, z] == null || board[x, z].team != team)
                return true;
        return false;
    }
}
