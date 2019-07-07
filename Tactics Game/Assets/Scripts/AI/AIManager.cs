using System.Collections.Generic;
using System.Linq;
using UnityEngine;  // Currently only used for Debug.Log

public class AIManager {

	/* Calculate beide teams score
		// Avarage level
		// Number of people alive
		// Current / Total health

	// Bereken Team state based op deze eigenschappen
		// Defensive, offensive
	*/

	// Score voor een debuff zou kunnen zijn: potentiele damage voorkomen * normalized(% chance of success)
	//@todo Verspreid de pathfinding over een aantal frames, zodat er niet 1 grote pathfinding spike op 1 frame valt
	// Als dit hide achter een UI transition of "x's turn" popup, kan dit heel ver uitgespreid worden (1 frame per: unit search, ability list, ability testing, execution; of nog verder).

	private List<OtherUnit> friendlyUnits = new List<OtherUnit> ();
	private List<OtherUnit> enemyUnits = new List<OtherUnit> ();
	private List<AIAction> actionsToConsider = new List<AIAction> ();
	private OtherUnit self;     // Zoek een beter manier dan dit om een self-reference van OtherUnit type te krijgen

	private AIAction [] actionSequence = new AIAction [2];
	private int sequenceIndex = 0;

	private UnitManager unitManager;


	public AIManager (UnitManager unitManager) {
		this.unitManager = unitManager;
	}

	///<summary>
	/// Starts off the AI; lets it make a decision on how to act
	///</summary>
	public void RunAI () {
#if UNITY_EDITOR
		if (DebugSettings.debugAIManager) {
			Debug.Log ("_____.....--~~~^^^^^^^^~~~--....._____");
			Debug.Log ("Starting AI for unit " + unitManager.currentTurnUnit + ".");
		}
#endif

		ScanForUnits ();        // Splitting into functions means some actions have to be done twice.
		ScanForActions ();      // But it does increase readability, instead of one absurdly large function (especially as features get added). 

		AIAction [] actionList = actionsToConsider.OrderByDescending (Action => Action.score).ToArray<AIAction> ();

#if UNITY_EDITOR
		if (DebugSettings.debugAIManager) {
			Debug.Log ("-**- AI " + unitManager.currentTurnUnit.name + " going. \n Has " + actionList.Length + " possible actions. -**-");
			int debugitterator = 0;
			foreach (AIAction action in actionList) {
				if (action is AIAttackAction) Debug.Log ("Action " + debugitterator + ": Attacking " + action.targetUnit.unit + ", for " + action.score + " score.");
				if (action is AIAbilityAction) Debug.Log ("Action " + debugitterator + ": casting " + "[ability name]" + ", on: " + action.targetUnit.unit + ", for " + action.score + " score.");
				debugitterator++;
			}
		}
#endif

		// Keeps trying actions (starting with highest score) until one succeeds
		int i = 0;
		bool hasTestedAction = false;

		while (hasTestedAction) {
			hasTestedAction = actionList [i].Try (unitManager.currentTurnUnit);
			i++;
		} // After this we have the highest scored action that has tested successfully (and thus should be executeable).

		if (actionList.Length != 0 && actionList [i] != null) {
			if (actionList [i].requiresMovement) {
				actionSequence [0] = new AIMoveAction (actionList [i].tileToMoveTo);
				actionSequence [1] = actionList [i];
			}
			else {
				actionSequence [0] = actionList [i];
				AIMoveAction newMoveAction = new AIMoveAction (FindPointToMoveTo ());
				if (newMoveAction.tileToMoveTo != null && !newMoveAction.tileToMoveTo.Equals (unitManager.currentTurnUnit.currentTile)) {
					actionSequence [1] = newMoveAction;
				}
				else {
					actionSequence [1] = null;
				}
			}
		} else {
			actionSequence [0] = new AIMoveAction (FindPointToMoveTo ());
		}

#if UNITY_EDITOR
		if (DebugSettings.debugAIManager) {
			Debug.Log ("AI Will be doing a " + actionSequence [0] + ", and a " + actionSequence [1]);
		}
#endif

		ContinueAI (); // Decision making complete; execute first action
	}

