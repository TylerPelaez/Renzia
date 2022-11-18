﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	public float speed = .01f;
	public float cameraMovementScreenSpacePct = 0.05f;
	
	private State state = State.Unlocked;

	
    void Start()
	{
		if (!Application.isEditor) {
			Cursor.lockState = CursorLockMode.Confined;
		}
    }

	void Update()
	{
		switch (state) {
		case State.Locked:
			break;
		case State.Unlocked:
			Vector3 mousePos = Input.mousePosition;
			float lowerX = Screen.width * cameraMovementScreenSpacePct;
			float upperX = Screen.width - lowerX;
			float lowerY = Screen.height * cameraMovementScreenSpacePct;
			float upperY = Screen.height - lowerY;
			
			
			Vector3 movementDirection = new Vector3(mousePos.x <= lowerX ? -1 : mousePos.x >= upperX ? 1 : 0, mousePos.y <= lowerY ? -1 : mousePos.y >= upperY ? 1 : 0, 0).normalized;
			
			transform.position += movementDirection * speed * Time.deltaTime;
			
			break;
		default:
			break;
		}
	}
	
	private enum State {
		Locked,
		Unlocked
	}
}