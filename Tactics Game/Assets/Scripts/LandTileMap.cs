using System.Collections.Generic;
using System;
using UnityEngine;

public class LandTileMap : MonoBehaviour {

	// LandTileMap holds a grid of all the tiles in a map, and functions to get its neighboring tiles.

	private static LandTileMap _instance = null;              //Static instance of LandTileMap which allows it to be accessed by any other script.
	public static LandTileMap instance { get { return _instance; } }

	// Components
	private Pathfinding pathfinder;
	private GridCursor gridCursor;

	public float minimumTileHeight = 1f;
	public float maximumTileHeight = 2f;
	private float tileSize = 1f;						// The X and Z size of all tiles on the map (Do not change run-time!)
	public int mapSizeX;								// The amount of Tiles on the X-axis
	public int mapSizeY;								// The amount of Tiles on the Y-axis
	public int totalSize {
		get { return mapSizeX * mapSizeY; }
	}
	public Sprite randomSprite;                         // !Temporary! Sprite applied to new Tiles; Replace with tile-type based sprite

#pragma warning disable CS0649
	[SerializeField] private GameObject TileMapContainer;     // The GameObject new Tiles will be parented to; reason: position, and 'this' is DontDestroyOnLoad
	[SerializeField] private GameObject tilePrefab;           // The prefab for blank, unset Tiles
#pragma warning restore CS0649

	private LandTile [,] mapGrid;                       // Grid containing all tiles based on X and Y positions of the map
	private LandTile [] highlightedTiles;               // Collection of the tiles that are highlighted (generally as part of movement or attack distance indicators).


	private void Awake () {
		if (_instance == null)
			_instance = this;
		else if (_instance != this)
			Destroy (this.gameObject);
		DontDestroyOnLoad (this.gameObject);

		pathfinder = new Pathfinding (this);
		gridCursor = GetComponentInChildren<GridCursor> ();

		if (TileMapContainer == null)
			TileMapContainer = new GameObject ();
		TileMapContainer.transform.position = Vector3.zero;     // The tiles are based on LandTileMap's position; so making sure it's 0.
		CreateGrid ();											// !Temporary! creates the grid (currently semi-random grid)
	}

	private void Update () {
		if (highlightedTiles == null)
			return;
		
		// If there are tiles being highlighted; adjust their transparency over time (blinking effect)
		float newAlpha = (Mathf.Sin (Time.time * 2) / 4) + 0.75f;
		foreach (LandTile tile in highlightedTiles) {
			tile.BlinkSelectionColor (newAlpha);
		}
	}

	// Sets the reference to the unitManager in the gridcursor
	//@todo find better way of making this link (or avoid it).
	public void SetupGridCursor (UnitManager unitManager) {
		gridCursor.SetupGridCursor (unitManager);
	}


	public void CreateGrid () {
		if (mapGrid != null) {
			foreach (LandTile tile in mapGrid) {
				GameObject.Destroy (tile.gameObject);
			}
		}
		mapGrid = new LandTile [mapSizeX, mapSizeY];

		CreateRandomMap ();
	}

	public LandTile GetFreeTile (LandTile tile) {
		List<LandTile> possibleTiles = GetTileNeighbours (tile);
		foreach (LandTile tryTile in possibleTiles) {
			if (tryTile.isPathable && !tryTile.unitOnTile) {
				return tryTile;
			}
		}
		LandTile newTile = null;

		while (newTile == null) {
			LandTile t = GetLandTile (UnityEngine.Random.Range (0, mapSizeX), UnityEngine.Random.Range (0, mapSizeY));
			if (t.isPathable && !t.unitOnTile) {
				newTile = t;
				break;
			}
		}

		return newTile;
	}

#region  //------ Tile Data Get Functions ----------------------------------------------------------------------------------------------
	///<summary>
	/// Returns a landTile based on their x/y coördinates on the gridmap.
	///</summary>
	public LandTile GetLandTile (int x, int y) {
		return mapGrid [x, y];
	}

	///<summary>
	/// Returns a LandTile based on given worldPosition (getting this by x/y position is prefered).
	///</summary>
	public LandTile GetTileByWorldPoint (Vector3 worldPosition) {
	float percentX = worldPosition.x / ((mapSizeX - 1) * tileSize);
		float percentY = worldPosition.z / ((mapSizeY - 1) * tileSize);

		percentX = Mathf.Clamp01 (percentX);
		percentY = Mathf.Clamp01 (percentY);

		int x = Mathf.RoundToInt ((mapSizeX - 1) * percentX);
		int y = Mathf.RoundToInt ((mapSizeY - 1) * percentY);

		return mapGrid [x, y];
	}

	///<summary>
	/// Returns the distance (in steps) between the two given tiles
	///</summary>
	public int GetTileDistance (LandTile tile1, LandTile tile2) {
		int dstX = Mathf.Abs (tile1.positionX - tile2.positionX);
		int dstY = Mathf.Abs (tile1.positionY - tile2.positionY);

		return dstX + dstY;
	}

