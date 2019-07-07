using System.Collections.Generic;
using System.Linq;

public class TurnManager {

	// TurnManager is contained by UnitManager, and manages: The Turn Order, Starting and Ending turns.

	public List<Turn> turnOrder = new List<Turn> ();		// List of all turns; in order from first to last
	public Turn currentTurn;								// reference to the currently active turn

	private UnitManager unitManager;						// Reference to the UnitManager


	public TurnManager (UnitManager unitManager) {
		this.unitManager = unitManager;
	}

	public bool GetTurnType () {
		return (currentTurn is UnitTurn) ? true : false;
	}

	///<summary>
	/// Sets up the initial turn order from all units on the map
	///</summary>
	public void SetupTurnOrder (Unit[] unitList) {
		turnOrder.Clear ();

		unitList = unitList.OrderByDescending (Unit => Unit.unitStats.speedRating).ToArray<Unit>();

		foreach (Unit u in unitList) {
			UnitTurn t = new UnitTurn (u, 100);
			turnOrder.Add (t);
		}
		currentTurn = turnOrder [0];
	}

	///<summary>
	/// Adds a Unit to the Turnorder
	///</summary>
	public void AddUnitTurn (Unit unit, bool startWithFullTimer) {
		UnitTurn t = new UnitTurn (unit, startWithFullTimer ? 100 : 0);
		turnOrder.Add (t);

		turnOrder = turnOrder.OrderByDescending (Turn => Turn.turnTimer).ToList ();
	}
	///<summary>
	/// Adds a CombatAction to the turn order
	///</summary>
	public void AddCombatAction (Unit unit, LandTile tileToTarget, Ability abilityToCast, int requiredTurnTimer) {
		CombatAction t = new CombatAction (unit, tileToTarget, abilityToCast, requiredTurnTimer);
		turnOrder.Add (t);

		turnOrder = turnOrder.OrderByDescending (Turn => Turn.turnTimer).ToList ();
	}

	public void RemoveUnitTurn (Unit unit) {
		if (currentTurn.unit == unit)
			currentTurn = null;

		for (int i = 0; i < turnOrder.Count; i++) {
			if (turnOrder [i].unit == unit) {
				turnOrder.Remove (turnOrder [i]);
				i--;
			}
		}
	}

	///<summary>
	/// Ends the current turn by reducing its turnTimer (placing it at the bottom of the turn order).
	///</summary>
	public void EndRepeatTurn (int newTurnTimer) {      // Currently only UnitTurns are counted as RepeatTurns, might add functionality to make spells repeatable
		currentTurn.turnTimer = currentTurn.turnTimer - newTurnTimer;
		if (currentTurn.turnTimer > 49) currentTurn.turnTimer = 50;

		if (currentTurn != null) {
			turnOrder.Remove (currentTurn);
			turnOrder.Add (currentTurn);
		}

		AdjustTurnOrder ();
	}

	///<summary>
	/// Ends the current turn by removing it from the turn order completely. 
	///</summary>
	public void EndOneShotTurn () {
		CombatAction action = (CombatAction)currentTurn;

		if (currentTurn is CombatAction)
			turnOrder.Remove (currentTurn);

		AdjustTurnOrder ();
	}

	private void AdjustTurnOrder () {
		// Checking if a Unit is ready to take a turn
		if (turnOrder [0].turnTimer >= 100) {
			currentTurn = turnOrder [0];
		} else {
			// Keep increasing the ChargeTimer until someone reaches 100
			bool waitingForTurn = true;
			while (waitingForTurn) {
				foreach (Turn t in turnOrder) {
					t.turnTimer += t.unit.unitStats.speedRating;
					if (t.turnTimer >= 100) {
						waitingForTurn = false;
					}
				}
			}

			turnOrder = turnOrder.OrderByDescending (Turn => Turn.turnTimer).ToList ();   // Maybe adjust so if multiple share the highest the best speed is prioritised
			currentTurn = turnOrder [0];
		}

		// Setting the temporary turn order text in the UIManager REPLACE LATER
		string s = "Turn Order: ";
		foreach (Turn t in turnOrder) {
			if (t is UnitTurn) {
				s += "\n" + t.turnTimer + ": " + t.unit + ".";
			}
			else {
				s += "\n" + t.turnTimer + ": " + t.unit + " (" + ((CombatAction) t).abilityToCast.abilityName + ").";
			}
		}
		UIManager.instance.TempTurnIndicator (s);
	}
}

public class Turn {
	public Unit unit;               // The Unit that does the turn/action
	public int requiredTimer;       // The amount the turnTimer has to reach for a turn to occure.
	public int turnTimer;           // The timer that fills up based on a units speed; and gives a turn when over requiredTimer.
}

public class UnitTurn : Turn {
	public UnitTurn (Unit unit, int turnTimer) {
		this.unit = unit;
		this.turnTimer = turnTimer;
		this.requiredTimer = 100;
	}
}

public class CombatAction : Turn {
	public Ability abilityToCast;
	public LandTile targettedTile;

	public CombatAction (Unit unit, LandTile tileToTarget, Ability abilityToCast, int requiredTurnTimer) {
		this.unit = unit;
		this.targettedTile = tileToTarget;
		this.abilityToCast = abilityToCast;
		this.requiredTimer = requiredTurnTimer;
		this.turnTimer = 0;
	}
}

