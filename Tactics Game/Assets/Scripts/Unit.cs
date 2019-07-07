using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour {

	// Refactor this to something like a UnitFactory that creates, holds and handles all unit logic/calls (since it's practically all the same).
	// The Unit itself just holds its own data

	public Sprite unitAvatar;								// The Sprite used for the unit's icon
	public int level;                                       // The unit's level
	public Ability[] unitAbilityList;

	// Movement/Pathfinding
	private float unitHeight = 0f;							// The unit's height (used to calculate it's position above tiles)
	private LandTile [] movementPath;						// The path for the unit to follow
	private float speed = 3;								// The speed at which a unit follows a path (not it's movement distance!)
	private int targetIndex;                                // Used in the FollowPath IEnumerator

	// Gameplay-related stats
	[HideInInspector] public LandTile currentTile;          // Reference to the tile the unit is positio
	private UnitManager unitManager;
	public UnitStats unitStats = new UnitStats ();

	public bool tempIsControledByPlayer = false;


	public void SetupUnit (UnitManager unitManager) {
		this.unitManager = unitManager;

		StopCoroutine ("FollowPath");

		unitStats.SetupStats ();

		//@todo Move this to some kind of overal manager eventually
		LandTileMap.instance.SetUnitOnTile (this, this.transform.position);

		if (!currentTile.isPathable) {
			LandTileMap.instance.SetUnitOnTile (this, LandTileMap.instance.GetFreeTile (currentTile));
			this.transform.position = new Vector3 (currentTile.transform.position.x, unitHeight + currentTile.tileHeight, currentTile.transform.position.z);
		}

		this.transform.position = new Vector3 (this.transform.position.x, currentTile.tileHeight + unitHeight, this.transform.position.z);
	}


#region  //------ Movement Functions ----------------------------------------------------------------------------------------------
	public void StartPath (LandTile[] path) {
		movementPath = path;

		StopCoroutine ("FollowPath");
		StartCoroutine ("FollowPath");
	}

	// Actually phyiscally moves the unit over the map, following the given path.
	private IEnumerator FollowPath () {
		if (movementPath == null || movementPath [0] == null) {
			yield break;
		}

		Vector3 currentWaypoint = new Vector3 (movementPath [0].transform.position.x, movementPath [0].tileHeight + unitHeight, movementPath [0].transform.position.z);
		targetIndex = 0;

		while (true) {
			if (transform.position == currentWaypoint) {
				targetIndex++;

				if (targetIndex >= movementPath.Length) {       // If at the end of the path
					unitManager.UnitFinishedMoving ();
					yield break;
				}
				currentWaypoint = new Vector3 (movementPath [targetIndex].transform.position.x, movementPath [targetIndex].tileHeight + unitHeight, movementPath [targetIndex].transform.position.z);
			}
			transform.position = Vector3.MoveTowards (transform.position, currentWaypoint, speed * Time.deltaTime);
			yield return null;
		}
	}
#endregion

	public void TakeDamage (int damage) {
		Debug.Log ("Unit: " + this.gameObject.name + " takes " + damage + " points of damage.");
		// Show floating damage text
		unitStats.currentHealth -= damage;
		if (unitStats.currentHealth <= 0) {
			Die ();
		}
	}

	public void Die () {
		Debug.Log ("I died " + this.name + ". :(");

		unitManager.UnitDied (this);
		Destroy (this.gameObject, 0.1f);
	}


	private void OnDrawGizmos () {
		// Draws blue cubes over tiles to show the unit's movement path.

		if (!DebugSettings.debugUnitPath)
			return;

		if (movementPath != null && movementPath [0] != null) {
			for (int i = targetIndex; i < movementPath.Length; i++) {
				Gizmos.color = Color.blue;
				Gizmos.DrawWireCube (movementPath [i].transform.position + new Vector3 (0, 0.1f, 0), Vector3.one);
				movementPath [i].SetSelectionColor (Color.blue);

				if (i == targetIndex) {
					Gizmos.DrawLine (transform.position, movementPath [i].transform.position);
				}
				else {
					Gizmos.DrawLine (movementPath [i - 1].transform.position, movementPath [i].transform.position);
				}
			}
		}
	}

}