	///<summary>
	/// Checks if the given tile contains a character
	///</summary>
	public bool DoesTileContainUnit (int x, int y) {
		LandTile tile = GetLandTile (x, y);
		return tile.unitOnTile;
	}
	public bool DoesTileContainUnit (LandTile tile) {
		return tile.unitOnTile;
	}

	///<summary>
	/// Checks if the given tile is highlighted for selection
	///</summary>
	public bool IsTileHighlighted (int x, int y) {
		LandTile tile = GetLandTile (x, y);
		int p = Array.IndexOf<LandTile> (highlightedTiles, tile);
		return (p > -1);
	}
	public bool IsTileHighlighted (LandTile tile) {
		int p = Array.IndexOf<LandTile> (highlightedTiles, tile);
		return (p > -1);
	}
	///<summary>
	/// Gets the neighbouring tiles, including diagonally.
	///</summary>
	public List<LandTile> GetTileNeighbours (LandTile tile) {
		List<LandTile> neighbours = new List<LandTile> ();
		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0) {
					continue;
				}
				int checkX = tile.positionX + x;
				int checkY = tile.positionY + y;

				if (checkX >= 0 && checkX <mapSizeX && checkY >= 0 && checkY < mapSizeY) {
					neighbours.Add (mapGrid[checkX, checkY]);
				}
			}
		}
		return neighbours;
	}

	///<summary>
	/// Gets the neighbouring tiles, excluding diagonally.
	///</summary>
	public List<LandTile> GetDirectTileNeighbours (LandTile tile) {
	List<LandTile> neighbours = new List<LandTile> ();
		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if ((x!=0 && y !=0) || (x == 0 && y == 0)) {
					continue;
				}
				int checkX = tile.positionX + x;
				int checkY = tile.positionY + y;

				if (checkX >= 0 && checkX < mapSizeX && checkY >= 0 && checkY < mapSizeY) {
					neighbours.Add (mapGrid [checkX, checkY]);
				}
			}
		}
		return neighbours;
	}
#endregion

#region  //------ Tile Unit-Placement Functions ----------------------------------------------------------------------------------------------
	///<summary>
	/// Assigns a unit on to tile (doesn't change any other tiles)
	///</summary>
	public void SetUnitOnTile (Unit unit, LandTile tile) {
		tile.unitOnTile = unit;
		unit.currentTile = tile;
	}
	public void SetUnitOnTile (Unit unit, int x, int y) {
		LandTile tile = GetLandTile (x, y);
		tile.unitOnTile = unit;
		unit.currentTile = tile;
	}
	public void SetUnitOnTile (Unit unit, Vector3 tilePosition) {
		LandTile tile = GetTileByWorldPoint (tilePosition);
		tile.unitOnTile = unit;
		unit.currentTile = tile;
	}
	///<summary>
	/// Assigns a unit on to tile, while empying the tile the unit is currently on (by world point)
	///</summary>
	public void MoveUnitToTile (Unit unit, LandTile newTile) {
		unit.currentTile.unitOnTile = null;
		newTile.unitOnTile = unit;
		unit.currentTile = newTile;
	}
	public void MoveUnitToTile (Unit unit, int x, int y) {
		unit.currentTile.unitOnTile = null;
		LandTile tile = GetLandTile (x, y);
		tile.unitOnTile = unit;
		unit.currentTile = tile;
	}
	public void MoveUnitToTile (Unit unit, Vector3 newPosition) {
		unit.currentTile.unitOnTile = null;
		LandTile tile = GetTileByWorldPoint (newPosition);
		tile.unitOnTile = unit;
		unit.currentTile = tile;
	}
	///<summary>
	/// Empties out the tile the unit is currently located on (by specific tile, world point, or x/y coördinates).
	///</summary>
	public void RemoveUnitOnTile (Unit unit) {
		GetTileByWorldPoint (unit.transform.position).unitOnTile = null;
	}
	public void RemoveUnitOnTile (int x, int y) {
		GetLandTile (x, y).unitOnTile = null;
	}
	public void RemoveUnitOnTile (LandTile tile) {
		tile.unitOnTile = null;
	}
#endregion

#region  //------ Pathfinding Call Functions ----------------------------------------------------------------------------------------------
	///<summary>
	/// Begins pathfinding from point A to B, and calls the given callback function.
	///</summary>
	public LandTile[] RequestPath (Vector3 pathStart, Vector3 pathEnd, bool canMoveDiagonally, bool moveThroughUnits) {
		LandTile startTile = GetTileByWorldPoint (pathStart);
		LandTile endTile = GetTileByWorldPoint (pathEnd);
		return pathfinder.FindPath (startTile, endTile, canMoveDiagonally, moveThroughUnits);
	}
	public LandTile [] RequestPath (LandTile startTile, LandTile endTile, bool canMoveDiagonally, bool moveThroughUnits) {
		return pathfinder.FindPath (startTile, endTile, canMoveDiagonally, moveThroughUnits);
	}
	///<summary>
	/// Returns tiles within the given range of the startPosition, and calls the given callback function.
	///</summary>
	public LandTile[] RequestRange (Vector3 startPosition, int distance, bool canGoDiagonally, bool includeMovementCost, bool canTargetStartPosition, bool canTargetUnitPositions) {
		LandTile startTile = GetTileByWorldPoint (startPosition);
		return pathfinder.FindRange (startTile, distance, canGoDiagonally, includeMovementCost, canTargetStartPosition, canTargetUnitPositions);
	}
	///<summary>
	/// Returns units within the given range of the startPosition, and calls the given callback function.
	///</summary>
	public Unit[] RequestUnits (Vector3 startPosition, int distance) {
		LandTile startTile = GetTileByWorldPoint (startPosition);
		return pathfinder.FindUnitsInRage (startTile, distance);
	}
