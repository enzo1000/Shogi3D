using System.Collections.Generic;
using UnityEngine;

public class GeneralDargent : ShogiPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        for (int rows = currentX - 1; rows <= currentX + 1; rows++)
            for (int cols = currentZ - 1; cols <= currentZ + 1; cols++)
                if (rows >= 0 && rows < tileCountX && cols >= 0 && cols < tileCountZ)
                    if (board[rows, cols] == null || board[rows, cols].team != team)
                        moves.Add(new Vector2Int(rows, cols));

        if (moves.Contains(new Vector2Int(currentX, currentZ - direction)))
            moves.Remove(new Vector2Int(currentX, currentZ - direction));

        if (moves.Contains(new Vector2Int(currentX + 1, currentZ)))
            moves.Remove(new Vector2Int(currentX + 1, currentZ));
        
        if (moves.Contains(new Vector2Int(currentX - 1, currentZ)))
            moves.Remove(new Vector2Int(currentX - 1, currentZ));

        return moves;
    }
    public override SpecialMove GetIfPromotion(ref ShogiPiece[,] board, ref List<Vector2Int[]> moveList)
    {
        if (team == 0)
        {
            // If our last move was in the promotion zone (moving out or in) or if our new move is in the promotion zone (moving in)
            if (moveList[^1][0].y >= 6 || moveList[^1][1].y >= 6)
                return SpecialMove.Promotion;
        }
        else
        {
            if (moveList[^1][0].y <= 2 || moveList[^1][1].y <= 2)
                return SpecialMove.Promotion;
        }
        return SpecialMove.None;
    }
}
