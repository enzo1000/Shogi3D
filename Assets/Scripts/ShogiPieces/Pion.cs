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
    public override SpecialMove GetIfPromotion(ref ShogiPiece[,] board, ref List<Vector2Int[]> moveList)
    {
        // ^1 == the last element of (in a tab)
        // 2..4 == the elements from index 2 to 4 (in a tab)
        // ^2.. the last two elements with ^2 meaning, the seconds last and .. the others elements "in front of"
        // So moveList.Count - 1 == ^1
        if (team == 0)
        {
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
        //TODO: This method seems to be right but need to be tested
        // We can simplify it complexity from current n^3 to n^2 (using while would be simpler)

        //Do specials things for the pawn
        // If put in front of the king to mate (unallowed)
        // If it is on the same column of one of the other same team pawn

        int direction = (team == 0) ? 1 : -1;
        bool lastLine = (team == 1) ? true : false;
        List<Vector2Int> dropList = new List<Vector2Int>();
        bool canBeDrop;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int z = 0; z < TILE_COUNT_Z; z++)
            {
                canBeDrop = true;
                //Column checking
                for (int z2 = 0; z2 < TILE_COUNT_Z; z2++)
                {
                    if (board[x, z2] != null)
                    {

                        Debug.Log(board[x, z2] + " ne gène pas");
                        if (board[x, z2].team == team && board[x, z2].type == type && board[x, z2].isPromoted == false)
                        {
                            Debug.Log(board[x, z2] + " nous empeche de poser");
                            canBeDrop = false;
                        }
                    }
                }

                //King checking
                if (z + direction < TILE_COUNT_Z && z + direction >= 0)
                {
                    if (board[x, z + direction] != null)
                    {
                        if (board[x, z + direction].team != team && (board[x, z + direction].type == ShogiPieceType.GeneralDeJade || board[x, z + direction].type == ShogiPieceType.Roi))
                        {
                            canBeDrop = false;
                        }
                    }  
                }
                //Last line checking
                if (canBeDrop)
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
        }
        return dropList;
    }
}
