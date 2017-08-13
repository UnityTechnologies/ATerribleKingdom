using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : Singleton<InputManager>
{
	public LayerMask unitsLayerMask;
	public bool mouseMovesCamera = true;

	private const float MOUSE_DEAD_ZONE = .4f;

	private void Update()
	{
		//select
		if(Input.GetMouseButtonUp(0))
		{
			RaycastHit hit;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			if(Physics.Raycast(ray, out hit, Mathf.Infinity, unitsLayerMask))
			{
				Unit newSelectedUnit = hit.collider.GetComponent<Unit>();
				GameManager.Instance.AddToSelection(newSelectedUnit);
			}
			else
			{
				GameManager.Instance.ClearSelection();
			}
		}

		//order move
		if(Input.GetMouseButtonUp(1)
			&& GameManager.Instance.GetSelectionLength() != 0)
		{
			//GameManager.Instance.IssueCommand
		}

		//-------------- GAMEPLAY CAMERA MOVEMENT --------------
		Vector2 amountToMove = new Vector2(0f, 0f);
		bool needToMove = false;

		if(mouseMovesCamera)
		{
			Vector3 mousePosition = Input.mousePosition;
			mousePosition.x -= Screen.width / 2f;
			mousePosition.y -= Screen.height / 2f;
			
			Vector2 deadZone = CameraManager.Instance.GetVCamDeadZone() * .5f;
			
			//horizontal
			float horizontalDeadZone = Screen.width * deadZone.x; //MOUSE_DEAD_ZONE;
			float absoluteXValue = Mathf.Abs(mousePosition.x);
			if(absoluteXValue > horizontalDeadZone)
			{
				//camera needs to move horizontally
				amountToMove.x = (absoluteXValue - horizontalDeadZone) * Mathf.Sign(mousePosition.x) * .01f;
				needToMove = true;
			}
			
			//vertical
			float verticalDeadZone = Screen.height * deadZone.y; //MOUSE_DEAD_ZONE;
			float absoluteYValue = Mathf.Abs(mousePosition.y);
			if(absoluteYValue > verticalDeadZone)
			{
				//camera needs to move horizontally
				amountToMove.y = (absoluteYValue - verticalDeadZone) * Mathf.Sign(mousePosition.y) * .01f;
				needToMove = true;
			}
		}

		//Keyboard movements only happen if mouse is not causing the camera to move already
		if(!needToMove)
		{
			amountToMove = new Vector2(Input.GetAxis("CameraHorizontal"), Input.GetAxis("CameraVertical"));
			needToMove = true;
		}

		if(needToMove)
		{
			CameraManager.Instance.MoveGameplayCamera(amountToMove * .5f);
		}
	}
}
