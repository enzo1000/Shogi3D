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
                    return SpecialMove.ForcedPromotion;
                return SpecialMove.Promotion;
            }
            return SpecialMove.None;
        }
        else
        {
            if (moveList[^1][1].y <= 2)
            {
                if (moveList[^1][1].y == 0)
                    return SpecialMove.ForcedPromotion;
                return SpecialMove.Promotion;
            }
            return SpecialMove.None;
        }
    }
    public override List<Vector2Int> isDropable(ref ShogiPiece[,] board, int TILE_COUNT_X, int TILE_COUNT_Z)
    {
        //TODO: This method seems to be right but need to be tested
        // We can simplify it complexity from current n^3 to n^2 (using while would be simpler)

        //Do specials things for the pawn
        // If put in front of the king to mate (unallowed)
        // If it is on the same column of one of the other same team pawn

        //Marche pas, rend 3 au lieu de 6 
        // Le pion ne peut pas être drop sur la last ligne du tableau (à ajouter)

        int direction = (team == 0) ? 1 : -1;
        bool lastLine = (team == 0) ? true : false;
        List<Vector2Int> dropList = new List<Vector2Int>();
        bool canBeDrop;

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int z = 0; z < TILE_COUNT_Z; z++)
            {
                canBeDrop = true;
                //Colomn checking
                for (int z2 = 0; z2 < TILE_COUNT_Z; z2++)
                    if (board[x, z2] != null)
                        if (board[x, z2].team != team && board[x, z2].type == type) //Quick reminder that the piece in the graveyard is an opponent piece
                            canBeDrop = false;

                //King checking
                if (x + direction < TILE_COUNT_X && x + direction >= 0)
                    if (board[x + direction, z] != null)
                        if (board[x + direction, z].team == team && board[x + direction, z].type == ShogiPieceType.Roi) //Same here
                            canBeDrop = false;

                if (canBeDrop)
                {
                    if (board[x, z] == null)
                    {
                        if (lastLine)
                        {
                            if (z != 0)
                                dropList.Add(new Vector2Int(x, z));
                        }
                        else
                        {
                            if (z != 8)
                                dropList.Add(new Vector2Int(x, z));
                        }
                    }
                }
            }
        return dropList;
    }
}
