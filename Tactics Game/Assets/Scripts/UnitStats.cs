
[System.Serializable]
public class UnitStats {

	public string className = "class";		// Temporary until actual class system is implemented
	public bool canMoveDiagonally = false;
	public int attackRange = 1;

	public int movementDistance = 4;
	public int jumpHeight = 3;

	public int maxHealth = 100;
	public int currentHealth = 100;
	public int maxMana = 50;
	public int currentMana = 50;

	public int attackPower = 15;
	public int intelligence = 10;

	public int defence = 15;
	public int resistance = 15;

	public int hitRating = 15;
	public int speedRating = 15;

	public void SetupStats () {
		currentHealth = maxHealth;
		currentMana = maxMana;
	}

}
