using UnityEngine;
using System.Collections.Generic;

public class Pathfinding {

	// Pathfinding functions specifically handles finding a path from A to B. 
	// Rangefinder functions specifically handles finding all reachable tiles from a certain point, according to given conditions. 

	// Components
	private LandTileMap tileMap;                                // A reference to the LandTileMap


	public Pathfinding (LandTileMap tileMap) {
		this.tileMap = tileMap;
	}

	///<summary>
	/// Finds a path from Tile A to Tile B
	///</summary>
	public LandTile [] FindPath (LandTile startTile, LandTile endTile, bool canMoveDiagonally, bool canMoveThroughUnitPositions) {
		LandTile [] path = new LandTile [0];
		bool pathfindingSuccess = false;

		if (startTile.isPathable && endTile.isPathable) {
			Heap<LandTile> openSet = new Heap<LandTile> (tileMap.totalSize);
			HashSet<LandTile> closedSet = new HashSet<LandTile> ();
			openSet.Add (startTile);

			while (openSet.Count > 0) {
				LandTile current = openSet.RemoveFirst ();
#if UNITY_EDITOR
				if (DebugSettings.debugUnitPath) {
					Debug.Log ("Testing tile " + openSet + ", at position: " + current.transform.position + ".");
					current.SetSelectionColor (Color.cyan);
					TextMesh text = new GameObject ().AddComponent<TextMesh> ();
					text.text = "" + closedSet.Count + 1;
					text.color = Color.black;
					text.fontSize = 6;
					text.alignment = TextAlignment.Center;
					text.transform.localRotation = Quaternion.Euler (90, 0, 0);
					text.gameObject.transform.parent = current.gameObject.transform;
					text.gameObject.transform.localPosition = new Vector3 (0, 1, 0.4f);
				}
#endif
				closedSet.Add (current);

				if (current == endTile) {
					pathfindingSuccess = true;
					break;
				}

				List<LandTile> neighbourTiles = (canMoveDiagonally) ? tileMap.GetTileNeighbours (current) : tileMap.GetDirectTileNeighbours (current);
				foreach (LandTile neighbour in neighbourTiles) {
					if (!neighbour.isPathable || closedSet.Contains (neighbour))
						continue;
					if (!canMoveThroughUnitPositions && neighbour.unitOnTile && neighbour != endTile) {
						closedSet.Add (neighbour);
						continue;       // If we can't select a tile that has a unit on it
					}
					int costToNeighbour = current.gCost + GetDistance (current, neighbour, canMoveDiagonally) + neighbour.movementCost;
					if (costToNeighbour < neighbour.gCost || !openSet.Contains (neighbour)) {
						neighbour.gCost = GetDistance (current, startTile, canMoveDiagonally);
						neighbour.hCost = GetDistance (neighbour, endTile, canMoveDiagonally);
						neighbour.parent = current;

						if (!openSet.Contains (neighbour)) {
							openSet.Add (neighbour);
						}
						else {
							openSet.UpdateItem (neighbour);
						}
					}
				}
			}
		}
#if UNITY_EDITOR
		if (DebugSettings.debugUnitPath) {
			Debug.Log ("Done Pathfinding, was a path found?: " + pathfindingSuccess + ".");
		}
#endif
		if (pathfindingSuccess) {
			path = RetracePath (startTile, endTile);
		}
		return path;
	}

	// Connects a path together by stepping through it backwards using each tile's 'parent' in the path.
	private LandTile [] RetracePath (LandTile startTile, LandTile endTile) {
		List<LandTile> path = new List<LandTile> ();
		LandTile current = endTile;

		while (current != startTile) {
			path.Add (current);
			current = current.parent;
		}
#if UNITY_EDITOR
		if (DebugSettings.debugUnitPath) {
			Debug.Log ("------------------ Total Path ------------------");
			int x = 0;
			foreach (LandTile t in path) {
				Debug.Log ("Step " + x + ": " + t.transform.position);
				x++;
			}
		}
#endif
		path.Reverse ();
		LandTile [] tilePath = path.ToArray ();

		return tilePath;
	}

	// Removes all parts of the path that move in the same direction (currently unused)
	private Vector3 [] SimplifyPath (ref List<LandTile> path) {
		List<Vector3> waypoints = new List<Vector3> ();
		Vector2 directionOld = Vector2.zero;

		for (int i = 1; i < path.Count; i++) {
			Vector2 directionNew = new Vector2 (path [i - 1].positionX - path [i].positionX, path [i - 1].positionY - path [i].positionY);
			if (directionNew != directionOld) {
				waypoints.Add (new Vector3 (path [i - 1].transform.position.x, path [i - 1].tileHeight, path [i - 1].transform.position.z));
			}
			directionOld = directionNew;
		}
		return waypoints.ToArray ();
	}

