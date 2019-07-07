using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour {

	public static CameraManager instance = null;                //Static instance of CameraManager which allows it to be accessed by any other script.

	//Static Isometric Values
	private Vector3 cameraOffset = new Vector3 (0, 20f, 0);     //The offset from the camera's target.
	private Vector3 isometricForward = new Vector3 (0.6f, 0, 0.6f);//Forward direction from an Isometric view.
	private Vector3 isometricRight = new Vector3 (0.6f, 0, -0.6f);//Right direction from an Isometric view.
	private float cameraHeight = 20f;                           //Saves the Y value the camera should be above its target (don't use transform height, that changes).
																//Remember to Adjust this eventually to tile height (?)
	private float panningSpeed = 1f;
	private CameraStates cameraState;

	private Camera gameCamera;                                  //The game camera
	private Transform cameraTarget;                             //The target of the camera during the TargetObject state.

	private enum CameraStates {
		Stationary, FollowObject, Panning
	}


	private void Awake () {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy (gameObject);
		DontDestroyOnLoad (gameObject);

		SetupSceneCamera ();
	}

	private void Update () {
		if (cameraState == CameraStates.Stationary)
			return;
		if (cameraState == CameraStates.Panning) {
			gameCamera.transform.position = Vector3.Lerp (gameCamera.transform.position, cameraTarget.position + cameraOffset, panningSpeed * Time.deltaTime);

			if (Vector3.Distance (gameCamera.transform.position, cameraTarget.position + cameraOffset) <= 0.5) {
				gameCamera.transform.position = cameraTarget.position + cameraOffset;
				cameraState = CameraStates.Stationary;
			}
			return;
		}
		if (cameraState == CameraStates.FollowObject) {
			gameCamera.transform.position = cameraTarget.position + cameraOffset;
		}
	}


	public void SetupSceneCamera () {
		gameCamera = GetComponentInChildren<Camera> ();
		cameraState = CameraStates.Stationary;
	}

	//Calculates the X and Z offsets required to put a targetted object in the centre of the view using the current camera height and angles.
	private void CalculateOffset () {
		float angleXRad = (90 - gameCamera.transform.rotation.eulerAngles.x) * Mathf.PI / 180;
		float angleYRad = (90 - gameCamera.transform.rotation.eulerAngles.y) * Mathf.PI / 180;

		float diagnalDistance = Mathf.Tan (angleXRad) * cameraHeight;
		float xOffset = Mathf.Sin (angleYRad) * diagnalDistance;
		float zOffset = Mathf.Cos (angleYRad) * diagnalDistance;

		cameraOffset = new Vector3 (-xOffset, cameraHeight + 1, -zOffset);
	}

#region    //------ Camera State Functions ----------------------------------------------------------------------------------------------
	public void StartCameraPan (Transform cameraTarget, float panDuration) {
		this.cameraTarget = cameraTarget;
		this.panningSpeed = Vector3.Distance (gameCamera.transform.position, cameraTarget.position + cameraOffset) / panDuration;
		cameraState = CameraStates.Panning;
	}
	public void StartCameraPan (Transform cameraTarget) {
		this.cameraTarget = cameraTarget;
		this.panningSpeed = 1f;
		cameraState = CameraStates.Panning;
	}

	public void FollowObject (Transform cameraTarget) {
		CalculateOffset ();
		this.cameraTarget = cameraTarget;
		cameraState = CameraStates.FollowObject;
	}
	public void StopFollowingObject () {
		cameraState = CameraStates.Stationary;
	}

	public void LookAtObject (Transform cameraTarget) {
		gameCamera.transform.position = cameraTarget.position + cameraOffset;
		cameraState = CameraStates.Stationary;
	}
#endregion

#region    //------ Random Camera Options ----------------------------------------------------------------------------------------------
	///<summary>
	/// Changes the zoom level of the camera. If no value for zoomDuration is given, it will be done instantly.
	///</summary>
	public void ChangeCameraZoom (float zoomAmount, float zoomDuration) {
		StartCoroutine (ChangeCameraZoomOverTime (zoomAmount, zoomDuration));
	}
	public void ChangeCameraZoom (float zoomAmount) {
		gameCamera.orthographicSize = zoomAmount;
	}

	private IEnumerator ChangeCameraZoomOverTime (float zoomAmount, float zoomDuration) {
		for (float t = 0; t < zoomDuration; t += Time.deltaTime) {
			gameCamera.orthographicSize = Mathf.Lerp (gameCamera.orthographicSize, zoomAmount, t / zoomDuration);
			yield return null;
		}
		gameCamera.orthographicSize = zoomAmount;
	}

	///<summary>
	/// Tilts the camera to the given angle. If no value for rotateTime is given, it will be done instantly. If no direction is given it goes right by default.
	///</summary>
	public void RotateCamera (float angle, float rotateTime, bool goRight) {
		StartCoroutine (RotateCameraOverTime (angle, rotateTime, goRight));
	}
	public void RotateCamera (float angle, float rotateTime) {
		StartCoroutine (RotateCameraOverTime (angle, rotateTime, true));
	}
	public void RotateCamera (float angle) {
		Quaternion r = Quaternion.Euler (angle, 30, 45);
		transform.rotation = r;
	}

	IEnumerator RotateCameraOverTime (float angle, float rotateTime, bool goRight) {
		Quaternion fromAngle = gameCamera.transform.rotation;
		Quaternion targetAngle = Quaternion.Euler (30, 45, angle);

		for (float t = 0; t < rotateTime; t += Time.deltaTime) {
			gameCamera.transform.rotation = Quaternion.Lerp (fromAngle, targetAngle, t / rotateTime);
			yield return null;
		}
		gameCamera.transform.rotation = targetAngle;
	}
#endregion

}
