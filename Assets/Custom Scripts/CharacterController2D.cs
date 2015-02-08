using UnityEngine;
using System.Collections;

public class CharacterController2D : MonoBehaviour 
{
	private const float SkinWidth = .02f;
	private const int TotalHorizontalRays = 8;
	private const int TotalVerticalRays = 4;
	private static readonly float SlopeLimitTangent = Mathf.Tan(75 * Mathf.Deg2Rad);

	private float JumpForce;

	public LayerMask PlatformMask;
	public ControllerParameters2D DefaultParameters;
	public bool StandingOn;
	public bool JumpPeaked;
	public ControllerState2D State { get; private set; }
	public Vector2 Velocity { get { return _Velocity; } }
	public bool CanJump { get; set; }
	public ControllerParameters2D Parameters { get { return _overrideParameters ?? DefaultParameters; } }
	public bool HandleCollisions { get; set; }

	public Sprite IdleSprite;
	public Sprite CrouchSprite;
	public Sprite AttackSprite;
	public Sprite CrouchAttackSprite;

	private Vector2 _Velocity;
	private Transform _transform;
	private Vector3 _localScale;
	private BoxCollider2D _boxCollider;
	private SpriteRenderer _spriteRenderer;
	private ControllerParameters2D _overrideParameters;
	private Vector3 _raycastTopLeft, _raycastBottomRight, _raycastBottomLeft;
	private float _jumpIn;
	private float 
		_verticalDistanceBetweenRays,
		_horizontalDistanceBetweenRays;
	public void Awake()
	{

		CanJump = true;
		HandleCollisions = true;
		State = new ControllerState2D ();
		JumpForce = Parameters.BaseJumpSpeed;
		_transform = transform;
		_localScale = transform.localScale;
		_boxCollider = GetComponent<BoxCollider2D> ();
		_spriteRenderer = gameObject.GetComponent<SpriteRenderer> ();
		var ColliderWidth = _boxCollider.size.x * Mathf.Abs (transform.localScale.x) - (2 * SkinWidth);
		_horizontalDistanceBetweenRays = ColliderWidth / (TotalVerticalRays - 1);

		var ColliderHeight = _boxCollider.size.y * Mathf.Abs (transform.localScale.y) - (2 * SkinWidth);
		_verticalDistanceBetweenRays = ColliderHeight / (TotalHorizontalRays -1);
	}

	public void AddForce(Vector2 force)
	{
		_Velocity = force;
	}
	
	public void SetForce(Vector2 force)
	{
		_Velocity += force;
	}
	
	public void SetHorizontalForce(float x)
	{
		_Velocity.x = x;
	}
	
	public void SetVerticalForce(float y)
	{
		_Velocity.y = y;
	}
	public void Crouch(){

		_spriteRenderer.sprite = CrouchSprite;
	}
	public void Attack(){
		_spriteRenderer.sprite = AttackSprite;
	StartCoroutine("PostAttack");
	}
	public void CrouchAttack(){
		_spriteRenderer.sprite = CrouchAttackSprite;
		StartCoroutine("PostCrouchAttack");
	}
	IEnumerator PostCrouchAttack(){
		
		yield return new WaitForSeconds (0.25f);
		Crouch ();
	}
	IEnumerator PostAttack(){

		yield return new WaitForSeconds (0.25f);
		Idle ();
	}
	public void Idle(){
		_spriteRenderer.sprite = IdleSprite;
	}
	public void Jump()
	{


			JumpForce -= Parameters.JumpMagnitude;
		if (JumpForce <= 0.1) {
			JumpForce = 0;
			CanJump = false;
		}

		if (_Velocity.y < Parameters.MaxJumpSpeed) {
		
			AddForce (new Vector2 (0, JumpForce));
		} else {
			JumpPeaked = true;
		}
		_jumpIn = Parameters.JumpInterval;
	}

	public void LateUpdate()
	{

		_Velocity.y += Parameters.Gravity * Time.deltaTime;
		Move (Velocity * Time.deltaTime);

	}

	private void Move(Vector2 deltaMovement)
	{

		var WasGrounded = State.IsCollidingBelow;
		State.Reset ();
		if (HandleCollisions) 
		{
			HandlePlatforms();
			CalculateRayOrigins();

			if (deltaMovement.y < 0 && WasGrounded){
				HandleVerticalSlope(ref deltaMovement);
			}
			if (Mathf.Abs(deltaMovement.x) > .001f){
				MoveHorizontally(ref deltaMovement);
			}

			MoveVertically(ref deltaMovement);
		}
		//Move Sprite to actual point in scene
		_transform.Translate (deltaMovement, Space.World);

		if (Time.deltaTime > 0) {
			_Velocity = deltaMovement / Time.deltaTime;

		}

		_Velocity.x = Mathf.Min(_Velocity.x, Parameters.MaxVelocity.x);
		_Velocity.y = Mathf.Min(_Velocity.y, Parameters.MaxVelocity.y);

		if (State.IsMovingUpSlope) {
			_Velocity.y = 0;
		}

	}