	// returns a score that represents the distance between the starting and ending tile.
	private int GetDistance (LandTile startTile, LandTile endTile, bool canMoveDiagonally) {
		int dstX = Mathf.Abs (startTile.positionX - endTile.positionX);
		int dstY = Mathf.Abs (startTile.positionY - endTile.positionY);

		if (!canMoveDiagonally)
			return (dstX + dstY) * 10;

		if (dstX > dstY) {
			return 14 * dstY + (dstX - dstY) * 10;
		}
		else {
			return 14 * dstX + (dstY - dstX) * 10;
		}
	}

	///<summary>
	/// Returns a LandTile[] containing all LandTiles within range of the startTile that fit the conditions.
	///</summary>
	public LandTile [] FindRange (LandTile startTile, int range, bool diagonal, bool includeMovementCost, bool canTargetStartPosition, bool canTargetUnitPositions) {
		List<LandTile> foundRange = new List<LandTile> ();
		List<LandTile> openSet = new List<LandTile> (tileMap.totalSize);
		HashSet<LandTile> closedSet = new HashSet<LandTile> ();

		openSet.Add (startTile);
		startTile.requiredMovement = 0;

		if (canTargetStartPosition)
			foundRange.Add (startTile);

		while (openSet.Count > 0) {
			LandTile current = openSet [0];
			openSet.Remove (current);
			closedSet.Add (current);

			if (current.requiredMovement == range)
				continue;

			List<LandTile> neighbourTiles = (diagonal) ? tileMap.GetTileNeighbours (current) : tileMap.GetDirectTileNeighbours (current);
			foreach (LandTile neighbour in neighbourTiles) {
				if (!neighbour.isPathable) {
					closedSet.Add (neighbour);
					continue;       // If the tile is unpathable.
				}
				if (!canTargetUnitPositions && neighbour.unitOnTile) {
					closedSet.Add (neighbour);
					continue;       // If we can't select a tile that has a unit on it
				}
				if (closedSet.Contains (neighbour) || openSet.Contains (neighbour))
					continue;       // If the tile has already been checked before.

				int movementToReachTile = (includeMovementCost) ? current.requiredMovement + neighbour.movementCost : current.requiredMovement + 1;
				if (movementToReachTile <= range) {
					neighbour.requiredMovement = movementToReachTile;
					foundRange.Add (neighbour);
					openSet.Add (neighbour);
				}
			}
		}

#if UNITY_EDITOR
		if (DebugSettings.debugRangefinder) {
			Debug.Log ("=== " + foundRange.Count + " Tiles In Range Found: ===");
			foreach (LandTile tile in foundRange) {
				Debug.Log (tile.gameObject.name);
			}
		}
#endif

		return foundRange.ToArray ();
	}

	///<summary>
	/// Returns a Unit[] containing all Units within range of the startTile.
	///</summary>
	public Unit [] FindUnitsInRage (LandTile startTile, int range) {
		List<Unit> foundUnits = new List<Unit> ();
		List<LandTile> openSet = new List<LandTile> (tileMap.totalSize);
		HashSet<LandTile> closedSet = new HashSet<LandTile> ();

		startTile.requiredMovement = 0;
		if (startTile.unitOnTile != null)
			foundUnits.Add (startTile.unitOnTile);

		while (openSet.Count > 0) {
			LandTile current = openSet [0];
			openSet.Remove (current);
			closedSet.Add (current);

			if (current.requiredMovement == range)
				continue;

			int movementToReachTile = current.requiredMovement + 1;

			List<LandTile> neighbourTiles = tileMap.GetDirectTileNeighbours (current);
			foreach (LandTile neighbour in neighbourTiles) {
				if (closedSet.Contains (neighbour) || openSet.Contains(neighbour))
					continue;       // If the tile has already been checked before.

				if (LandTileMap.instance.DoesTileContainUnit (neighbour)) {
					foundUnits.Add (neighbour.unitOnTile);
				}

				if (movementToReachTile == range) {
					closedSet.Add (neighbour);          // If that is at the range of the edge distance, add it to closed
				}
				else {
					neighbour.requiredMovement = movementToReachTile;
					openSet.Add (neighbour);            // Otherwise add it to OpenSet so its Neighbours get checked
				}
			}
		}

#if UNITY_EDITOR
		if (DebugSettings.debugRangefinder) {
			Debug.Log ("=== Units In Range Found: ===");
			if (foundUnits.Count == 0)
				Debug.Log ("None were found");
			foreach (Unit u in foundUnits) {
				Debug.Log (u.gameObject.name);
			}
		}
#endif

		return foundUnits.ToArray ();
	}

}
