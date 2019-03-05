using UnityEngine;
using System.Collections;

/// <summary>
///	Controls:
/// WASD : Directional movement
/// Shift/Control : Hold to increase/decrease speed
/// Q/E : Moves camera up/down on the Y-axis
/// Right Click : Zooms in
/// End : Toggle cursor locking to screen
/// </summary>
[RequireComponent(typeof(Camera))]
public class iCECamera : MonoBehaviour
{
	[Range(1.0f, 360.0f)]
	public float cameraSensitivity = 90;

	[Range(1.0f, 250.0f)]
	public float climbSpeed = 4;

	[Range(1.0f, 250.0f)]
	public float moveSpeed = 10;

	[Range(2.0f, 20.0f)]
	public float rotateSpeed = 5.0f;

	[Range(0.0f, 20.0f)]
	public float zoomSpeed = 2.0f;

	[Range(10.0f, 100.0f)]
	public float zoomInFov = 45.0f;

	[Range(0.0f, 45.0f)]
	public float tiltAmount = 5.0f;

	private float slowMoveFactor = 0.25f;
	private float fastMoveFactor = 3;
 
	private float rotationX;
	private float rotationY;
	private float rotationZ;

	private float initialFov = 60.0f;
	private float fovDampen;
	private Camera currentCamera;
 
	/// <summary>
	///	Used to setup any methods or fields before start is called
	/// </summary>
	private void Awake ()
	{
		// Fix for initial rotation being reset to 0
		rotationX = transform.rotation.eulerAngles.y;
	}

	/// <summary>
	///	Initializing fields for camera fly script
	/// </summary>
	private void Start ()
	{
		// Start with the cursor locked to the screen
		Cursor.visible = false;
   	Cursor.lockState = CursorLockMode.Locked;

		currentCamera = GetComponent<Camera>();
		initialFov = currentCamera.fieldOfView;
	}

	/// <summary>
	///	Handles the FOV zoom based on mouse input
	/// </summary>
	private void HandleZoom ()
	{
		bool isHoldingRightMouse = Input.GetMouseButton(1);

		if (currentCamera)
		{
			if (!isHoldingRightMouse)
			{
				currentCamera.fieldOfView = Mathf.SmoothDamp(currentCamera.fieldOfView, initialFov, ref fovDampen, 1 / zoomSpeed);
			}

			else if (isHoldingRightMouse)
			{
				currentCamera.fieldOfView = Mathf.SmoothDamp(currentCamera.fieldOfView, zoomInFov, ref fovDampen, 1 / zoomSpeed);
			}
		}
	}

	/// <summary>
	///	Handles the cursor visibility/lock state
	/// </summary>
	private void HandleCursor ()
	{
		bool isCursorLocked = Cursor.lockState == CursorLockMode.Locked;

		// Always hide the cursor if it's position is locked
		if (isCursorLocked)
		{
			Cursor.visible = false;
		}

		// Toggle the current state if we press ESC
		if (Input.GetKeyDown(KeyCode.Escape))
		{
   		Cursor.lockState = isCursorLocked
			 ? CursorLockMode.None 
			 : CursorLockMode.Locked;
		}
	}

	/// <summary>
	///	Handles the rotation based on mouse input and sensitivity
	/// </summary>
	private void HandleRotation ()
	{
		float canRotate = (Cursor.lockState == CursorLockMode.Locked) ? 1 : 0;

		rotationX += Input.GetAxis("Mouse X") * cameraSensitivity * canRotate * Time.deltaTime;
		rotationY += Input.GetAxis("Mouse Y") * cameraSensitivity * canRotate * Time.deltaTime;
		rotationZ = -Input.GetAxis("Mouse X") * tiltAmount * canRotate;

		// Clamp y-axis rotation as we don't want to flip the camera upside down
		rotationY = Mathf.Clamp(rotationY, -90, 90);

		Quaternion targetRotation = Quaternion.Euler(-rotationY, rotationX, rotationZ);

		transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
	}

	/// <summary>
	///	Handles the position changes based on input
	/// </summary>
	private void HandleMovement ()
	{
		if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift))
	 	{
			transform.position += transform.forward * (moveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
			transform.position += transform.right * (moveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
	 	}
	 	else if (Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl))
	 	{
			transform.position += transform.forward * (moveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
			transform.position += transform.right * (moveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
	 	}
	 	else
	 	{
	 		transform.position += transform.forward * moveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
			transform.position += transform.right * moveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
	 	}
	}

	/// <summary>
	///	Handles the increase and decrease in elevation based on input
	/// </summary>
	private void HandleElevation ()
	{
		if (Input.GetKey (KeyCode.Q)) transform.position += transform.up * climbSpeed * Time.deltaTime;
		if (Input.GetKey (KeyCode.E)) transform.position -= transform.up * climbSpeed * Time.deltaTime;
	}
 
	private void Update ()
	{
		HandleCursor();

		HandleZoom();

		HandleRotation();

	 	HandleMovement();
 
		HandleElevation();
	}
}