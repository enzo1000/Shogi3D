using System.Collections.Generic;
using UnityEngine;

public enum ShogiPieceType
{
    None = 0,
    Pion = 1,
    Tour = 2,
    Fou = 3,
    Lancier = 4,
    Cavalier = 5,
    GeneralDargent = 6,
    GeneralDor = 7,
    GeneralDeJade = 8,
    Roi = 9,
}

public class ShogiPiece : MonoBehaviour
{
    public int team;    //Reignant = 0 / Opposant = 1
    public int currentX;
    public int currentZ;
    public ShogiPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ShogiPiece[,] board, int tileCountX, int tileCountZ)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(4, 4));
        r.Add(new Vector2Int(4, 5));
        r.Add(new Vector2Int(5, 4));
        r.Add(new Vector2Int(5, 5));

        return r;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
}
