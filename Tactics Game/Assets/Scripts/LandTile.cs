using UnityEngine;

public class LandTile : MonoBehaviour, IHeapItem<LandTile> {
	
	// Landtile is one tile on the tilemap; it contains all its data, as well as a selectionplane (coloured for movement/attack indicators).
	// Additionally it holds data to calculate pathfinding costs

	// Gameplay Information
	public Sprite tileSprite;							// The Sprite used for the tile
	public LandType landType;                           // The 'type' of land of the tile (gras, dirt, snow, etc)
	public float tileHeight;                            // The height of the tile (currently equal to the y-scale)

	public bool isPathable;                             // Wether or not units can move over this tile
	public int movementCost;                            // The amount of >extra< movement it requires to move over this tile (default is 0)
	public Unit unitOnTile;                      // The Unit that's located on the tile

	// Components
	private Renderer selectionPlane;					// The Selection plane used to indicate movement/attack ranges


	public void SetTile (float height, Sprite sprite, LandType type, int xPos, int yPos, bool walkable, int mCost) {
		tileHeight = height;
		tileSprite = sprite;
		landType = type;
		positionX = xPos;
		positionY = yPos;
		isPathable = walkable;
		movementCost = mCost;

		selectionPlane = this.transform.Find ("SelectionPlane").GetComponent<Renderer> ();
#if UNITY_EDITOR
		if (selectionPlane == null) {
			Debug.LogError ("Tile didn't find a selectionPlane; all Tiles need a child component named specifically 'SelectionPlane'.");
		}
#endif
		if (!isPathable) {
			SetSelectionColor (Color.red);
		} else {
			SetSelectionColor (Color.clear);
		}
	}


	private void Start () {
		selectionPlane = this.transform.Find ("SelectionPlane").GetComponent<Renderer> ();
#if UNITY_EDITOR
		if (selectionPlane == null) {
			Debug.LogError ("Tile didn't find a selectionPlane; all Tiles need a child component named specifically 'SelectionPlane'.");
		}
#endif
		SetSelectionColor (Color.clear);
	}

	public void SetSelectionColor (Color c) {
		selectionPlane.material.color = c;
	}
	public void HideSelectionColor () {
		selectionPlane.material.color = Color.clear;
	}
	public void BlinkSelectionColor (float newAlpha) {
		Color c = selectionPlane.material.color;
		c.a = newAlpha;
		selectionPlane.material.color = c;
	}

	// Pathfinding Variables
	[HideInInspector] public int positionX;
	[HideInInspector] public int positionY;
	[HideInInspector] public int requiredMovement;
	[HideInInspector] public int gCost;
	[HideInInspector] public int hCost;
	[HideInInspector]
	public int fCost {
		get {
			return gCost + hCost;
		}
	}
	[HideInInspector] public LandTile parent;

	// Heap Management Variables
	int heapIndex;
	[HideInInspector]
	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public int CompareTo (LandTile tileToCompare) {
		int compare = fCost.CompareTo (tileToCompare.fCost);
		if (compare == 0)
			compare = hCost.CompareTo (tileToCompare.hCost);
		return -compare;
	}
}

public enum LandType {
	Empty,
	Gras,
	Dirt,
	Stone,
	Sand,
	Water
}