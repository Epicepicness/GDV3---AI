using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour {

	// UnitManager handles creating and destroying units, as well as handles all unit-related calls and steers individual units.
	// UnitManager contains all unit-related logic, Units contain their own data as well as handle their own animations.

	private TurnManager turnManager;
	private AIManager aiManager;

#pragma warning disable CS0649
	[SerializeField] GameObject turnIndicator;
	public List<Unit> allUnitsList = new List<Unit> ();
	public List<Unit> tempFriendlyUnitList = new List<Unit> ();
	public List<Unit> tempEnemyUnitList = new List<Unit> ();
	private Ability abilityBeingUsed;           // Reference to the ability being used during the tile-selection cursor; find better way of achieving this later.
#pragma warning restore CS0649

	public Unit currentTurnUnit { get { return turnManager.currentTurn.unit; } }
	public bool unitHasMoved { get; private set; }          // Tracks if the current unit has moved this turn
	public bool unitHasActed { get; private set; }          // Tracks if the current unit has taken an action this turn


	private void Awake () {
		turnManager = new TurnManager (this);
		turnManager.SetupTurnOrder (allUnitsList.ToArray ());

		aiManager = new AIManager (this);
	}

	private void Start () {
		LandTileMap.instance.SetupGridCursor (this);

		SetupAllUnits ();
	}

	private bool tempExecuteStartTurn = true;
	private void Update () {
		if (tempExecuteStartTurn) {
			tempExecuteStartTurn = false;   // Now in Update as to skip one frame and have everything setup beforehand.
			StartUnitTurn ();				// Should eventually be done once a battle scene is loaded and setup properly.
		}
	}

	//@todo Find way to merge all gridmap/GridCursor functions to one function? Currently have a seperate set of functions for each
	//@todo Save the previous UI state in the UIManager, return to that if the selection cursor is canceled.

	private void SetupAllUnits () {
		foreach (Unit u in allUnitsList) {
			u.SetupUnit (this);
		}
	}

	public void UnitDied (Unit unit) {
		if (currentTurnUnit == unit) {
			EndTurn ();
		}

		turnManager.RemoveUnitTurn (unit);

		allUnitsList.Remove (unit);
		if (unit.tempIsControledByPlayer)
			tempFriendlyUnitList.Remove (unit);
		else
			tempEnemyUnitList.Remove (unit);

		if (tempFriendlyUnitList.Count == 0)
			UIManager.instance.ShowDefeatScreen ();
		if (tempEnemyUnitList.Count == 0)
			UIManager.instance.ShowVictoryScreen ();
	}

	#region  //------ Turn Functions ----------------------------------------------------------------------------------------------
	// Called at the end of EndTurn(); Sets everything up for a new turn
	private void StartUnitTurn () {
		unitHasActed = false;
		unitHasMoved = false;

		turnIndicator.transform.parent = currentTurnUnit.transform;
		turnIndicator.transform.localPosition = Vector3.zero;
		turnIndicator.gameObject.SetActive (true);

		if (turnManager.GetTurnType ()) {
			if (currentTurnUnit.tempIsControledByPlayer) {
				UIManager.instance.SelectCurrentUnit ();
			}
			else {
				aiManager.RunAI ();
			}
		}
		else {
			CombatAction action = turnManager.currentTurn as CombatAction;
			ActivateCastingAbility (action.unit, action.targettedTile, action.abilityToCast);
		}
	}
	///<summary>
	/// Ends the current turn, and starts the next turn
	///</summary>
	public void EndTurn () {
		LandTileMap.instance.DisableGridCursor ();
		UIManager.instance.DeselectUnit ();

		if (turnManager.GetTurnType ()) {
			int turnTimerReduction;
			if (unitHasActed && unitHasMoved) turnTimerReduction = 100;
			else if (unitHasActed || unitHasMoved) turnTimerReduction = 80;
			else turnTimerReduction = 60;

			turnManager.EndRepeatTurn (turnTimerReduction);
		} else {
			turnManager.EndOneShotTurn ();
		}

		turnIndicator.gameObject.SetActive (false);         // Disabling the turnindicator; setting parent to this to prevent accidentally losing it
		turnIndicator.transform.parent = this.transform;

		//@todo Zouden we hier een frame kunnen wachten? Zeker weten dat er geen vreemde overwrites zijn, geen meerdere AI calculaties in 1 frame, etc etc.
		//StartUnitTurn ();
		tempExecuteStartTurn = true;
	}

	private void CheckTurnStatus () {
		if (currentTurnUnit == null)    // If the unit somehow died
			EndTurn ();

		if (currentTurnUnit.tempIsControledByPlayer) {
			if (unitHasMoved) {
				EndTurn ();
			} else {
				UIManager.instance.SelectCurrentUnit ();
			}
		} else {
			aiManager.ContinueAI ();
		}
	}
	#endregion

	#region //------ Movement Functions ----------------------------------------------------------------------------------------------
	///<summary>
	/// Called from the UIManager script when the "move" button is pressed in the UI.
	///</summary>
	public void ShowMovementIndicators () {
		LandTile [] movementDistance = LandTileMap.instance.RequestRange (currentTurnUnit.transform.position, currentTurnUnit.unitStats.movementDistance, currentTurnUnit.unitStats.canMoveDiagonally, true, false, false);

		if (movementDistance.Length == 0) {
			CancelRangeIndicator (true);
			return;
		}
		LandTileMap.instance.ActivateTileIndicators (movementDistance, Color.blue);
		LandTileMap.instance.ActivateGridCursor (currentTurnUnit.transform.position, RequestPath, CancelRangeIndicator);
	}

	// Asks the Pathfinding class to generate a path to a target tile; if there's a path it'll call "OnPathFound()", and start following the path until it reaches it goal.
	public void RequestPath (Transform targetTile) {
		LandTile [] newPath = LandTileMap.instance.RequestPath (currentTurnUnit.transform.position, targetTile.position, currentTurnUnit.unitStats.canMoveDiagonally, false);

		if (newPath != null && newPath.Length != 0) {
			// Fix this later, this is messy
			LandTileMap.instance.DisableGridCursor ();
			LandTileMap.instance.DeactivateTileIndicators ();
			LandTileMap.instance.MoveUnitToTile (currentTurnUnit, newPath [newPath.Length - 1]);
			CameraManager.instance.FollowObject (currentTurnUnit.transform);
			GameManager.instance.SetModeProcessing ();

			currentTurnUnit.StartPath (newPath);
		}
	}
	///<summary>
	/// Called from the Unit script once it has completed its movement across the path; resets the processing/follwing state.
	///</summary>
	public void UnitFinishedMoving () {
		CameraManager.instance.StopFollowingObject ();
		unitHasMoved = true;
		CheckTurnStatus ();
	}
	#endregion

	#region //------ Attack Functions ----------------------------------------------------------------------------------------------
	///<summary>
	/// Shows movement distance, enable tile-curser; if a tile is chosen call RequestPath() to find a path and move; if canceled call the CancelMovementIndicator()
	///</summary>
	public void ShowAttackIndicators () {
		LandTile [] attackRange = LandTileMap.instance.RequestRange (currentTurnUnit.transform.position, 1, false, false, false, true);

		if (attackRange.Length == 0) {
			CancelRangeIndicator (true);
			return;
		}
		LandTileMap.instance.ActivateTileIndicators (attackRange, Color.red);
		LandTileMap.instance.ActivateGridCursor (currentTurnUnit.transform.position, AttackTile, CancelRangeIndicator);
	}
	///<summary>
	/// Attacks a target tile using the current turn unit.
	///</summary>
	public void AttackTile (Transform attackTile) {
		LandTileMap.instance.DisableGridCursor ();
		LandTileMap.instance.DeactivateTileIndicators ();

		// Calculate hit chance
		// Calculate damage number

		Unit targetUnit = LandTileMap.instance.GetTileByWorldPoint (attackTile.position).unitOnTile;
		if (targetUnit != null) {
			targetUnit.TakeDamage (currentTurnUnit.unitStats.attackPower);
		}

		unitHasActed = true;
		CheckTurnStatus ();
	}
	#endregion

	#region //------ Ability Functions ----------------------------------------------------------------------------------------------
	///<summary>
	/// Shows movement distance, enable tile-curser; if a tile is chosen call RequestPath() to find a path and move; if canceled call the CancelMovementIndicator()
	///</summary>
	public void ShowAbilityRange (Ability ability, bool canTargetUnitTiles) {
		abilityBeingUsed = ability;
		LandTile [] targetableTiles = LandTileMap.instance.RequestRange (currentTurnUnit.transform.position, ability.range, false, false, true, canTargetUnitTiles);

		LandTileMap.instance.ActivateTileIndicators (targetableTiles, Color.red);
		LandTileMap.instance.ActivateGridCursor (currentTurnUnit.transform.position, OnGridSelection, CancelRangeIndicator);
	}

	// Called when the gridcursor selects a tile, and calls BeginCastingAbility (currently can't call directly; as it requires a transform)
	private void OnGridSelection (Transform transform) {
		BeginCastingAbility (currentTurnUnit, LandTileMap.instance.GetTileByWorldPoint (transform.position), abilityBeingUsed);
	}

	///<summary>
	/// Called when the command is given (by the player, via the UI/Gridcursor) to cast an ability; Called from the Ability's script.
	/// It handles: either calling ActivateCastingAbility(), or adding the ability to the TurnOrder; as well as moving on with the turn.
	///</summary>
	public void BeginCastingAbility (Unit unit, LandTile tile, Ability ability) {
		unit.unitStats.currentMana -= ability.manaCost;

		CancelRangeIndicator (true);

		if (ability.castTime == 0) {
			ActivateCastingAbility (unit, tile, ability);
		} else {
			Debug.Log ("Ability: " + ability.abilityName + " added to the turn list (" + ability.castTime + ").");
			turnManager.AddCombatAction (unit, tile, ability, ability.castTime);
		}
		// Set unit's state to 'casting'; so it's animations etc can be set

		unitHasActed = true;
		CheckTurnStatus ();
	}

	///<summary>
	/// Called from either BeginCastingAbility or the turnManager.
	/// Handles: actually casting the ability; calling the ability's event script, etc.
	///</summary>
	public void ActivateCastingAbility (Unit castingUnit, LandTile targetTile, Ability ability) {
		Debug.Log ("Ability: " + ability.abilityName + " being cast.");
		//@todo UI shows ability name
		//@todo Unit plays animation (sound is likely part of the animation?)

		Unit [] unitsToAffect = LandTileMap.instance.RequestUnits (targetTile.transform.position, ability.areaSize);

		ability.OnAbilityCast (unitsToAffect);

		if (ability.castTime == 0) {
			unitHasActed = true;
			CheckTurnStatus ();
		} else {
			EndTurn ();
		}
	}
#endregion

	// Called if the cancel key is pressed while selecting a movement location
	private void CancelRangeIndicator (bool b) {
		LandTileMap.instance.DisableGridCursor ();
		LandTileMap.instance.DeactivateTileIndicators ();
		UIManager.instance.SelectSpecificUnit (currentTurnUnit);
	}

}
