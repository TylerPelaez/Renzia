using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	private const float CAMERA_Z = -0.5f;
	public float speed = .01f;
	public float cameraMovementScreenSpacePct = 0.05f;
	
	private State state = State.Unlocked;
	private Unit following;
	
	public Bounds Bounds { get; set; }
	
	
    void Start()
	{
		if (!Application.isEditor) {
			Cursor.lockState = CursorLockMode.Confined;
		}
    }

	void Update()
	{
		switch (state) {
		case State.Following:
			MoveTo(following.transform.position);
			break;
		case State.Unlocked:
			Vector3 mousePos = Input.mousePosition;
			float lowerX = Screen.width * cameraMovementScreenSpacePct;
			float upperX = Screen.width - lowerX;
			float lowerY = Screen.height * cameraMovementScreenSpacePct;
			float upperY = Screen.height - lowerY;
			
			
			Vector3 movementDirection = new Vector3(mousePos.x <= lowerX ? -1 : mousePos.x >= upperX ? 1 : 0, mousePos.y <= lowerY ? -1 : mousePos.y >= upperY ? 1 : 0, 0).normalized;
			Vector3 newPosition = transform.position + (movementDirection * speed * Time.deltaTime);

			if (newPosition.x < Bounds.min.x || newPosition.y < Bounds.min.y || newPosition.x > Bounds.max.x ||
			    newPosition.y > Bounds.max.y)
			{
				newPosition = Bounds.ClosestPoint(newPosition);
			}

			MoveTo(newPosition);
			break;
		default:
			break;
		}
	}

	public void MoveTo(Vector3 position)
	{
		transform.position = new Vector3(position.x, position.y, CAMERA_Z);
	}

	public void FollowUnit(Unit unit)
	{
		state = State.Following;
		following = unit;
		MoveTo(unit.transform.position);
	}

	public void Unlock()
	{
		state = State.Unlocked;
		following = null;
	}
	
	
	private enum State {
		Unlocked,
		Following,
	}
}