#endregion

	///<summary>
	/// Highlights the color indicators of the given tiles with the given color
	///</summary>
	public void ActivateTileIndicators (LandTile[] tilesToIndicate, Color indicationColor) {
		if (highlightedTiles != null) {		// Makes sure there are no currently active indicators, else they'd become "forgotten" and stay highlighted
			DeactivateTileIndicators ();
		}

		highlightedTiles = tilesToIndicate;

		foreach (LandTile tile in highlightedTiles) {
			tile.SetSelectionColor (indicationColor);
		}
	}
	///<summary>
	/// Deactivates the color indicators of all tiles
	///</summary>
	public void DeactivateTileIndicators () {
		if (highlightedTiles == null)
			return;

		foreach (LandTile tile in highlightedTiles) {
			tile.HideSelectionColor ();
		}
		highlightedTiles = null;
		// Ook de gridcursor deactivaten? Is er een situatie waarin dit niet nodig is?
	}

	///<summary>
	/// Activates the Grid Cursor
	///</summary>
	public void ActivateGridCursor (Vector3 startPosition, Action<Transform> OnConfirmationEvent, Action<bool> OnCancelationEvent) {
	GameManager.instance.SetModeInputCursor ();
		gridCursor.gameObject.SetActive (true);

		LandTile tile = GetTileByWorldPoint (startPosition);
		gridCursor.SetupSelectionCursor (tile.positionX, tile.positionY, OnConfirmationEvent, OnCancelationEvent);
		CameraManager.instance.FollowObject (gridCursor.transform);
	}
	///<summary>
	/// Deactivates the Grid Cursor
	///</summary>
	public void DisableGridCursor () {
		CameraManager.instance.StopFollowingObject ();
		gridCursor.gameObject.SetActive (false);
	}

	// Spawns tiles semi-randomly.
	private void CreateRandomMap () {
		for (int y = 0; y < mapSizeY; y++) {
			for (int x = 0; x < mapSizeX; x++) {
				float tileHeight = UnityEngine.Random.Range (minimumTileHeight, maximumTileHeight);
				Vector3 spawnPosition = gameObject.transform.position + new Vector3 (tileSize * x, (tileHeight / 2) - 0.5f, tileSize * y);

				GameObject newTile = Instantiate (tilePrefab, spawnPosition, Quaternion.identity, TileMapContainer.transform);
				newTile.transform.localScale = new Vector3 (tileSize, tileHeight, tileSize);
				newTile.gameObject.name = "Tile[" + x + "," + y + "]";

				LandTile tileScript = newTile.GetComponent<LandTile> ();
				bool pathable = (UnityEngine.Random.Range (0, 4) == 0) ? false : true;

				int movementCost = 0;
				if (pathable) {
					//movementCost = UnityEngine.Random.Range (1, 2);		// Remember, this is added to the 'normal' movement; so +0 for normal terrain
					movementCost = 1;
					if (movementCost == 1) {
						newTile.gameObject.GetComponent<Renderer> ().material.color = Color.green;
					}
					// Get Tile type for movementCost of the tile
				} else {
					newTile.gameObject.GetComponent<Renderer> ().material.color = Color.red;
				}

				tileScript.SetTile (tileHeight, randomSprite, LandType.Gras, x, y, pathable, movementCost);
				mapGrid [x, y] = tileScript;
			}
		}
	}

	private void OnDrawGizmos () {
		// Draws a white cube around the tilemap, to indicate its size in the editor
		Gizmos.DrawWireCube (transform.position + new Vector3 ((mapSizeX / 2) - (tileSize / 2), 0, (mapSizeY / 2) - (tileSize / 2)), new Vector3 (mapSizeX, 1, mapSizeY));

		// Draws red/white cubes around each tile to indicate which tile is pathable and which isn't.
		if (!DebugSettings.debugTileMap)
			return;
		
		if (mapGrid != null) {
			foreach (LandTile tile in mapGrid) {
				Gizmos.color = (!tile.isPathable) ? Color.red : Color.white;
				Gizmos.DrawWireCube (tile.gameObject.transform.position, new Vector3 (tileSize, tile.tileHeight, tileSize));
			}
		}
	}

}
