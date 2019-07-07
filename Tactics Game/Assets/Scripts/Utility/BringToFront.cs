using UnityEngine;

	//Brings a gameobject to the back of the hierarchy when enabled; meaning it will be drawn last / on top.

public class BringToFront : MonoBehaviour {

	private void OnEnable () {
		transform.SetAsLastSibling ();
	}

}
