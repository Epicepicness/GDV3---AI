using UnityEngine;

public class InputManager : MonoBehaviour {

	public static PlayerInput Current;

	private void Start () {
		Current = new PlayerInput ();
	}

	private void Update () {
		Vector2 directionalInput = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical"));

		bool horizontalDirectionalInput = Input.GetButtonDown ("Horizontal");
		bool verticleDirectionalInput = Input.GetButtonDown ("Vertical");

		bool confirmationKey = Input.GetButtonDown ("Confirm");
		bool cancelKey = Input.GetButtonDown ("Cancel");
		bool menuKey = Input.GetButtonDown ("Menu");

		Current = new PlayerInput () {
			DirectionalInput = directionalInput,
			HorizontalDirectionalInput = horizontalDirectionalInput,
			VerticleDirectionalInput = verticleDirectionalInput,
			ConfirmationKey = confirmationKey,
			CancelKey = cancelKey,
			MenuKey = menuKey,
		};
	}

}

public struct PlayerInput {
	public Vector2 DirectionalInput;
	public bool HorizontalDirectionalInput;
	public bool VerticleDirectionalInput;
	public bool ConfirmationKey;
	public bool CancelKey;
	public bool MenuKey;
}
