using System.Collections.Generic;
using UnityEngine;

public class Pion : ShogiPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        // One in front && Kill Move
        if (currentZ + direction != tileCountZ && currentZ + direction != -1)
            if (board[currentX, currentZ + direction] == null || board[currentX, currentZ + direction].team != team)
                moves.Add(new Vector2Int(currentX, currentZ + direction));

        return moves;
    }


}
