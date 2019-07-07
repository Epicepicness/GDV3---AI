using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour {

	private static UIManager _instance = null;              //Static instance of UIManager which allows it to be accessed by any other script.
	public static UIManager instance { get => _instance; }

	private UnitManager unitManager;

#pragma warning disable CS0649
	[SerializeField] private GameObject selectedUnitCanvas;

	// Selected Frame Components (health/mana frame, the act buttons (move, attack, etc), and the ability list stuff).
	[Header ("Selection Frame Components")]
	[SerializeField] private GameObject actionButtonFrame;		// The Frame containing the action buttons (move, attack, etc).
	[SerializeField] private GameObject abilityListFrame;		// The Frame containing abilities (Throw rock, fire, heal, etc).
	[SerializeField] private Transform abilityGridParent;		// The Scrollable grid that the ability buttons will be parented to.
	[SerializeField] private GameObject abilityButtonPrefab;	// Reference to the initial ability button that others will be cloned off of.
	[SerializeField] private Image movementButtonImage;			// The image on the movement button; gets darkened if already moved.
	[SerializeField] private Image attackButtonImage;           // The image on the attack button; gets darkened if already acted.
	[SerializeField] private Image abilityButtonImage;          // The image on the ability button; gets darkened if already acted.
	[SerializeField] private Image unitAvatar;					// The unit avatar on the Unit frame.
	[SerializeField] private Text unitLevel;                    // The unit's level on the Unit frame.
	[SerializeField] private Text unitHealth;                   // The unit's health on the Unit frame.
	[SerializeField] private Text unitMana;						// The unit's mana on the Unit frame.
	[SerializeField] private Text unitTurnTimer;                // The unit's turn timer on the Unit frame.

	// Status Screen Components (all stuff related to the in-combat unit status screen
	[Header ("Status Screen Texts")]
	[SerializeField] private GameObject statusScreenFrame;		// The Status Screen frame (all unit stats, shown on "status" button).
	[SerializeField] private Text className;
	[SerializeField] private Text movementAmount;
	[SerializeField] private Text jumpHeight;
	[SerializeField] private Text speedRating;
	[SerializeField] private Text attackPower;
	[SerializeField] private Text magicPower;
	[SerializeField] private Text defenceRating;
	[SerializeField] private Text resistanceRating;
	[SerializeField] private Text hitRating;

	private List<GameObject> abilityButtonsList = new List<GameObject> ();	// List of buttons in the ability list.
	private Unit lastShownUnit;                                             // last unit; so the ability list doesn't have to reload if unncessary.

	[SerializeField] private GameObject victoryScreen;
	[SerializeField] private GameObject defeatScreen;

	[SerializeField] private Text tempTurnIndicator;
#pragma warning restore CS0649


	private void Awake () {
		if (_instance == null)
			_instance = this;
		else if (_instance != this)
			Destroy (this.gameObject);
		DontDestroyOnLoad (this.gameObject);

		unitManager = GameManager.instance.unitManager;
		abilityButtonsList.Add (abilityButtonPrefab);	// The ability list starts with 1 button (Also the prefab template for other buttons)
	}

	public void TempTurnIndicator (string order) {
		tempTurnIndicator.text = order;
	}

	public void ShowVictoryScreen () {
		DeselectUnit ();
		victoryScreen.SetActive (true);
	}
	public void ShowDefeatScreen () {
		DeselectUnit ();
		defeatScreen.SetActive (true);
	}


#region  //------ Combat Unit Frame ----------------------------------------------------------------------------------------------
	///<summary>
	/// Selects a unit on the combat map; showing it's health frame; and either status or combat buttons. If no argument is given it will select the current-turn unit
	///</summary>
	public void SelectCurrentUnit () {
		Unit selectedUnit = unitManager.currentTurnUnit;

		GameManager.instance.SetModeInputMenu ();
		CameraManager.instance.LookAtObject ( selectedUnit.transform);

		ShowUnitFrame (selectedUnit);
		actionButtonFrame.SetActive (true);        //Only show the action buttons if it's the current unit's turn
		movementButtonImage.color = new Color (movementButtonImage.color.r, movementButtonImage.color.g, movementButtonImage.color.b, (unitManager.unitHasMoved) ? 0.5f : 1f);
		attackButtonImage.color = new Color (attackButtonImage.color.r, attackButtonImage.color.g, attackButtonImage.color.b, (unitManager.unitHasActed) ? 0.5f : 1f);
		abilityButtonImage.color = new Color (attackButtonImage.color.r, attackButtonImage.color.g, attackButtonImage.color.b, (unitManager.unitHasActed) ? 0.5f : 1f);
	}
	///<summary>
	/// Selects a unit on the combat map; showing it's health frame; and either status or combat buttons (depending if it's the unit's turn).
	///</summary>
	public void SelectSpecificUnit (Unit selectedUnit) {
		GameManager.instance.SetModeInputMenu ();
		CameraManager.instance.LookAtObject (selectedUnit.transform);

		ShowUnitFrame (selectedUnit);
		bool isCurrentTurnUnit = (selectedUnit == unitManager.currentTurnUnit);
		if (isCurrentTurnUnit) {
			actionButtonFrame.SetActive (true);        //Only show the action buttons if it's the current unit's turn
			movementButtonImage.color = new Color (movementButtonImage.color.r, movementButtonImage.color.g, movementButtonImage.color.b, (unitManager.unitHasMoved) ? 0.5f : 1f);
			attackButtonImage.color = new Color (attackButtonImage.color.r, attackButtonImage.color.g, attackButtonImage.color.b, (unitManager.unitHasActed) ? 0.5f : 1f);
			abilityButtonImage.color = new Color (attackButtonImage.color.r, attackButtonImage.color.g, attackButtonImage.color.b, (unitManager.unitHasActed) ? 0.5f : 1f);
		}
		else {
			className.text = selectedUnit.unitStats.className;
			movementAmount.text = selectedUnit.unitStats.movementDistance.ToString ();
			jumpHeight.text = selectedUnit.unitStats.jumpHeight.ToString ();
			speedRating.text = selectedUnit.unitStats.speedRating.ToString ();
			attackPower.text = selectedUnit.unitStats.attackPower.ToString ();
			magicPower.text = selectedUnit.unitStats.intelligence.ToString ();
			defenceRating.text = selectedUnit.unitStats.defence.ToString ();
			resistanceRating.text = selectedUnit.unitStats.resistance.ToString ();
			hitRating.text = selectedUnit.unitStats.hitRating.ToString ();
			statusScreenFrame.SetActive (true);
		}
	}
	///<summary>
	/// Deselects a unit; hiding all unit-related UI elements.
	///</summary>
	public void DeselectUnit () {
		selectedUnitCanvas.SetActive (false);
		actionButtonFrame.SetActive (false);
		abilityListFrame.SetActive (false);
		statusScreenFrame.SetActive (false);
	}
	///<summary>
	/// Shows a units Unit Frame UI (containing health, mana, level, name, etc).
	///</summary>
	public void ShowUnitFrame (Unit selectedUnit) {
		if (selectedUnit.unitAvatar != null)
			unitAvatar.sprite = selectedUnit.unitAvatar;
		unitHealth.text = ("Level: " + selectedUnit.level);
		unitHealth.text = ("Health: " + selectedUnit.unitStats.maxHealth + "/" + selectedUnit.unitStats.currentHealth);
		unitMana.text = ("Mana: " + selectedUnit.unitStats.maxMana + "/" + selectedUnit.unitStats.currentMana);
		unitTurnTimer.text = ("not yet implemented");

		selectedUnitCanvas.SetActive (true);
	}
	public void ShowAbilityList () {
		if (unitManager.currentTurnUnit.unitAbilityList.Length == 0)
			return;
		if (lastShownUnit == unitManager.currentTurnUnit) {
			abilityListFrame.SetActive (true);
			return;
		}

		int i = 0;
		while (unitManager.currentTurnUnit.unitAbilityList.Length > i) {
			if (abilityButtonsList.Count > i) {         // If there's a button ready/availible, set it to the correct ability.
				Text t = abilityButtonsList [i].GetComponentInChildren<Text> ();
				t.text = unitManager.currentTurnUnit.unitAbilityList[i].abilityName;

				Button b = abilityButtonsList [i].GetComponent<Button> ();
				b.onClick.RemoveAllListeners ();
				int i2 = i;
				b.onClick.AddListener (DeselectUnit);
				b.onClick.AddListener (delegate { unitManager.currentTurnUnit.unitAbilityList [i2].OnAbilitySelection (); });

				abilityButtonsList [i].SetActive (true);
			} else {                                    // If there's no button ready/availible, create a new one and set it up.
				GameObject newButton = Instantiate (abilityButtonPrefab, Vector3.zero, Quaternion.identity, abilityGridParent);
				abilityButtonsList.Add (newButton);

				Text t = newButton.GetComponentInChildren<Text> ();
				t.text = unitManager.currentTurnUnit.unitAbilityList [i].abilityName;

				Button b = newButton.GetComponent<Button> ();
				b.onClick.RemoveAllListeners ();
				int i2 = i;
				b.onClick.AddListener (DeselectUnit);
				b.onClick.AddListener (delegate { unitManager.currentTurnUnit.unitAbilityList [i2].OnAbilitySelection (); });

				newButton.SetActive (true);
			}
			i++;
		}
		while (abilityButtonsList.Count > i) {
			abilityButtonsList [i].SetActive (false);	// If there's more buttons than abilities, set the remaining inactive.
			i++;
		}

		// After everything is set up correctly; activate the correct frames etc.
		abilityListFrame.SetActive (true);
	}
#endregion

#region  //------ Combat Buttons ----------------------------------------------------------------------------------------------
	public void MovementButton () {     // A unit's "move" button in combat
		if (!unitManager.unitHasMoved)
			DeselectUnit ();
			unitManager.ShowMovementIndicators ();
	}

	public void AttackButton () {       // A unit's "attack" button in combat
		if (!unitManager.unitHasActed)
			DeselectUnit ();
			unitManager.ShowAttackIndicators ();
	}

	public void AbilityButton () {       // A unit's "attack" button in combat
		if (!unitManager.unitHasActed) {
			ShowAbilityList ();
		}
	}

	public void WaitButton () {         // A unit's "wait" button in combat
		DeselectUnit ();
		unitManager.EndTurn ();
	}
	public void ReloadSceneButton () {
		victoryScreen.SetActive (false);
		defeatScreen.SetActive (false);
		GameManager.instance.ReloadScene ();
	}

	public void StatusButton () {       // A unit's "Status" button in combat
		if (statusScreenFrame.activeInHierarchy) {
			statusScreenFrame.SetActive (false);
		} else {
			Unit selectedUnit = unitManager.currentTurnUnit;
			className.text = selectedUnit.unitStats.className;
			movementAmount.text = selectedUnit.unitStats.movementDistance.ToString ();
			jumpHeight.text = selectedUnit.unitStats.jumpHeight.ToString ();
			speedRating.text = selectedUnit.unitStats.speedRating.ToString ();
			attackPower.text = selectedUnit.unitStats.attackPower.ToString ();
			magicPower.text = selectedUnit.unitStats.intelligence.ToString ();
			defenceRating.text = selectedUnit.unitStats.defence.ToString ();
			resistanceRating.text = selectedUnit.unitStats.resistance.ToString ();
			hitRating.text = selectedUnit.unitStats.hitRating.ToString ();
			statusScreenFrame.SetActive (true);
		}
	}
#endregion

}
