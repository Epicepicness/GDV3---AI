using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities;

public class GameManager : MonoBehaviour {

	private static GameManager _instance = null;              //Static instance of GameManager which allows it to be accessed by any other script.
	public static GameManager instance { get { return _instance; } }

	[HideInInspector] public Camera mainCamera;             //Reference to current Camera.
	[HideInInspector] public InputManager inputManager;     //Reference to InputManager component.
	[HideInInspector] public UIManager uiManager;           //Reference to UIManager component.
	[HideInInspector] public SoundManager soundManager;   //Reference to SoundManager Script.
	[HideInInspector] public CameraManager cameraManager; //Reference to CameraManager Script.
	[HideInInspector] public UnitManager unitManager; //Reference to CameraManager Script.

	public static GameStates gameState;
	public SceneField theOneScene;
#pragma warning disable CS0649
	[SerializeField] private GameObject TemporaryStatIndicator;
#pragma warning restore CS0649


	private void Awake () {
		if (_instance == null)
			_instance = this;
		else if (_instance != this)
			Destroy (this.gameObject);
		DontDestroyOnLoad (this.gameObject);

		inputManager = gameObject.GetComponent<InputManager> ();
		unitManager = gameObject.GetComponent<UnitManager> ();

		uiManager = (GameObject.Find ("UIManager")) ? GameObject.Find ("UIManager").GetComponent<UIManager> () :
			((GameObject) Instantiate (Resources.Load ("Prefabs/Managers/UIManager"), Vector3.zero, Quaternion.identity)).GetComponent<UIManager> ();
		soundManager = null;
		/*soundManager = (GameObject.Find ("SoundManager")) ? GameObject.Find ("SoundManager").GetComponent<SoundManager> () :
			((GameObject) Instantiate (Resources.Load ("Prefabs/Managers/SoundManager"), Vector3.zero, Quaternion.identity)).GetComponent<SoundManager> ();*/
		cameraManager = (GameObject.Find ("CameraManager")) ? GameObject.Find ("CameraManager").GetComponent<CameraManager> () :
			((GameObject) Instantiate (Resources.Load ("Prefabs/Managers/CameraManager"), Vector3.zero, Quaternion.identity)).GetComponent<CameraManager> ();
	}

	private void Start () {
		CameraManager.instance.SetupSceneCamera ();
	}


#region //------ Game State Functions ----------------------------------------------------------------------------------------------
	public void SetModeMainMenu () {
		if (TemporaryStatIndicator.gameObject != null)
			TemporaryStatIndicator.gameObject.GetComponent<Renderer> ().material.color = Color.black;
		gameState = GameStates.mainMenu;
	}
	public void SetModeInputMenu () {
		if (TemporaryStatIndicator.gameObject != null)
			TemporaryStatIndicator.gameObject.GetComponent<Renderer> ().material.color = Color.green;
		gameState = GameStates.waitingForInputMenu;
	}
	public void SetModeInputCursor () {
		if (TemporaryStatIndicator.gameObject != null)
			TemporaryStatIndicator.gameObject.GetComponent<Renderer> ().material.color = Color.blue;
		gameState = GameStates.waitingForInputCursor;
	}
	public void SetModeProcessing () {
		if (TemporaryStatIndicator.gameObject != null)
			TemporaryStatIndicator.gameObject.GetComponent<Renderer> ().material.color = Color.red;
		gameState = GameStates.processingActions;
	}

	public enum GameStates {
		mainMenu,
		waitingForInputMenu,
		waitingForInputCursor,
		processingActions
	}
#endregion

#region //------ SceneManagement Functions ----------------------------------------------------------------------------------------------
	public void LoadMainMenu () {
		//UIManager.instance.HideEverything ();
		//soundManager.PlayBackgroundMusic (mainMenu.backgroundSong);
		//LoadScene (mainMenu.scene);
	}

	public void ReloadScene () {
		LoadScene (theOneScene);
	}

	private void LoadScene (SceneField scene) {
		SceneManager.LoadScene (scene);
	}

	private void OnSceneChanged (Scene current, LoadSceneMode mode) {
		string sceneName = ""; // mainMenu.scene.SceneName.Substring (mainMenu.scene.SceneName.LastIndexOf ('/') + 1);
		if (SceneManager.GetActiveScene ().name != sceneName) {
			SetupSceneComponents ();
		}
		else {
			SetupMainMenuComponents ();
		}
	}

	// SetupSceneComponents sets up all the Managers, and unpauzes the game for gameplay.
	private void SetupSceneComponents () {

	}
	private void SetupMainMenuComponents () {

	}
#endregion

}

/*
 * Internal Logic:
 * -> GameManager holds overal state (doet nog niet zo veel tbh)
 * -> UnitManager creates/holds/manages the units (tie the TurnManager to this?)
 * -> LandTileMap holds the grid, and handles any grid/tile/range/pathfinding logic
 * -> UIManager manages anything UI related (including handling menu calls)
 * 
 * Combat states: 
 * -> Waiting for animations/movement
 * -> Having a menu opened up (attack/move menu; status menu; main menu)
 * -> Moving over grid with cursor
 *			- To just look around
 *			- To select a target tile (for movement/attack/spell)
 */

/*
 * Player input is only recieved with: 
 * -> Selecting in main menu					(part of UI (Unity), and UIManager)
 * -> Selecting in combat menu					(Part of UI (Unity), and UIManager)
 * -> Moving the grid cursor					(Part of LandTileMap)
 * -> Adjusting rotation at the end of a turn	(Part of UnitManager)
 */


/*
 * Todo List:
 * Pas je design aan; minder singletons, minder global access. Minder, minder, minder
 *			Je hebt er op het moment 7 ofzo
 *		Koppel turnManager aan GameManager
 *		Haal PathRequestManager weg, koppel aan LandTileMap
 *	Refactor alle code, zodat alles zo minimaal mogelijk editbaar is (gebruik get{} variableen etc).
 * Waarom zijn de pathfinder functions nog Coroutintes?
 *		Maak normale functions van zodat we gewoon een return hebben, en minstens 2 function calls uit de hele 'onRangeFound' spree kunnen halen
 * 
 *  - Maak de rotation-picker aan het einde van een turn
 *		Zorg ook dat je er uit kan cancelen als je nog een optie op je turn hebt
 * 
 * >AI stuff<
 * 
 * 
 */