	///<summary>
	/// Makes the AI Act; executing the next Action on its sequence
	///</summary>
	public void ContinueAI () {
		if (sequenceIndex != actionSequence.Length && actionSequence [sequenceIndex] != null) {
#if UNITY_EDITOR
			if (DebugSettings.debugAIManager) {
				if (actionSequence [sequenceIndex] is AIAbilityAction)
					Debug.Log ("-===== EXECUTING " + actionSequence [sequenceIndex] + ", Ability " + ((AIAbilityAction) actionSequence [sequenceIndex]).abilityToUse + "; at " + actionSequence [sequenceIndex].targetUnit.unit);
				if (actionSequence [sequenceIndex] is AIAttackAction)
					Debug.Log ("-===== EXECUTING " + actionSequence [sequenceIndex] + ", on " + actionSequence [sequenceIndex].targetUnit.unit);
				if (actionSequence [sequenceIndex] is AIMoveAction)
					Debug.Log ("-===== EXECUTING " + actionSequence [sequenceIndex] + ", to tile " + actionSequence [sequenceIndex].tileToMoveTo + " [" + actionSequence [sequenceIndex].tileToMoveTo.positionX + ", " + actionSequence [sequenceIndex].tileToMoveTo.positionY + "].");
			}
#endif
			sequenceIndex++;        // weird sequenceIndex++ order and -1 here are to prevent an endless loop; as the next Act call is made within the same frame
									// This shouldn't be needed once waits and animations are implemented; still do checks though.

			actionSequence [sequenceIndex-1].Act (unitManager, unitManager.currentTurnUnit);
		}
		else {
			EndAI ();
		}
	}

	// Resets all the AI components back to null (for safety); and calls the UnitManager to end the turn
	private void EndAI () {
		friendlyUnits.Clear ();
		enemyUnits.Clear ();
		actionsToConsider.Clear ();

		actionSequence [0] = null;
		actionSequence [1] = null;
		sequenceIndex = 0;

#if UNITY_EDITOR
		if (DebugSettings.debugAIManager) {
			Debug.Log ("AI for unit " + unitManager.currentTurnUnit + " has finished.");
			Debug.Log ("______________________________________");
		}
#endif

		unitManager.EndTurn ();
	}

	private void AnalyseSelf () {
		// Set a OtherUnit reference to self
		// Check for the longest-range ability (hostile and beneficial) we have

		// Check if the unit has self-cast abilities
		// check if the unit has beneficial abilities		// Is this necessary? Is it a benefit to check this now so we can potentially skip later, or just run through the loops normally
		// check if the unit has offencive abilities		// Might be worth doing at the start of combat, and save them individually per unit
	}

	// Checks for each unit to see if it could potentially be reached, and if so adds to the lists.
	private void ScanForUnits () {
		//@todo Unit scan doesn't consider "faction" yet, in case you want to use it for player-friendly units as well
		Unit currentUnit = unitManager.currentTurnUnit;

		int maxDistance = currentUnit.unitStats.movementDistance + 6;   //@todo Replace the +6 with the longest-range ability the unit has

		// Getting Friendly units
		//@todo only do so if the current AI unit has beneficial spells that can be cast on allies; do so during the 
		foreach (Unit attemptUnit in unitManager.tempEnemyUnitList) {
			if (currentUnit.Equals (attemptUnit)) { // If the unit is itself
				self = new OtherUnit (attemptUnit, 0, null, true);
				friendlyUnits.Add (self);
				continue;
			}

			int unitDistance = LandTileMap.instance.GetTileDistance (currentUnit.currentTile, attemptUnit.currentTile);
			if (maxDistance >= unitDistance) {
				OtherUnit otherUnit;

				LandTile [] pathTowards = LandTileMap.instance.RequestPath (currentUnit.currentTile, attemptUnit.currentTile, currentUnit.unitStats.canMoveDiagonally, false);
				if (pathTowards.Length != 0 && pathTowards.Length <= currentUnit.unitStats.movementDistance) {
					otherUnit = new OtherUnit (attemptUnit, unitDistance, pathTowards, true);
				}
				else {      // No path or out of our movement range means there's no walkable path
					otherUnit = new OtherUnit (attemptUnit, unitDistance, pathTowards);
				}

				friendlyUnits.Add (otherUnit);
			}
		}

		// Getting Hostile units
		foreach (Unit attemptUnit in unitManager.tempFriendlyUnitList) {
			int unitDistance = LandTileMap.instance.GetTileDistance (currentUnit.currentTile, attemptUnit.currentTile);
			if (maxDistance >= unitDistance) {
				OtherUnit otherUnit;

				LandTile [] pathTowards = LandTileMap.instance.RequestPath (currentUnit.currentTile, attemptUnit.currentTile, currentUnit.unitStats.canMoveDiagonally, false);
				if (pathTowards.Length != 0 && pathTowards.Length <= currentUnit.unitStats.movementDistance) {
					otherUnit = new OtherUnit (attemptUnit, unitDistance, pathTowards, true);
				}
				else {      // No path or out of our movement range means there's no walkable path
					otherUnit = new OtherUnit (attemptUnit, unitDistance, pathTowards);
				}

				enemyUnits.Add (otherUnit);
			}
		}
	}

