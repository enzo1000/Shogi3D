using System.Collections.Generic;
using UnityEngine;

public class Lancier : ShogiPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;
        int z = currentZ + direction;

        for (int i = z; i >= 0 && i < tileCountZ; i += direction)
        {
            if (board[currentX, i] == null)
            {
                moves.Add(new Vector2Int(currentX, i));
            }
            else if (board[currentX, i].team != team)
            {
                moves.Add(new Vector2Int(currentX, i));
                break;
            }
            else
                break;
        }

        return moves;
    }
}
