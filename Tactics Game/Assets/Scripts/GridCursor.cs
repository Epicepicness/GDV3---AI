using UnityEngine;
using System;

public class GridCursor : MonoBehaviour {

	// The GridCursor is the cursor used to select things on the grid with.
	// This is used when: selecting position for Movement; selecting a tile to attack / use Ability on; 
	// Or when canceling out of a unit's action selection to look around the map a bit.

#pragma warning disable CS0649
	[SerializeField] private GameObject CursorArrow;

	private UnitManager unitManager;
#pragma warning restore CS0649

	[SerializeField] private float hoverSpeed = 4;
	[SerializeField] private float hoverHeight = 0.4f;
	[SerializeField] private float defaultHeight = 1;

	private int xPosition = 0;
	private int yPosition = 0;
	private LandTile targettedTile;
	private bool isSelecting = false;				// Tracks if the Cursor is currently used to make a selection.

	public Action<Transform> OnConfirmationAction;		// The action that should be taken if a valid tile is selected.
	public Action<bool> OnCancelationAction;              // The action that should be taken if canceled out of the selection.

	private bool temp = false;  // Temp for skip frame on start; not required normally

			// Maak hier een state machine voor? Overbodig? Normale map cursing state; tile-selection state; iets anders?
	
	public void SetupGridCursor (UnitManager unitManager) {
		this.unitManager = unitManager;
	}


	private void Update () {
		//if (GameManager.gameState != GameManager.GameStates.waitingForInputCursor)
		//	return;
		if (!temp) {
			Setup ();
			temp = true;
		}                   // Temporary, remove (used to skip a frame for setup; not required normally

		bool hasMoved = false;

		if (InputManager.Current.HorizontalDirectionalInput) {
			if (xPosition + InputManager.Current.DirectionalInput.x < LandTileMap.instance.mapSizeX && xPosition + InputManager.Current.DirectionalInput.x > -1) {
				xPosition += (int) InputManager.Current.DirectionalInput.x;
				hasMoved = true;
			}
		}
		if (InputManager.Current.VerticleDirectionalInput) {
			if (yPosition + InputManager.Current.DirectionalInput.y < LandTileMap.instance.mapSizeY && yPosition + InputManager.Current.DirectionalInput.y > -1) {
				yPosition += (int) InputManager.Current.DirectionalInput.y;
				hasMoved = true;
			}
		}
		// IMPLEMENT "HOLD DOWN KEY" FOR QUICK MOVEMENT AFTER ~1 SECOND

		CursorMovement (hasMoved);

		if (InputManager.Current.ConfirmationKey) {
			if (isSelecting) {              // if selecting, and valid target do the Confirmation action.
				if (LandTileMap.instance.IsTileHighlighted (xPosition, yPosition)) {
					OnConfirmationAction (LandTileMap.instance.GetLandTile (xPosition, yPosition).transform);
				}
			}
			else {                      // If the cursor is not being used to select a target, it will select the current target (if any).
				if (LandTileMap.instance.DoesTileContainUnit (xPosition, yPosition)) {
					UIManager.instance.SelectSpecificUnit (LandTileMap.instance.GetLandTile (xPosition, yPosition).unitOnTile);
				}
			}
		}
		if (InputManager.Current.CancelKey) {
			if (isSelecting) {              // if selecting, do the cancelation action
				OnCancelationAction (true);
			}
			else {							// If in map-cursing state; return to the current-turn unit and select it
				UIManager.instance.SelectSpecificUnit (unitManager.currentTurnUnit);
				LandTileMap.instance.DisableGridCursor ();
			}
		}
	}


	private void Setup () {
		targettedTile = LandTileMap.instance.GetLandTile (xPosition, yPosition);
		CursorMovement (true);

		CameraManager.instance.FollowObject (this.transform);   // TEMPORARY THIS IS JUST HERE TO START THE SCENE CORRECTLY BEFORE PROPER START IS IMPLEMENTED
	}

	// Adjusts the cursor's position, makes the cursor Arrow move up and down, and if there's a new tile it checks the tile
	private void CursorMovement (bool hasMoved) {
		float newY = (Mathf.Sin (Time.time * hoverSpeed) * hoverHeight) + (defaultHeight + targettedTile.tileHeight);
		CursorArrow.transform.localPosition = new Vector3 (0, 0, -newY);  // Adjusts Z-axis, and negative, due to rotation magic

		if (hasMoved) {
			targettedTile = LandTileMap.instance.GetLandTile (xPosition, yPosition);

			this.transform.position = new Vector3 (xPosition, targettedTile.tileHeight-0.4f, yPosition);
			if (targettedTile.unitOnTile != null) {
				UIManager.instance.ShowUnitFrame (targettedTile.unitOnTile);
			} else {
				UIManager.instance.DeselectUnit ();
			}
		}
	}

	///<summary>
	/// Called to show the cursor on the grid; without a movement/ability selection
	///</summary>
	public void SetupSelectionCursor (int xPosition, int yPosition) {
		isSelecting = false;
		this.xPosition = xPosition; this.yPosition = yPosition;
		this.transform.position = new Vector3 (xPosition, targettedTile.tileHeight - 0.4f, yPosition);

		targettedTile = LandTileMap.instance.GetLandTile (xPosition, yPosition);
	}
	///<summary>
	/// Called to show the cursor on the grid; with movement/ability selection
	///</summary>
	public void SetupSelectionCursor (int xPosition, int yPosition, Action<Transform> OnConfirmationEvent, Action<bool> OnCancelationEvent) {
		isSelecting = true;
		this.xPosition = xPosition; this.yPosition = yPosition;
		this.transform.position = new Vector3 (xPosition, targettedTile.tileHeight - 0.4f, yPosition);

		this.OnConfirmationAction = OnConfirmationEvent;
		OnCancelationAction = OnCancelationEvent;

		targettedTile = LandTileMap.instance.GetLandTile (xPosition, yPosition);
	}

}
