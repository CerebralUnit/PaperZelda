using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

	private bool IsFacingRight;
	private CharacterController2D controller;
	private float normalizedHorizontalSpeed;
	public ControllerParameters2D Parameters { get; set;} 
	public float MaxSpeed = 8;
	private bool JumpIsHeld = false;
	private bool IsCrouching = false;
	public void Start()
	{
		controller = GetComponent<CharacterController2D>();
		IsFacingRight = transform.localScale.x > 0;
		Parameters = new ControllerParameters2D();

	}

	public void Update()
	{
		HandleInput();

		var MovementFactor = controller.State.IsGrounded ? Parameters.GroundAcceleration : Parameters.AirAcceleration;

		controller.SetHorizontalForce (Mathf.Lerp (controller.Velocity.x, normalizedHorizontalSpeed * MaxSpeed, Time.deltaTime * MovementFactor));
	}

	private void HandleInput(){

		if (Input.GetKey (KeyCode.D) && !IsCrouching) {
			normalizedHorizontalSpeed = 1;

			if (!IsFacingRight) {
				Flip ();
			}

		} else if (Input.GetKey (KeyCode.D) && !IsFacingRight) {
			Flip ();
		} else if (Input.GetKey (KeyCode.A) && !IsCrouching) {
			normalizedHorizontalSpeed = -1;
			if (IsFacingRight) {
				Flip ();
			}
			
		} else if (Input.GetKey (KeyCode.A) && IsFacingRight) {
			Flip ();
		} else {
			normalizedHorizontalSpeed = 0;
		}

		if (Input.GetKey (KeyCode.S) && controller.State.IsGrounded && !IsCrouching) {
			IsCrouching = true;
			controller.Crouch();
		}
		if (Input.GetKeyDown (KeyCode.K) && !IsCrouching) {
			controller.Attack ();

		} else if (Input.GetKeyDown (KeyCode.K) && IsCrouching) {
			controller.CrouchAttack();
		}
		if(Input.GetKeyUp(KeyCode.S)){
			IsCrouching = false;
			controller.Idle();
		}
		if (Input.GetKey(KeyCode.Space)) {
			Debug.Log(controller.CanJump);
		}
		if (controller.CanJump  && Input.GetKey(KeyCode.Space) && JumpIsHeld && !controller.State.IsGrounded) {
			controller.Jump();
		}
		if (controller.CanJump && Input.GetKeyDown(KeyCode.Space) && !JumpIsHeld) {

			controller.Jump();
			JumpIsHeld = true;
		}


		if (Input.GetKeyUp (KeyCode.Space) ) {
			if (!controller.State.IsGrounded){
				controller.CanJump = false;
			}else{
				controller.ResetCanJump();

			}

			JumpIsHeld = false;
		}
	}

	private void Flip()
	{
		transform.localScale = new Vector3 (-transform.localScale.x, transform.localScale.y, transform.localScale.z);
		IsFacingRight = transform.localScale.x > 0;
	}
}