	// For each unit, loop over potentially usable abilities, add those to the considered actions
	private void ScanForActions () {
		Unit currentUnit = unitManager.currentTurnUnit;

		//@todo check for list of self-only cast spells once implemented

		// Checking for Regular attacks
		foreach (OtherUnit enemy in enemyUnits) {
			int unitDistance = LandTileMap.instance.GetTileDistance (currentUnit.currentTile, enemy.unit.currentTile);

			// Checking for Normal attacks
			if (unitDistance <= currentUnit.unitStats.movementDistance + currentUnit.unitStats.attackRange) {
				int score = currentUnit.unitStats.attackPower - enemy.unit.unitStats.defence;
				//@todo Some form of defense/evasion/counter calculation
				//@todo more hit chance when attacking from behind (once implemented); maybe (if melee) add each direction as a seperate attack
				//@todo check for weapon damage, and special effects on attack (once implemented)
				//@note maybe rather than doing all the pathfinding here, do so when the action is getting checked/tried out
				//@note Rather than guarenteed pathfinding runs, it'll only check the top scoring one(s) until one succeeds
				if (unitDistance <= currentUnit.unitStats.attackRange) {        // Already in attack range
					AIAttackAction newAction = new AIAttackAction (enemy, score);
					actionsToConsider.Add (newAction);
				}
				else {                                                          // Has to move first
					if (enemy.pathToTarget != null && enemy.pathToTarget.Length != 0) {
						AIAttackAction newAction = new AIAttackAction (enemy, score, enemy.pathToTarget [enemy.pathToTarget.Length - Mathf.Clamp (currentUnit.unitStats.attackRange, 1, enemy.pathToTarget.Length - 1)]);
						actionsToConsider.Add (newAction);
					}
				}
			}

			// Checking for offensive Abilities
			foreach (Ability ability in currentUnit.unitAbilityList) {
				if (ability.AI_DontUseThis) continue;
				if (ability.AI_isBeneficial) continue;
				if (ability.manaCost > currentUnit.unitStats.currentMana) continue;
				if (unitDistance > currentUnit.unitStats.movementDistance + ability.range) continue;

				//@todo find way of handling AoE spells and multiple targets hit
				//@todo find way of handling casting times and target unit upcoming turns
				int score = ability.AI_GetAbilityScore () + currentUnit.unitStats.intelligence - enemy.unit.unitStats.resistance;
				if (unitDistance <= ability.range) {
					AIAbilityAction newAction = new AIAbilityAction (enemy, ability, score);
					actionsToConsider.Add (newAction);
				}
				else {
					if (enemy.pathToTarget != null && enemy.pathToTarget.Length != 0) {
						AIAbilityAction newAction = new AIAbilityAction (enemy, ability, score, enemy.pathToTarget [enemy.pathToTarget.Length - Mathf.Clamp (ability.range, 1, enemy.pathToTarget.Length)]);
						actionsToConsider.Add (newAction);
					}
				}
			}
		}

		foreach (OtherUnit ally in friendlyUnits) {
			int unitDistance = LandTileMap.instance.GetTileDistance (currentUnit.currentTile, ally.unit.currentTile);

			// Checking for Beneficial Abilities
			//@todo Can currently decide to heal at maximum health when no other option is availible
			foreach (Ability ability in currentUnit.unitAbilityList) {
				if (ability.AI_DontUseThis) continue;
				if (!ability.AI_isBeneficial) continue;
				if (ability.manaCost > currentUnit.unitStats.currentMana) continue;
				if (unitDistance > currentUnit.unitStats.movementDistance + ability.range) continue;

				//@todo find way of handling AoE spells and multiple targets hit
				//@todo find way of handling casting times and target unit upcoming turns
				int score = Mathf.Clamp (ability.AI_GetAbilityScore (), 0, currentUnit.unitStats.maxHealth - currentUnit.unitStats.currentHealth);
				// Healing score is the potential healing clamped between 0 and missing health.
				if (unitDistance <= ability.range) {
					AIAbilityAction newAction = new AIAbilityAction (ally, ability, score);
					actionsToConsider.Add (newAction);
				}
				else {
					if (ally.pathToTarget != null && ally.pathToTarget.Length != 0) {
						AIAbilityAction newAction = new AIAbilityAction (ally, ability, score, ally.pathToTarget [ally.pathToTarget.Length - Mathf.Clamp (ability.range, 1, ally.pathToTarget.Length - 1)]);
						actionsToConsider.Add (newAction);
					}
				}
			}
		}
	}