	private void HandlePlatforms()
	{
		
	}
	//Precomputes where rays will be
	private void CalculateRayOrigins()
	{
		var size = new Vector2(_boxCollider.size.x * Mathf.Abs(_localScale.x), _boxCollider.size.y * Mathf.Abs(_localScale.y)) / 2;
		var center = new Vector2(_boxCollider.center.x * _localScale.x, _boxCollider.center.y * _localScale.y);
		
		_raycastTopLeft = _transform.position + new Vector3(center.x - size.x + SkinWidth, center.y + size.y - SkinWidth);
		_raycastBottomRight = _transform.position + new Vector3(center.x + size.x - SkinWidth, center.y - size.y + SkinWidth);
		_raycastBottomLeft = _transform.position + new Vector3(center.x - size.x + SkinWidth, center.y - size.y + SkinWidth);

	}

	//Takes a ref because it needs to manipulate/constrain deltamovement
	private void MoveHorizontally(ref Vector2 deltaMovement)
	{

		var isGoingRight = deltaMovement.x > 0;
		var rayDistance = Mathf.Abs(deltaMovement.x) + SkinWidth;
		var rayDirection = isGoingRight ? Vector2.right : -Vector2.right;
		var rayOrigin = isGoingRight ? _raycastBottomRight : _raycastBottomLeft;
		
		for (var i = 0; i < TotalHorizontalRays; i++)
		{
			var rayVector = new Vector2(rayOrigin.x, rayOrigin.y + (i * _verticalDistanceBetweenRays));
			Debug.DrawRay(rayVector, rayDirection * rayDistance, Color.red);
			
			var rayCastHit = Physics2D.Raycast(rayVector, rayDirection, rayDistance, PlatformMask);
			if (!rayCastHit)
				continue;
			
			if ( i == 0 && HandleHorizontalSlope(ref deltaMovement, Vector2.Angle(rayCastHit.normal, Vector2.up), isGoingRight))
				break;
			
			deltaMovement.x = rayCastHit.point.x - rayVector.x;
			rayDistance = Mathf.Abs(deltaMovement.x);
			
			if (isGoingRight)
			{
				deltaMovement.x -= SkinWidth;
				State.IsCollidingRight = true;
			}
			else
			{
				deltaMovement.x += SkinWidth;
				State.IsCollidingLeft = true;
			}
			
			if (rayDistance < SkinWidth + .0001f)
				break;
		}

	}

	private void MoveVertically(ref Vector2 deltaMovement)
	{
		var isGoingUp = deltaMovement.y > 0;
		var rayDistance = Mathf.Abs(deltaMovement.y) + SkinWidth;
		var rayDirection = isGoingUp ? Vector2.up : -Vector2.up;
		var rayOrigin = isGoingUp ? _raycastTopLeft : _raycastBottomLeft;
		
		rayOrigin.x += deltaMovement.x;

		var standingOnDistance = float.MaxValue;
		for (var i = 0; i < TotalVerticalRays; i++)
		{
			var rayVector = new Vector2(rayOrigin.x + (i * _horizontalDistanceBetweenRays), rayOrigin.y);
			Debug.DrawRay(rayVector, rayDirection * rayDistance, Color.red);
			
			var raycastHit = Physics2D.Raycast(rayVector, rayDirection, rayDistance, PlatformMask);
			if (!raycastHit)
				continue;
			
			if (!isGoingUp)
			{
				var verticalDistanceToHit = _transform.position.y - raycastHit.point.y;


				if (verticalDistanceToHit < standingOnDistance)
				{
					standingOnDistance = verticalDistanceToHit;
					StandingOn = raycastHit.collider.gameObject;
				}
			}
			
			deltaMovement.y = raycastHit.point.y - rayVector.y;
			rayDistance = Mathf.Abs(deltaMovement.y);
			
			if (isGoingUp)
			{
				deltaMovement.y -= SkinWidth;
			
				State.IsCollidingAbove = true;
			}
			else
			{
				deltaMovement.y += SkinWidth;
				State.IsCollidingBelow = true;

				if (State.IsGrounded){
					ResetCanJump();
				}	

			}
			
			if (!isGoingUp && deltaMovement.y > .0001f)
				State.IsMovingUpSlope = true;
			
			if (rayDistance < SkinWidth + .0001f)
				break;
		}
	}
	public void ResetCanJump(){

		JumpForce = Parameters.BaseJumpSpeed;
		CanJump = true;

	}
	private void HandleVerticalSlope(ref Vector2 deltaMovement)
	{

	}

	private bool HandleHorizontalSlope(ref Vector2 deltaMovement, float angle, bool IsMovingRight)
	{
		if (Mathf.RoundToInt(angle) == 90)
			return false;
		
		if (angle > Parameters.SlopeLimit)
		{
			deltaMovement.x = 0;
			return true;
		}
		
		if (deltaMovement.y > .07f)
			return true;
		
		deltaMovement.x += IsMovingRight ? -SkinWidth : SkinWidth;
		deltaMovement.y = Mathf.Abs(Mathf.Tan(angle * Mathf.Deg2Rad) * deltaMovement.x);
		State.IsMovingUpSlope = true;
		State.IsCollidingBelow = true;
		return true;
	}

	public void OnTriggerEnter2D(Collider2D other)
	{

	}
	public void OnTriggerExit2D(Collider2D other)
	{}




}
