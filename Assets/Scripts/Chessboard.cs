using System.Collections.Generic;
using UnityEngine;

public enum SpecialMove 
{
    None = 0,
    Promotion,
    Parachutage,
}

public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f; // Default Value
    [SerializeField] private float yOffset = 0;  // Idem
    [SerializeField] private Vector3 boardCenter = Vector3.zero;    // Center of the board set to zero
    [SerializeField] private float deathSize = 0.8f;
    [SerializeField] private float deathSpacing = 0.5f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;  // If we want to assign different colors or material to each team

    // LOGIC (Things that will be use in the program)
    private ShogiPiece[,] shogiPieces;
    private ShogiPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ShogiPiece> deadRegnant = new List<ShogiPiece>();   //Regnant = 0
    private List<ShogiPiece> deadOpposant = new List<ShogiPiece>();  //Opposant = 1
    private const int TILE_COUNT_X = 9;
    private const int TILE_COUNT_Z = 9;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isRegnantTurn;

    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private SpecialMove specialMove;


    // Awake is called one time at the start of the project
    private void Awake()
    {
        isRegnantTurn = true;   //TODO : Need to code the furigoma
        // Furigoma : Le joueur le plus gradé jette 5 pions, s'il y a plus de pions non promus, il commence sinon c'est l'autre

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Z);
        SpawnAllPieces();
        PositionAllPieces();
    }

    // Every Frames, we are searching if the mouse is hovering the board
    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight"))) // If our mouse hovering the board
        {
            // Get the indexes of the tile hitted
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if(currentHover == -Vector2Int.one) // First time hover
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // If we were already hovering a tile, change the previous one
            if(currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");    // Unhover current tile
                currentHover = hitPosition;                                                     // Change tile
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");     // Hover newtile
            }

            if (Input.GetMouseButtonDown(0))    //True when left click (0) pressed, else False
            {
                if (shogiPieces[hitPosition.x, hitPosition.y] != null) // If we clicking on a piece
                {
                    //Is it our turn ?
                    if ((shogiPieces[hitPosition.x, hitPosition.y].team == 0 && isRegnantTurn)
                        ||
                        (shogiPieces[hitPosition.x, hitPosition.y].team == 1 && !isRegnantTurn))
                    {
                        currentlyDragging = shogiPieces[hitPosition.x, hitPosition.y];  //Reference copy

                        // Get a list of where I can go (The moves the piece have)
                        availableMoves = currentlyDragging.GetAvailableMoves(ref shogiPieces, TILE_COUNT_X, TILE_COUNT_Z);
                        // Get a list of special move
                        specialMove = currentlyDragging.GetSpecialMoves(ref shogiPieces, ref moveList, ref availableMoves);

                        // Highlight Tiles
                        HighlightTiles();
                    }
                }
            }

            //If we are releasing the mouse left button
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                // Thanks to the reference copy, we can get the old position of the piece before dragging it
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentZ);
                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                    currentlyDragging.SetPosition( GetTileCenter(previousPosition.x, previousPosition.y));
                
                currentlyDragging = null;
                RemoveHighlightTiles();
            }

        }
        else // Else : if we are not mouse hovering the board
        {
            if (currentHover != -Vector2Int.one)
            {
                // if (ContainsValidMoves) return true : return false
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if(currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentZ));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        //If we're dragging a piece
        //Make the piece fly and follow the mouse
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if(horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    // Generate all the tiles of our board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountZ)
    {
        //Rajouté sans le tuto, explication en français.
        // Je crois que l'erreur etait induite par le fait que j avais un terrain impair
        // 9x9 donc même si ca devrait pas être le pb. En tout cas, en rajoutant une demi piece
        // de decallage, j arrive a centrer mon terrain en x et z.
        // Sans ce decallage, j obtenais une ligne totale de decallage, cest donc pour cela que jai pensé à un
        // Offset d'une demi piece. Cela recentre les tiles créées sur notre terrain. 
        // (ptet que le tuto n'a pas eu de soucis il avait un bord de 1 case déjà présent sur son terrain ?)

        //Les cases du plateau na restent pas """exactement""" alignées avec le plateau.
        // la val optimal "tileSize" est de 0.77f, il faudra donc re faire le plateau pour tendre vers un 1 plus simple à disposer

        // Finalement, le tuto propose la même solution (dansla partie 2) pour le positionnement des pièces via la méthode GetTileCenter(x,z)

        float xOffset = tileSize / 2;
        float zOffset = tileSize / 2;   
        yOffset += transform.position.y;    // We adding the actual Y position of our object Board to the yOffset

        bounds = new Vector3((tileCountX / 2) * tileSize + xOffset, 0, (tileCountZ / 2) * tileSize + zOffset) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountZ];
        for (int x = 0; x < tileCountX; x++)
            for (int z = 0; z < tileCountZ; z++)
                tiles[x, z] = GenerateSingleTile(tileSize, x, z);
    }
    // To create each tile of our board
    private GameObject GenerateSingleTile(float tileSize, int x, int z)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Z:{1}", x, z));    // How it will be printed
        tileObject.transform.parent = transform;    // Our tile receive

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, z * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (z+1) * tileSize) - bounds;
        vertices[2] = new Vector3((x+1) * tileSize, yOffset, z * tileSize) - bounds;
        vertices[3] = new Vector3((x+1) * tileSize, yOffset, (z+1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();  // To fix lights problems

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }
    
    // Spawning of the pieces
    private void SpawnAllPieces()
    {
        shogiPieces = new ShogiPiece[TILE_COUNT_X, TILE_COUNT_Z];

        int regnant = 0, opposant = 1;

        // Regnant (0)
        shogiPieces[0, 0] = SpawnSinglePiece(ShogiPieceType.Lancier, regnant);
        shogiPieces[1, 0] = SpawnSinglePiece(ShogiPieceType.Cavalier, regnant);
        shogiPieces[2, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, regnant);
        shogiPieces[3, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDor, regnant);
        shogiPieces[4, 0] = SpawnSinglePiece(ShogiPieceType.Roi, regnant);
        shogiPieces[5, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDor, regnant);
        shogiPieces[6, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, regnant);
        shogiPieces[7, 0] = SpawnSinglePiece(ShogiPieceType.Cavalier, regnant);
        shogiPieces[8, 0] = SpawnSinglePiece(ShogiPieceType.Lancier, regnant);

        shogiPieces[1, 1] = SpawnSinglePiece(ShogiPieceType.Tour, regnant);
        shogiPieces[7, 1] = SpawnSinglePiece(ShogiPieceType.Fou, regnant);

        for (int i = 0; i < TILE_COUNT_X; i++)
            shogiPieces[i, 2] = SpawnSinglePiece(ShogiPieceType.Pion, regnant);

        // Opposant (1)
        shogiPieces[0, 8] = SpawnSinglePiece(ShogiPieceType.Lancier, opposant);
        shogiPieces[1, 8] = SpawnSinglePiece(ShogiPieceType.Cavalier, opposant);
        shogiPieces[2, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, opposant);
        shogiPieces[3, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDor, opposant);
        shogiPieces[4, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDeJade, opposant);
        shogiPieces[5, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDor, opposant);
        shogiPieces[6, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, opposant);
        shogiPieces[7, 8] = SpawnSinglePiece(ShogiPieceType.Cavalier, opposant);
        shogiPieces[8, 8] = SpawnSinglePiece(ShogiPieceType.Lancier, opposant);

        shogiPieces[1, 7] = SpawnSinglePiece(ShogiPieceType.Fou, opposant);
        shogiPieces[7, 7] = SpawnSinglePiece(ShogiPieceType.Tour, opposant);

        for (int i = 0; i < TILE_COUNT_X; i++)
            shogiPieces[i, 6] = SpawnSinglePiece(ShogiPieceType.Pion, opposant);
    }
    private ShogiPiece SpawnSinglePiece(ShogiPieceType type, int team)
    {
        // - 1 to align the prefabs pieces with the enum type ShogiPieceType
        // Instantiate cause we using prefabs
        ShogiPiece sp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ShogiPiece>();
        sp.type = type;
        sp.team = team;
        if (team == 0)
            //Our object position / Our rotation axis / the degree that we want to apply
            sp.transform.RotateAround(transform.position, Vector3.up, 180);

        //sp.GetComponent<MeshRenderer>().material = teamMaterials[team]; // We don't need that for the moment (in case we want to change color of pieces for skin / other thing)

        return sp;
    }
    
    // Positionning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int z = 0; z < TILE_COUNT_Z; z++)
                if (shogiPieces[x, z] != null)
                    PositionSinglePiece(x, z, true);
            
    }
    // Force is the option to teleport (true) the piece when it spawn
    //  or moove it smoothly (false) to it position (by default it set to false)
    // For exemple, if we make a moove, we will set it to false, when we 
    //  spawn it, we will not need the smooth option
    private void PositionSinglePiece(int x, int z, bool force = false)
    {
        shogiPieces[x, z].currentX = x;
        shogiPieces[x, z].currentZ = z;
        shogiPieces[x, z].SetPosition(GetTileCenter(x, z), force);
    }
    private Vector3 GetTileCenter(int x, int z)
    {
        return new Vector3(x * tileSize, yOffset, z * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    // Highlight Tiles
    private void HighlightTiles()
    {
        for(int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    private void RemoveHighlightTiles()
    {
        for(int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    // Checkmate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnResetButton()
    {
        // UI
        // We are accessing child value in theyre order by the hierarchy menu
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        // Field Reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();

        // Clean up
        //  Board
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            for (int j = 0; j < TILE_COUNT_Z; j++)
            {
                if (shogiPieces[i,j] != null)
                {
                    Destroy(shogiPieces[i, j].gameObject);
                    shogiPieces[i, j] = null;
                }
            }
        }

        //  Cimetery
        for (int x = 0; x < deadOpposant.Count; x++)
            Destroy(deadOpposant[x].gameObject);

        for (int x = 0; x < deadRegnant.Count; x++)
            Destroy(deadRegnant[x].gameObject);

        deadOpposant.Clear();
        deadRegnant.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isRegnantTurn = true;
    }
    public void OnExitButton()
    {
        Application.Quit();
    }

    // Special Moves
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.Promotion)
        {

        }
    }

    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        return false;
    }
    private bool MoveTo(ShogiPiece cp, int x, int z)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2Int(x, z)))
            return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentZ);

        // Is there another piece on the target position ?
        if (shogiPieces[x, z] != null)
        {
            ShogiPiece ocp = shogiPieces[x, z];
            if (ocp.team == cp.team)
                return false;

            // If it's the enemy team
            //TODO : Mettre en place un système de colonne de tel manière à ce que les pièces soit visible
            // et ne dépace pas de l'écran
            if(ocp.team == 0)
            {
                if (ocp.type == ShogiPieceType.Roi)
                    CheckMate(1);

                deadRegnant.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(-1 * tileSize, yOffset, 9 * tileSize)   //Size of the board
                    - bounds                                            // Bounds of it
                    + new Vector3(tileSize / 2, 0, tileSize / 2)        // Center of the "tile"
                    + (Vector3.back * deathSpacing) * deadRegnant.Count);       //Because we adding more or less pieces
            }
            else
            {
                if (ocp.type == ShogiPieceType.GeneralDeJade)
                    CheckMate(0);

                deadOpposant.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(9 * tileSize, yOffset, -1 * tileSize)   //Size of the board
                    - bounds                                            // Bounds of it
                    + new Vector3(tileSize / 2, 0, tileSize / 2)        // Center of the "tile"
                    + (Vector3.forward * deathSpacing) * deadOpposant.Count);       //Because we adding more or less pieces
            }
        }

        shogiPieces[x, z] = cp;
        shogiPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, z);
        isRegnantTurn = !isRegnantTurn;
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, z) });

        ProcessSpecialMove();

        return true;
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int z = 0; z < TILE_COUNT_Z; z++)
                if (tiles[x, z] == hitInfo)
                    return new Vector2Int(x,z);

        return -Vector2Int.one; //If bug return -1/-1
    }
}
