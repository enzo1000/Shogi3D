using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.VisualScripting.Member;

public enum SpecialMove
{
    None = 0,
    Promotion,
    ForcedPromotion,
    Parachutage,
}

public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f; // Default Value
    [SerializeField] private float yOffset = 0;     // Idem
    [SerializeField] private Vector3 boardCenter = Vector3.zero;    // Center of the board set to zero
    [SerializeField] private float deathSize = 0.8f;
    [SerializeField] private float deathSpacing = 0.5f;
    //[SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;  //Prefabs for the simple shogi pieces
    [SerializeField] private GameObject[] PromotionPrefabs;  //Prefabs for the promoted shogi pieces
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
        // If your planning to click on the board or you are just hovering it

        if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight"))) // If our mouse hovering the board
        {
            // Get the index of the tile hitted
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if(currentHover == -Vector2Int.one) // First time hover
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if(currentHover != hitPosition) // If we were already hovering a tile, change the previous one
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");    // Unhover current tile
                currentHover = hitPosition;                                                     // Change tile
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");     // Hover newtile
            }

            if (Input.GetMouseButtonDown(0))    //True when left click (0) pressed, else False
            {
                Debug.Log(info.transform.name); //ICI LA
                Debug.Log(shogiPieces[hitPosition.x, hitPosition.y]); //ICI LA

                if (shogiPieces[hitPosition.x, hitPosition.y] != null) // If we click on a piece
                {
                    //Is it our turn ?
                    if ((shogiPieces[hitPosition.x, hitPosition.y].team == 0 && isRegnantTurn)
                        ||
                        (shogiPieces[hitPosition.x, hitPosition.y].team == 1 && !isRegnantTurn))
                    {
                        currentlyDragging = shogiPieces[hitPosition.x, hitPosition.y];  //Reference copy

                        // Get a list of where I can go (The moves the piece have)
                        availableMoves = currentlyDragging.GetAvailableMoves(ref shogiPieces, TILE_COUNT_X, TILE_COUNT_Z);

                        PreventCheck(currentlyDragging);

                        // Highlight Tiles
                        HighlightTiles(availableMoves, "Highlight");
                    }
                }
            }
            
            if (currentlyDragging != null && Input.GetMouseButtonUp(0)) //If we are releasing the mouse left button
            {
                if (currentlyDragging.gameObject.layer == LayerMask.NameToLayer("Alive")) // If the piece is already on the field
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentZ);
                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    RemoveHighlightTiles(availableMoves);

                    if (!validMove) //If the move is invalide
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    else            //If valid, check promotion
                        StartCoroutine(PromotionCoroutine());
                }
                else if (currentlyDragging.gameObject.layer == LayerMask.NameToLayer("Dead"))// It's a drop from the graveyard
                {
                    DropTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    currentlyDragging = null;
                    RemoveHighlightTiles(availableMoves);
                }
            }
        }
        else if(Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Dead"))) //If we are mouse hovering a dead piece
        {
            if (Input.GetMouseButtonDown(0)) //If we click
            {
                ShogiPiece dropTarget = info.transform.GetComponents<ShogiPiece>()[0];
                if (dropTarget.team == 0) //If we click on the Regnant graveyard
                {
                    if (isRegnantTurn)
                    {
                        availableMoves = dropTarget.isDropable(ref shogiPieces, TILE_COUNT_X, TILE_COUNT_Z);

                        PreventCheck(dropTarget);

                        HighlightTiles(availableMoves, "Highlight");
                        currentlyDragging = dropTarget;
                    }
                }
                else //If we're clicking on the opposant graveyard
                {
                    if (!isRegnantTurn)
                    {
                        availableMoves = dropTarget.isDropable(ref shogiPieces, TILE_COUNT_X, TILE_COUNT_Z);

                        PreventCheck(dropTarget);

                        HighlightTiles(availableMoves, "Highlight");
                        currentlyDragging = dropTarget;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))    //If we releasing our mouse button
            {
                currentlyDragging = null;
                RemoveHighlightTiles(availableMoves);
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
                if (currentlyDragging.gameObject.layer == LayerMask.NameToLayer("Alive"))
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentZ));
                }
                /*else
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentZ));
                }*/
                currentlyDragging = null;               //Ptet faire ça que au release de la souris et pas au down
                RemoveHighlightTiles(availableMoves);
            }
            else if (Input.GetMouseButtonUp(0))    //If we releasing our mouse button
            {
                currentlyDragging = null;
                RemoveHighlightTiles(availableMoves);
            }
        }

        //If we're dragging a piece
        //Make the piece fly and follow the mouse
        /*
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if(horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }       
        */
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
        shogiPieces[0, 0] = SpawnSinglePiece(ShogiPieceType.Lancier, regnant, ShogiPromuType.LancierDor);
        shogiPieces[1, 0] = SpawnSinglePiece(ShogiPieceType.Cavalier, regnant, ShogiPromuType.CavalierDor);
        shogiPieces[2, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, regnant, ShogiPromuType.ArgentDor);
        shogiPieces[3, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDor, regnant);
        shogiPieces[4, 0] = SpawnSinglePiece(ShogiPieceType.Roi, regnant);
        shogiPieces[5, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDor, regnant);
        shogiPieces[6, 0] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, regnant, ShogiPromuType.ArgentDor);
        shogiPieces[7, 0] = SpawnSinglePiece(ShogiPieceType.Cavalier, regnant, ShogiPromuType.CavalierDor);
        shogiPieces[8, 0] = SpawnSinglePiece(ShogiPieceType.Lancier, regnant, ShogiPromuType.LancierDor);

        shogiPieces[1, 1] = SpawnSinglePiece(ShogiPieceType.Tour, regnant, ShogiPromuType.Dragon);
        shogiPieces[7, 1] = SpawnSinglePiece(ShogiPieceType.Fou, regnant, ShogiPromuType.ChevalDragon);

        for (int i = 0; i < TILE_COUNT_X; i++)
            shogiPieces[i, 2] = SpawnSinglePiece(ShogiPieceType.Pion, regnant, ShogiPromuType.PionDor);

        // Opposant (1)
        shogiPieces[0, 8] = SpawnSinglePiece(ShogiPieceType.Lancier, opposant, ShogiPromuType.LancierDor);
        shogiPieces[1, 8] = SpawnSinglePiece(ShogiPieceType.Cavalier, opposant, ShogiPromuType.CavalierDor);
        shogiPieces[2, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, opposant, ShogiPromuType.ArgentDor);
        shogiPieces[3, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDor, opposant);
        shogiPieces[4, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDeJade, opposant);
        shogiPieces[5, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDor, opposant);
        shogiPieces[6, 8] = SpawnSinglePiece(ShogiPieceType.GeneralDargent, opposant, ShogiPromuType.ArgentDor);
        shogiPieces[7, 8] = SpawnSinglePiece(ShogiPieceType.Cavalier, opposant, ShogiPromuType.CavalierDor);
        shogiPieces[8, 8] = SpawnSinglePiece(ShogiPieceType.Lancier, opposant, ShogiPromuType.LancierDor);

        shogiPieces[1, 7] = SpawnSinglePiece(ShogiPieceType.Fou, opposant, ShogiPromuType.ChevalDragon);
        shogiPieces[7, 7] = SpawnSinglePiece(ShogiPieceType.Tour, opposant, ShogiPromuType.Dragon);

        for (int i = 0; i < TILE_COUNT_X; i++)
            shogiPieces[i, 6] = SpawnSinglePiece(ShogiPieceType.Pion, opposant, ShogiPromuType.PionDor);
    }
    private ShogiPiece SpawnSinglePiece(ShogiPieceType type, int team, ShogiPromuType typeP = ShogiPromuType.None)
    {
        // - 1 to align the prefabs pieces with the enum type ShogiPieceType
        // Instantiate cause we using prefabs
        ShogiPiece sp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ShogiPiece>();
        sp.type = type;
        sp.typeP = typeP;
        sp.team = team;

        sp.gameObject.AddComponent<BoxCollider>();
        sp.gameObject.layer = LayerMask.NameToLayer("Alive");

        // TODO : Faire comme dans le tuto est le définir en global
        if (team == 0)
            //Our object position / Our rotation axis / the degree that we want to apply
            sp.transform.RotateAround(transform.position, Vector3.up, 180);

        //sp.GetComponent<MeshRenderer>().material = teamMaterials[team]; // We don't need that for the moment (in case we want to change the pieces color for skin / other thing)

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
    private void HighlightTiles(List<Vector2Int> availableMovesL, string LayerName)
    {
        for(int i = 0; i < availableMovesL.Count; i++)
        {
            tiles[availableMovesL[i].x, availableMovesL[i].y].layer = LayerMask.NameToLayer(LayerName);
        }
    }
    private void RemoveHighlightTiles(List<Vector2Int> availableMovesL)
    {
        for(int i = 0; i < availableMovesL.Count; i++)
        {
            tiles[availableMovesL[i].x, availableMovesL[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMovesL.Clear();
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
    private void PreventCheck(ShogiPiece piece)
    {
        ShogiPiece targetGeneralKing = null;
        //To know were the king is :
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Z; y++)
            {
                if (shogiPieces[x, y] != null)
                {
                    if (piece.team == 0)
                    {
                        if (shogiPieces[x, y].type == ShogiPieceType.Roi)
                        {
                            targetGeneralKing = shogiPieces[x, y];
                        }
                    }
                    else
                    {
                        if (shogiPieces[x, y].type == ShogiPieceType.GeneralDeJade)
                        {
                            targetGeneralKing = shogiPieces[x, y];
                        }
                    }
                }
            }
        }

        //Since we're sending availableMoves, we will deleting moves that are putting us in check
        SimulateMoveForSinglePiece(piece, ref availableMoves, targetGeneralKing);
    }
    private void SimulateMoveForSinglePiece(ShogiPiece sp, ref List<Vector2Int> moves, ShogiPiece targetGeneralKing)
    {
        //https://www.youtube.com/watch?v=rSntP8EC4kY&t=3s
        //Explanation time code - 21:17

        // Save the current values, to reset after the function call
        int actualX = sp.currentX;
        int actualY = sp.currentZ;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // Going through all the moves, simulate them and check if we're in check
        for(int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPositionThisSim = new Vector2Int(targetGeneralKing.currentX, targetGeneralKing.currentZ);
            //Did we simulate the king's move
            if(sp.type == targetGeneralKing.type)
            {
                kingPositionThisSim = new Vector2Int(simX, simY);
            }

            // Copy the [,] and not a reference
            ShogiPiece[,] simulation = new ShogiPiece[TILE_COUNT_X, TILE_COUNT_Z];
            List<ShogiPiece> simAttackingPieces = new List<ShogiPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for (int z = 0; z < TILE_COUNT_Z; z++)
                {
                    if (shogiPieces[x, z] != null)
                    {
                        simulation[x, z] = shogiPieces[x, z];
                        if (simulation[x, z].team != sp.team)
                        {
                            simAttackingPieces.Add(simulation[x, z]);
                        }
                    }
                }
            }

            // Simulate that move
            simulation[actualX, actualY] = null;
            sp.currentX = simX;
            sp.currentZ = simY;
            simulation[simX, simY] = sp;

            // Did one of the piece got taken down during our simulation
            var deadPiece = simAttackingPieces.Find(s => s.currentX == simX && s.currentZ == simY);
            if(deadPiece != null)
            {
                simAttackingPieces.Remove(deadPiece);
            }

            // Get all the simulated attacking pieces moves
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackingPieces.Count; a++)
            {
                var pieceMoves = simAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Z);
                for (int b = 0; b < pieceMoves.Count; b++)
                {
                    simMoves.Add(pieceMoves[b]);
                }
            }

            // Is the king in trouble ? if so, remove the move
            if (ContainsValidMove(ref simMoves, kingPositionThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            // Restore the actual SP data
            sp.currentX = actualX;
            sp.currentZ = actualY;
        }

        // Remove from the current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (shogiPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;
        List<ShogiPiece> deadTargetPieces = (shogiPieces[lastMove[1].x, lastMove[1].y].team == 0) ? deadRegnant : deadOpposant;

        List<ShogiPiece> attackingPieces = new List<ShogiPiece>();
        List<ShogiPiece> defendingPieces = new List<ShogiPiece>();
        List<ShogiPiece> defendingDropablePieces = new List<ShogiPiece>();

        ShogiPiece targetGeneralKing = null;

        //To know were the king is :
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Z; y++)
            {
                if (shogiPieces[x, y] != null)
                {
                    if (shogiPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(shogiPieces[x, y]);
                    }
                    else
                    {
                        attackingPieces.Add(shogiPieces[x, y]);
                    }

                    if (shogiPieces[x, y].team == 0)
                    {
                        if (shogiPieces[x, y].type == ShogiPieceType.Roi)
                        {
                            targetGeneralKing = shogiPieces[x, y];
                        }
                    }
                    else
                    {
                        if (shogiPieces[x, y].type == ShogiPieceType.GeneralDeJade)
                        {
                            targetGeneralKing = shogiPieces[x, y];
                        }
                    }
                }
            }
        }

        //Defending dead pieces
        foreach (ShogiPiece sp in deadTargetPieces)
        {
            defendingDropablePieces.Add(sp);
        }

        //Is the king attacked right now ?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for(int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref shogiPieces, TILE_COUNT_X, TILE_COUNT_Z);
            for (int b = 0; b < pieceMoves.Count; b++)
            {
                currentAvailableMoves.Add(pieceMoves[b]);
            }
        }

        //Are we in check right now ?
        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetGeneralKing.currentX, targetGeneralKing.currentZ)))
        {
            // King is under attack, can we move something to help him ?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref shogiPieces, TILE_COUNT_X, TILE_COUNT_Z);
                SimulateMoveForSinglePiece(defendingPieces[i], ref defendingMoves, targetGeneralKing);

                if (defendingMoves.Count != 0)
                {
                    Debug.Log("On peut défendre le roi en bougeant" + defendingMoves[0].x + " | " + defendingMoves[0].y);
                    return false;
                }
            }
            
            //Idem but, can we drop a piece to help him ?
            for (int j = 0; j < defendingDropablePieces.Count; j++)
            {
                List<Vector2Int> defendingDrops = defendingDropablePieces[j].isDropable(ref shogiPieces, TILE_COUNT_X, TILE_COUNT_Z);
                SimulateMoveForSinglePiece(defendingDropablePieces[j], ref defendingDrops, targetGeneralKing);

                if (defendingDrops.Count != 0)
                {
                    Debug.Log("On peut défendre le roi en se parachutant" + defendingDrops[0].x + " | " + defendingDrops[0].y);
                    return false;
                }
            }
            Debug.Log("Checkmate");
            return true;    //Checkmate exit
        }
        return false;
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

            //Spawn une pièce et on la déplace
            //TODO : Rajouter une 2eme colonne si le nombre de pièce dans le cimetière dépasse un certain nombre
            if (ocp.team == 0)
            {
                if (ocp.type == ShogiPieceType.Roi)
                    CheckMate(1);

                ShogiPiece osp = Instantiate(prefabs[(int)ocp.type - 1], transform).GetComponent<ShogiPiece>();

                Vector3 movement = new Vector3(-1 * tileSize, yOffset, 9 * tileSize)           // Size of the board
                    - bounds                                                   // Its Boundaries
                    + new Vector3(tileSize / 2, 0, tileSize / 2)               // Center of the "tile"
                    + (Vector3.back * deathSpacing) * deadRegnant.Count;       // Because we adding more or less pieces

                osp.SetPosition(movement, true);
                osp.SetScale(Vector3.one * deathSize);

                osp.type = ocp.type;
                osp.typeP = ocp.typeP;
                osp.team = currentlyDragging.team;  //We changing the team here for dropable function purpose 
                osp.isPromoted = false;             //Do we need to do it again here or does it auto do when instantiate ?
                osp.gameObject.AddComponent<BoxCollider>();
                osp.gameObject.layer = LayerMask.NameToLayer("Dead");

                deadRegnant.Add(osp);

                Destroy(shogiPieces[x, z].GameObject());
            }
            else
            {
                if (ocp.type == ShogiPieceType.GeneralDeJade)
                    CheckMate(0);

                //Vector3 position = new Vector3(ocp.transform.position.x, ocp.transform.position.y, ocp.transform.position.z);

                Vector3 movement = new Vector3(9 * tileSize, yOffset, -1 * tileSize)
                                    - bounds
                                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                                    + (Vector3.forward * deathSpacing) * deadOpposant.Count;

                Quaternion rotation = new Quaternion(ocp.transform.rotation.y, ocp.transform.rotation.w, ocp.transform.rotation.x, ocp.transform.rotation.z);

                ShogiPiece osp = Instantiate(prefabs[(int)ocp.type - 1], transform).GetComponent<ShogiPiece>();
                osp.transform.RotateAround(transform.position, Vector3.up, 180);

                osp.SetScale(Vector3.one * deathSize);
                osp.SetPosition(movement, true);

                osp.type = currentlyDragging.type;
                osp.typeP = ocp.typeP;
                osp.team = currentlyDragging.team;
                osp.isPromoted = false; 
                osp.gameObject.AddComponent<BoxCollider>();
                osp.gameObject.layer = LayerMask.NameToLayer("Dead");

                deadOpposant.Add(osp);

                Destroy(shogiPieces[x, z].GameObject());
            }
            //Change ocp.currentX and Z here ?
        }

        shogiPieces[x, z] = cp;
        shogiPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, z);
        moveList.Add(new Vector2Int[] {previousPosition, new Vector2Int(x, z)});

        if (CheckForCheckmate())    //If after our move, we are checkmating the oponent
        {
            CheckMate(cp.team);     //Is that true ?
        }

        return true;
    }
    private void DropTo(ShogiPiece sp, int x, int z)
    {
        if(ContainsValidMove(ref availableMoves, new Vector2Int(x, z))) 
        {
            //print("On drop au bon endroit");
            sp.gameObject.layer = LayerMask.NameToLayer("Alive");       //Change the piece layout ??? (to alive)
            sp.SetPosition(GetTileCenter(x, z));
            sp.currentX = x;
            sp.currentZ = z;
            sp.SetScale(Vector3.one);

            shogiPieces[x, z] = sp;

            List<ShogiPiece> deadCimetery;
            float yOffset;

            if (!isRegnantTurn)
            {
                deadCimetery = deadRegnant;
                yOffset = -deathSpacing;
            }
            else
            {
                deadCimetery = deadOpposant;
                yOffset = deathSpacing;
            }
                        
            int index = deadCimetery.IndexOf(sp);
            //Debug.Log("Son index dans le cimetière est : " + index);

            //On parcour notre tableau à partir de la pièce au dessus de notre pièce
            for (int i = index + 1; i < deadCimetery.Count; i++)
            {
                //Debug.Log("Pièces au dessus : " + deadCimetery[i]);
                deadCimetery[i].SetPosition(deadCimetery[i].transform.position - new Vector3(0, 0, yOffset));
            }

            deadCimetery.Remove(sp);
            isRegnantTurn = !isRegnantTurn;

            // 10 represent the graveyard
            moveList.Add(new Vector2Int[] { new Vector2Int(10, 10), new Vector2Int(x, z) });

            if (CheckForCheckmate())    //If after our drop, we are checkmating the oponent
            {
                CheckMate(sp.team);
            }

        }
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int z = 0; z < TILE_COUNT_Z; z++)
                if (tiles[x, z] == hitInfo)
                    return new Vector2Int(x,z);

        return -Vector2Int.one; //If bug return -1/-1
    }

    //Coroutines
    IEnumerator PromotionCoroutine()
    {
        //this.enabled = false;
        //Récup coordonnées du currentlyDragging ici (cette ligne) afin de réactiver la fonction pour que la pièce suive le curseur
        SpecialMove sm = currentlyDragging.GetIfPromotion(ref shogiPieces, ref moveList);
        int coordX = currentlyDragging.currentX;
        int coordY = currentlyDragging.currentZ;
        while (1 == 1)
        {
            RaycastHit info;
            Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            if (sm == SpecialMove.Promotion)
            {
                List<Vector2Int> promotL = new List<Vector2Int>() { new Vector2Int(coordX, coordY) };
                HighlightTiles(promotL, "Hover");
                if (Input.GetMouseButtonDown(0))    //True when left click (0) pressed, else False
                {
                    if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight"))) // If our mouse hovering the board
                    {
                        Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);
                        if (shogiPieces[hitPosition.x, hitPosition.y] == shogiPieces[coordX, coordY])
                        {
                            //Création piece promu

                            ShogiPiece sp = Instantiate(PromotionPrefabs[(int)currentlyDragging.typeP - 1], transform).GetComponent<ShogiPiece>();

                            sp.type = currentlyDragging.type;
                            sp.typeP = currentlyDragging.typeP;
                            sp.team = currentlyDragging.team;
                            sp.isPromoted = true;
                            sp.gameObject.AddComponent<BoxCollider>();
                            sp.gameObject.layer = LayerMask.NameToLayer("Alive");
                            
                            if (currentlyDragging.team == 0)
                                sp.transform.RotateAround(transform.position, Vector3.up, 180);
                            
                            //Destruction pièce sur le board
                            Destroy(shogiPieces[coordX, coordY].gameObject);

                            shogiPieces[coordX, coordY] = sp;
                            PositionSinglePiece(coordX, coordY, true);

                            RemoveHighlightTiles(promotL);
                            RemoveHighlightTiles(availableMoves);
                        }
                    }
                    RemoveHighlightTiles(promotL);
                    currentlyDragging = null;
                    isRegnantTurn = !isRegnantTurn;
                    StopCoroutine(PromotionCoroutine());
                    break;
                }
            }
            else if (sm == SpecialMove.ForcedPromotion)
            {

                //We can simplifie this with SpawnSinglePiece function
                ShogiPiece sp = Instantiate(PromotionPrefabs[(int)currentlyDragging.typeP - 1], transform).GetComponent<ShogiPiece>();

                sp.type = currentlyDragging.type;
                sp.typeP = ShogiPromuType.None;
                sp.team = currentlyDragging.team;
                sp.gameObject.AddComponent<BoxCollider>();
                sp.gameObject.layer = LayerMask.NameToLayer("Alive");

                if (currentlyDragging.team == 0)
                    sp.transform.RotateAround(transform.position, Vector3.up, 180);

                //Destroy the mesh pieces on the board
                Destroy(shogiPieces[coordX, coordY].gameObject);

                shogiPieces[coordX, coordY] = sp;
                PositionSinglePiece(coordX, coordY, true);
                currentlyDragging = null;
                isRegnantTurn = !isRegnantTurn;
                StopCoroutine(PromotionCoroutine());
                break;
            }
            else
            {
                currentlyDragging = null;
                isRegnantTurn = !isRegnantTurn;
                StopCoroutine(PromotionCoroutine());
                break;
            }
            yield return null;
        }
        yield return 0;
    }
}