using System;
using UnityEngine;
using System.Collections;

[Serializable]
public class ControllerParameters2D {
	public enum JumpBehavior
	{
		CanJumpOnGround,
		CanJumpAnywhere,
		CantJump
	}

	public Vector2 MaxVelocity = new Vector2 (float.MaxValue, 30);

	[Range(0, 90)]
	public float SlopeLimit = 30;

	public float Gravity = -35f;

	public JumpBehavior JumpParameters;

	public float JumpInterval = 0.14f;

	public float JumpMagnitude = 0.2f;
	
	public float MaxJumpSpeed = 5;

	public float BaseJumpSpeed = 9;

	public float GroundAcceleration = 5f;

	public float AirAcceleration = 100f;
}
