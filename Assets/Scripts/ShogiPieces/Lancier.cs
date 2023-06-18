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
    public override SpecialMove GetIfPromotion(ref ShogiPiece[,] board, ref List<Vector2Int[]> moveList)
    {
        if (team == 0)
        {
            // If our last move was in the promotion zone (moving out or in) or if our new move is in the promotion zone (moving in)
            if (moveList[^1][1].y >= 6)
            {
                if (moveList[^1][1].y == 8)
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
                if (moveList[^1][1].y == 0)
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

        // Need to know if the lancier is from the regnant or the opponent to know if it is dropable on the last line of the board

        // lastLine = true if Regnant / false if opposant
        bool lastLine = (team == 1) ? true : false;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int z = 0; z < TILE_COUNT_Z; z++)
            {
                if (board[x, z] == null)
                {
                    if (lastLine)
                    {
                        if (z != 0)
                        {
                            dropList.Add(new Vector2Int(x, z));
                        }
                    }
                    else
                    {
                        if (z != 8)
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