	private LandTile FindPointToMoveTo () {
		//@todo Does adding a "is being threatened" tag to tiles improve this performance-wise?
		//@todo Has the option of returning its current position

		Unit currentUnit = unitManager.currentTurnUnit;
		LandTile [] possibleTiles = LandTileMap.instance.RequestRange (currentUnit.transform.position, currentUnit.unitStats.movementDistance, currentUnit.unitStats.canMoveDiagonally, true, false, false);
		if (possibleTiles.Length == 0) {
			return null;
		}

		LandTile tileToMoveTo = null;

		if (enemyUnits.Count != 0) {
			// Am I ranged/wizard? Move away from closest target
			//@todo Find way of recognizing spellcasters/healers etc. Could be class-based, but not preferable. If Intelligence>Attack?
			if (currentUnit.unitStats.attackRange > 1 || currentUnit.unitStats.intelligence > currentUnit.unitStats.attackPower) {
				int [] tileScores = new int [possibleTiles.Length];
				int lowestValue = 9999;
				int lowestIndex = 0;

				for (int i = 0; i < possibleTiles.Length; i++) {    // Calculates the 'safest' spot by adding up the tile's distance to each enemy and picking the lowest
					foreach (OtherUnit enemy in enemyUnits) {       //@todo Better ways to do this?
						tileScores [i] += LandTileMap.instance.GetTileDistance (currentUnit.currentTile, enemy.unit.currentTile);
						if (tileScores [i] < lowestValue) {
							lowestValue = tileScores [i];
							lowestIndex = i;
						}
					}
				}

				return tileToMoveTo = possibleTiles [lowestIndex];
			}

			int lowestHealth = 9999;
			OtherUnit lowestHealthEnemy = null;
			// Scan for lowest health enemy, move towards that one
			foreach (OtherUnit enemy in enemyUnits) {
				if (enemy.canReach && enemy.unit.unitStats.currentHealth < lowestHealth) {
					LandTile testingTile = enemy.pathToTarget [Mathf.Clamp (enemy.pathToTarget.Length - 2, 0, enemy.pathToTarget.Length-1)];
					if ((possibleTiles.Contains<LandTile> (testingTile) || currentUnit.currentTile == testingTile) && testingTile.unitOnTile== null) {
						lowestHealthEnemy = enemy;
						lowestHealth = enemy.unit.unitStats.currentHealth;
					} 
				}
			}
			if (lowestHealthEnemy != null) {
				if (LandTileMap.instance.GetTileDistance (currentUnit.currentTile, lowestHealthEnemy.unit.currentTile) < currentUnit.unitStats.attackRange) {
					return null;	// if we're already in range of the lowest health unit; we don't have to move.
				} else {
					return tileToMoveTo = lowestHealthEnemy.pathToTarget [Mathf.Clamp (lowestHealthEnemy.pathToTarget.Length - 2, 0, lowestHealthEnemy.pathToTarget.Length)];
				}
			}

			// If there were no possible enemies to path to, move in the general direction of one
			//@todo doesn't consider movement costs yet
			if (tileToMoveTo == null) {
				for (int i = 0; i < enemyUnits.Count - 1; i++) {
					int random = Random.Range (i, enemyUnits.Count);
					if (enemyUnits [random].pathToTarget != null && enemyUnits[random].pathToTarget.Length != 0) {
						tileToMoveTo = enemyUnits [random].pathToTarget [Mathf.Clamp (currentUnit.unitStats.movementDistance, 0, enemyUnits [random].pathToTarget.Length - 1)];

						break;
					}
				}
			}
		}

		// If all else fails, pick a random tile within movement range
		if (tileToMoveTo == null) {
			tileToMoveTo = possibleTiles [Random.Range (0, possibleTiles.Length - 1)];
		}

		return tileToMoveTo;
	}

}

public class OtherUnit {
	public Unit unit;
	public int distance;
	public bool canReach;
	public LandTile [] pathToTarget;

	public OtherUnit (Unit unit, int distance, LandTile[] pathToTarget, bool canReach = false) {
		this.unit = unit;
		this.distance = distance;
		this.pathToTarget = pathToTarget;
		this.canReach = canReach;
	}
}
