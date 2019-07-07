
public abstract class AIAction {
	public OtherUnit targetUnit;
	public bool requiresMovement = false;
	public LandTile tileToMoveTo;

	public int score;

	public virtual bool Try (Unit actingUnit) {
		return true;
	}

	public virtual void Act (UnitManager manager, Unit actingUnit) {
	}
}

public class AIAttackAction : AIAction {

	public AIAttackAction (OtherUnit targetUnit, int score, LandTile tileToMoveTo = null) {
		this.targetUnit = targetUnit;
		this.score = score;

		if (tileToMoveTo == null) return;
		this.tileToMoveTo = tileToMoveTo;
		requiresMovement = true;
	}

	public override bool Try (Unit actingUnit) {

		// There is a path to where we decided we need to move
		if (requiresMovement) {
			LandTile[] path = LandTileMap.instance.RequestPath (actingUnit.currentTile, tileToMoveTo, actingUnit.unitStats.canMoveDiagonally, false);
			if (path.Length == 0 && path.Length < actingUnit.unitStats.movementDistance) return false;
		}
		// Are we in attacking range?
		if (LandTileMap.instance.GetTileDistance ((requiresMovement) ? tileToMoveTo : actingUnit.currentTile, targetUnit.unit.currentTile) > actingUnit.unitStats.attackRange)
			return false;

		return true;	// Test successful
	}

	public override void Act (UnitManager manager, Unit actingUnit) {
		manager.AttackTile (targetUnit.unit.transform);
	}
}

public class AIAbilityAction : AIAction {
	public Ability abilityToUse;

	public AIAbilityAction (OtherUnit targetUnit, Ability abilityToUse, int score, LandTile tileToMoveTo = null) {
		this.targetUnit = targetUnit;
		this.abilityToUse = abilityToUse;
		this.score = score;

		if (tileToMoveTo == null) return;
		this.tileToMoveTo = tileToMoveTo;
		requiresMovement = true;
	}

	public override bool Try (Unit actingUnit) {

		// There is a path to where we decided we need to move
		if (requiresMovement) {
			LandTile [] path = LandTileMap.instance.RequestPath (actingUnit.currentTile, tileToMoveTo, actingUnit.unitStats.canMoveDiagonally, false);
			if (path.Length == 0 && path.Length < actingUnit.unitStats.movementDistance) return false;
		}
		// Are we in attacking range?
		if (LandTileMap.instance.GetTileDistance ((requiresMovement) ? tileToMoveTo : actingUnit.currentTile, targetUnit.unit.currentTile) > abilityToUse.range)
			return false;

		return true;    // Test successful
	}

	public override void Act (UnitManager manager, Unit actingUnit) {
		manager.BeginCastingAbility (actingUnit, targetUnit.unit.currentTile, abilityToUse);
	}
}

public class AIMoveAction : AIAction {

	public AIMoveAction (LandTile tileToMoveTo) {
		this.tileToMoveTo = tileToMoveTo;
	}

	public override bool Try (Unit actingUnit) {
		// There is a path to where we decided we need to move
		if (requiresMovement) {
			LandTile [] path = LandTileMap.instance.RequestPath (actingUnit.currentTile, tileToMoveTo, actingUnit.unitStats.canMoveDiagonally, false);
			if (path.Length == 0 && path.Length < actingUnit.unitStats.movementDistance) return false;
		}

		return true;    // Test successful
	}

	public override void Act (UnitManager manager, Unit actingUnit) {
		manager.RequestPath (tileToMoveTo.transform);
	}
}
