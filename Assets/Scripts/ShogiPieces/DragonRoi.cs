using System.Collections.Generic;
using UnityEngine;

public class DragonRoi : ShogiPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // Down
        for (int i = currentZ - 1; i >= 0; i--)
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
        // Up
        for (int i = currentZ + 1; i < tileCountZ; i++)
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
        // Left
        for (int i = currentX - 1; i >= 0; i--)
        {
            if (board[i, currentZ] == null)
            {
                moves.Add(new Vector2Int(i, currentZ));
            }
            else if (board[i, currentZ].team != team)
            {
                moves.Add(new Vector2Int(i, currentZ));
                break;
            }
            else
                break;
        }
        // Right
        for (int i = currentX + 1; i < tileCountX; i++)
        {
            if (board[i, currentZ] == null)
            {
                moves.Add(new Vector2Int(i, currentZ));
            }
            else if (board[i, currentZ].team != team)
            {
                moves.Add(new Vector2Int(i, currentZ));
                break;
            }
            else
                break;
        }

        for (int rows = currentX - 1; rows <= currentX + 1; rows++)
            for (int cols = currentZ - 1; cols <= currentZ + 1; cols++)
                if (rows >= 0 && rows < tileCountX && cols >= 0 && cols < tileCountZ)
                    if (board[rows, cols] == null || board[rows, cols].team != team)
                        moves.Add(new Vector2Int(rows, cols));

        return moves;
    }
}