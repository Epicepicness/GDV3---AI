using UnityEngine;

public abstract class Ability : ScriptableObject {

	[Header ("Ability Description")]
	[Tooltip ("The in-game name for this ability.")]
	public string abilityName = "New Ability";
	[Tooltip ("The in-game tooltip for this ability.")]
	public string abilityTooltip;

	[Header("Costs")]
	[Tooltip ("Health lost to use this.")]
	public int healthCost = 0;
	[Tooltip ("Mana cost to use this.")]
	public int manaCost = 0;
	[Tooltip ("Similar to turns; an amount that gets filled with the unit's speed rating; castTime of 100 would take a full turn to cast.")]
	public int castTime = 0;

	[Header ("Use Restrictions")]
	[Tooltip ("Range in tiles; 0 means only cast on self. This is non-diagonally!")]
	public int range = 0;
	[Tooltip ("Size (in tiles) of the area that's affected. This is non-diagonally!")]
	public int areaSize = 1;
	[Tooltip ("Wether or not the ability can target Tiles that contain units.")]
	public bool requiresTarget = true;

	[Header ("AI-Related Variables")]
	[Tooltip ("Decides if the AI will ever consider using this ability, or completely ignore it.")]
	public bool AI_DontUseThis = false;
	[Tooltip ("Decides if the AI should use this as a beneficial or harmful ability. Or: 'should I use this on allies?'.")]
	public bool AI_isBeneficial = false;

	public virtual void OnAbilitySelection () {

	}
	public virtual void OnAbilityCast () {

	}
	public virtual void OnAbilityCast (LandTile tileToTarget) {

	}
	public virtual void OnAbilityCast (Unit[] unitsToAffect) {

	}
	public virtual int AI_GetAbilityScore () {
		return 0;
	}

}
