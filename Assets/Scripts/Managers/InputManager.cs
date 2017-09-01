using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : Singleton<InputManager>
{
	[Header("Camera")]
	public bool mouseMovesCamera = true;
	public Vector2 mouseDeadZone = new Vector2(.8f, .8f);
	public float keyboardSpeed = 4f;
	public float mouseSpeed = 2f;

	[Space]

	public LayerMask unitsLayerMask;
	public LayerMask enemiesLayerMask;

	private Camera mainCamera;
	private Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
	//private Vector3 initialSelectionWorldPos, currentSelectionWorldPos; //world coordinates //currently unused
	private Vector2 LMBDownMousePos, currentMousePos; //screen coordinates
	private Rect selectionRect; //screen coordinates
	private bool LMBClickedDown = false, boxSelectionInitiated = false;
	private float timeOfClick;

	private const float CLICK_TOLERANCE = .5f; //the player has this time to release the mouse button for it to be registered as a click

	private void Awake()
	{
		mainCamera = GameObject.FindObjectOfType<Camera>();

		#if !UNITY_EDITOR
		//to restore the mouseMovesCamera parameter (which in the player has to be always true)
		//in case someone forgot it on false in the Editor :)
		mouseMovesCamera = true;
		#endif
	}

	private void Update()
	{
		switch(GameManager.Instance.gameMode)
		{
			case GameManager.GameMode.Gameplay:
				currentMousePos = Input.mousePosition;
				
				//-------------- LEFT MOUSE BUTTON DOWN --------------
				if(Input.GetMouseButtonDown(0)) {
					LMBDownMousePos = currentMousePos;
					timeOfClick = Time.unscaledTime;
					LMBClickedDown = true;
				}
				
				
				
				//-------------- LEFT MOUSE BUTTON HELD DOWN --------------
				if(LMBClickedDown
				   && Vector2.Distance(LMBDownMousePos, currentMousePos) > .1f) {
					UIManager.Instance.ToggleSelectionRectangle(true);
					boxSelectionInitiated = true;
					LMBClickedDown = false; //this will avoid repeating this block every frame
				}
				
				if(boxSelectionInitiated) {
					//draw the screen space selection rectangle
					Vector2 rectPos = new Vector2(
						                  (LMBDownMousePos.x + currentMousePos.x) * .5f,
						                  (LMBDownMousePos.y + currentMousePos.y) * .5f);
					Vector2 rectSize = new Vector2(
						                   Mathf.Abs(LMBDownMousePos.x - currentMousePos.x),
						                   Mathf.Abs(LMBDownMousePos.y - currentMousePos.y));
					selectionRect = new Rect(rectPos - (rectSize * .5f), rectSize);
					
					UIManager.Instance.SetSelectionRectangle(selectionRect);
				}
				
				
				//-------------- LEFT MOUSE BUTTON UP --------------
				if(Input.GetMouseButtonUp(0)) {
					GameManager.Instance.ClearSelection();
					
					if(boxSelectionInitiated) {
						//consider the mouse release as the end of a box selection
						Unit[] allSelectables = GameManager.Instance.GetAllSelectableUnits();
						for(int i = 0; i < allSelectables.Length; i++) {
							Vector2 screenPos = mainCamera.WorldToScreenPoint(allSelectables[i].transform.position);
							if(selectionRect.Contains(screenPos)) {
								GameManager.Instance.AddToSelection(allSelectables[i], false);
							} else {
								//GameManager.Instance.RemoveFromSelection(allSelectables[i]); //Not necessary anymore, selection is cleared above
							}
						}
						
						//hide the box
						UIManager.Instance.ToggleSelectionRectangle(false);
					} else {
						if(Time.unscaledTime < timeOfClick + CLICK_TOLERANCE) {
							//consider the mouse release as a click
							RaycastHit hit;
							Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
							
							if(Physics.Raycast(ray, out hit, Mathf.Infinity, unitsLayerMask)) {
								if(hit.collider.gameObject.CompareTag("Locals")) {
									Unit newSelectedUnit = hit.collider.GetComponent<Unit>();
									GameManager.Instance.AddToSelection(newSelectedUnit);
									newSelectedUnit.SetSelected(true);
								}
							}
						}
					}
					
					LMBClickedDown = false;
					boxSelectionInitiated = false;
					
				}
				
				//-------------- RIGHT MOUSE BUTTON UP --------------
				if(Input.GetMouseButtonUp(1)
				   && GameManager.Instance.GetSelectionLength() != 0) {
					RaycastHit hit;
					Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
					
					if(Physics.Raycast(ray, out hit, Mathf.Infinity, unitsLayerMask)) {
						if(hit.collider.gameObject.CompareTag("Foreigners")) {
							Unit targetOfAttack = hit.collider.GetComponent<Unit>();
							GameManager.Instance.AttackTarget(targetOfAttack);
						}
					} else {
						Vector3 commandPoint;
						GetMouseOnGroundPlane(out commandPoint);
						GameManager.Instance.SentSelectedUnitsTo(commandPoint);
					}
					
				}
				
				//-------------- GAMEPLAY CAMERA MOVEMENT --------------
				if(!boxSelectionInitiated) {
					Vector2 amountToMove = new Vector2(0f, 0f);
					bool mouseIsMovingCamera = false;
					bool keyboardIsMovingCamera = false;
					
					//This check doesn't allow the camera to move with the mouse if we're currently framing a platoon
					if(mouseMovesCamera
					   && !CameraManager.Instance.IsFramingPlatoon) {
						Vector3 mousePosition = Input.mousePosition;
						mousePosition.x -= Screen.width / 2f;
						mousePosition.y -= Screen.height / 2f;
						
						//horizontal
						float horizontalDeadZone = Screen.width * mouseDeadZone.x;
						float absoluteXValue = Mathf.Abs(mousePosition.x);
						if(absoluteXValue > horizontalDeadZone) {
							//camera needs to move horizontally
							amountToMove.x = (absoluteXValue - horizontalDeadZone) * Mathf.Sign(mousePosition.x) * .01f * mouseSpeed;
							mouseIsMovingCamera = true;
						}
						
						//vertical
						float verticalDeadZone = Screen.height * mouseDeadZone.y;
						float absoluteYValue = Mathf.Abs(mousePosition.y);
						if(absoluteYValue > verticalDeadZone) {
							//camera needs to move horizontally
							amountToMove.y = (absoluteYValue - verticalDeadZone) * Mathf.Sign(mousePosition.y) * .01f * mouseSpeed;
							mouseIsMovingCamera = true;
						}
					}
					
					//Keyboard movements only happen if mouse is not causing the camera to move already
					if(!mouseIsMovingCamera) {
						float horKeyValue = Input.GetAxis("CameraHorizontal");
						float vertKeyValue = Input.GetAxis("CameraVertical");
						if(horKeyValue != 0f || vertKeyValue != 0f) {
							amountToMove = new Vector2(horKeyValue, vertKeyValue) * keyboardSpeed;
							keyboardIsMovingCamera = true;
						}
					}
					
					if(mouseIsMovingCamera || keyboardIsMovingCamera) {
						CameraManager.Instance.SetPlatoonFramingMode(false); //will deactivate platoon following camera, only if it was active
						CameraManager.Instance.MoveGameplayCamera(amountToMove * .5f);
					}
				}
				
				//-------------- REQUEST GROUP TARGET CAMERA --------------
				if(Input.GetKeyDown(KeyCode.G)) {
					CameraManager.Instance.TogglePlatoonFramingMode();
				}
				break;

			case GameManager.GameMode.DialogueMoment:
				if(Input.GetKeyDown(KeyCode.Space))
				{
					GameManager.Instance.ResumeTimeline();
				}
				break;
		}
	}

	private bool GetMouseOnGroundPlane(out Vector3 thePoint)
	{
		thePoint = Vector3.zero;

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		float rayDistance;
		if (groundPlane.Raycast(ray, out rayDistance))
		{
			thePoint = ray.GetPoint(rayDistance);
			return true;
		}
		else
		{
			return false;
		}
	}
}
