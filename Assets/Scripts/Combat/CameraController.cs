using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	private const float CAMERA_Z = -0.5f;
	public float speed = .01f;
	public float cameraMovementScreenSpacePct = 0.05f;


	private float cameraLerpStartTime;
	private float cameraLerpTime = 0.15f;
	private Vector3 cameraLerpStartingPosition;

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
		case State.SmoothMoveThenUnlock:	
		case State.Following:
			if (following != null)
			{
				float t = Mathf.Min((Time.time - cameraLerpStartTime) / cameraLerpTime, 1);
				Vector3 targetPosition = Vector3.Lerp(cameraLerpStartingPosition, following.transform.position, t);

				MoveTo(targetPosition);
				if (state == State.SmoothMoveThenUnlock && t >= 1f)
				{
					state = State.Unlocked;
				}
			} 
			else if (state == State.SmoothMoveThenUnlock)
			{
				state = State.Unlocked;
			}
			break;
		case State.Unlocked:
			Vector3 movementDirection = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f).normalized;
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

	public void SmoothMoveToThenUnlock(Unit unit)
	{
		state = State.SmoothMoveThenUnlock;
		following = unit;
		cameraLerpStartTime = Time.time;
		cameraLerpStartingPosition = transform.position;
	}

	private void MoveTo(Vector3 position)
	{
		transform.position = new Vector3(position.x, position.y, CAMERA_Z);
	}

	public void FollowUnit(Unit unit)
	{
		state = State.Following;
		following = unit;
		cameraLerpStartTime = Time.time;
		cameraLerpStartingPosition = transform.position;
	}

	public void Unlock()
	{
		state = State.Unlocked;
		following = null;
	}
	
	
	private enum State {
		Unlocked,
		Following,
		SmoothMoveThenUnlock,
	}
}
