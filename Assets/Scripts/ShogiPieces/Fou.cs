using System.Collections.Generic;
using UnityEngine;

public class Fou : ShogiPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // /
        for (int x = currentX + 1, z = currentZ + 1; x < tileCountX && z < tileCountZ; x++, z++)
        {
            if (board[x, z] == null)
            {
                moves.Add(new Vector2Int(x, z));
            }
            else if (board[x, z].team != team)
            {
                moves.Add(new Vector2Int(x, z));
                break;
            }
            else
                break;
        }
        // \
        for (int x = currentX - 1, z = currentZ + 1; x >= 0 && z < tileCountZ; x--, z++)
        {
            if (board[x, z] == null)
            {
                moves.Add(new Vector2Int(x, z));
            }
            else if (board[x, z].team != team)
            {
                moves.Add(new Vector2Int(x, z));
                break;
            }
            else
                break;
        }
        // /
        for (int x = currentX - 1, z = currentZ - 1; x >= 0 && z >= 0; x--, z--)
        {
            if (board[x, z] == null)
            {
                moves.Add(new Vector2Int(x, z));
            }
            else if (board[x, z].team != team)
            {
                moves.Add(new Vector2Int(x, z));
                break;
            }
            else
                break;
        }
        // \
        for (int x = currentX + 1, z = currentZ - 1; x < tileCountX && z >= 0; x++, z--)
        {
            if (board[x, z] == null)
            {
                moves.Add(new Vector2Int(x, z));
            }
            else if (board[x, z].team != team)
            {
                moves.Add(new Vector2Int(x, z));
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
