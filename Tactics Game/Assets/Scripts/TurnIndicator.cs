using UnityEngine;

public class TurnIndicator : MonoBehaviour {

	// TurnIndicator is attatched to the Unit that has the active turn; and creates a spinnnig circle around it.
	// In short: visual turn indicator

	[SerializeField] private float rotationSpeed = 1f;

	private void Update () {
		this.transform.Rotate (0, rotationSpeed, 0, Space.Self);
	}

}
