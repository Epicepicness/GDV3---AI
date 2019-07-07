using UnityEngine;

[CreateAssetMenu (fileName = "Ability", menuName = "Ability/Tile-Targetting Ability")]
public class GeneralUnitAffectingAbility : Ability {

	[Header ("Effect")]
	[Tooltip ("The damage done of the ability on cast; use negative numbers for healing.")]
	public int damageDone;


	// Mogelijk voor zorgen dat Ability's alleen de UnitManager hoeven te kennen, en dat de Unitmanager alle Landtile-related dingen doet.
	// Aka: OnAbilityCast moet een Unit[] of een Landtile door krijgen (hoe bepalen we welke?)
	//	 Mogelijk dat we apparte scripts moeten maken voor LandTile en Unit !>affecting<! spells; Beide targetten tiles, maar affecten verschillend.
	// Aanroepen van RequestRange, OnTargetRangeFound, etc moeten allemaal naar UnitManager.

	// OnAbilitySelection is called when the ability is selected from the UI (UIManager) by the player
	// This will need some way of being called from other places as well later on? Special triggers etc.
	public override void OnAbilitySelection () {
		base.OnAbilitySelection ();

		//@todo find better way of making this reference.
		UnitManager unitManager = GameManager.instance.unitManager;

		if (range == 0) {   // target own position automatically, no cursor required
			unitManager.BeginCastingAbility (unitManager.currentTurnUnit, unitManager.currentTurnUnit.currentTile, this);

			return;
		}
		unitManager.ShowAbilityRange (this, true);
	}

	// OnAbilityCast is called from UnitManager, and is the actual casting of the ability (either instantly, or when the CombatAction Turn arrives).
	public override void OnAbilityCast (Unit [] affectedUnits) {
		Debug.Log (abilityName + " being cast, units affected: " + affectedUnits.Length);
		foreach (Unit u in affectedUnits) {
			AbilityEffect (u);
		}
	}

	public override int AI_GetAbilityScore () {
		return damageDone;
	}

	private void AbilityEffect (Unit unit) {
		if (damageDone != 0)
			unit.TakeDamage (damageDone);
	}

}
