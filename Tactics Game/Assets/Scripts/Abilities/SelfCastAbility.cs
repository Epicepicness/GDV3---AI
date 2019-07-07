using UnityEngine;

[CreateAssetMenu (fileName = "Ability", menuName = "Ability/Self-cast Ability")]
public class SelfCastAbility : Ability {

	[Header ("Effect")]
	[Tooltip ("The damage done of the ability on cast; use negative numbers for healing.")]
	public int damageDone;
	[Tooltip ("Temporary until proper buff/status system is implemented.")]
	public int attackBonus;


	public override void OnAbilitySelection () {
		base.OnAbilitySelection ();

		UnitManager unitManager = GameManager.instance.unitManager;

		if (damageDone != 0)
			unitManager.currentTurnUnit.TakeDamage (damageDone);

		unitManager.currentTurnUnit.unitStats.attackPower += 5;
	}

	public override int AI_GetAbilityScore () {
		return damageDone + attackBonus;
	}


}
