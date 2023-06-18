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
    public override SpecialMove GetIfPromotion(ref ShogiPiece[,] board, ref List<Vector2Int[]> moveList)
    {
        if (team == 0)
        {
            // If our last move was in the promotion zone (moving out or in) or if our new move is in the promotion zone (moving in)
            if (moveList[^1][1].y >= 6)
            {
                if (moveList[^1][1].y > 6)
                {
                    return SpecialMove.ForcedPromotion;
                }
                return SpecialMove.Promotion;
            }
        }
        else
        {
            if (moveList[^1][1].y <= 2)
            {
                if (moveList[^1][1].y < 2)
                {
                    return SpecialMove.ForcedPromotion;
                }
                return SpecialMove.Promotion;
            }                
        }
        return SpecialMove.None;
    }
    public override List<Vector2Int> isDropable(ref ShogiPiece[,] board, int TILE_COUNT_X, int TILE_COUNT_Z)
    {
        List<Vector2Int> dropList = new List<Vector2Int>();

        // lastLine = true if Regnant / false if opposant
        bool lastLine = (team == 0) ? true : false;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int z = 0; z < TILE_COUNT_Z; z++)
            {
                if (board[x, z] == null)
                {
                    if (lastLine)
                    {
                        if (z <= 6)
                        {
                            dropList.Add(new Vector2Int(x, z));
                        }
                    }
                    else
                    {
                        if (z >= 2)
                        {
                            dropList.Add(new Vector2Int(x, z));
                        }  
                    }   
                }   
            }
        }
        return dropList;
    }
}
