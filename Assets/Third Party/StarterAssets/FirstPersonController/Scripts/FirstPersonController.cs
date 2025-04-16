using System;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		public float _crouchSpeed = 2.0f;
        [SerializeField] private bool _isCrouched = false;
        private bool _isCrouchTransitioning = false;
        private float _originalPlayerHeight;
        [SerializeField] private float _crouchHeight = 1;
        [SerializeField] private bool _isCrouchAvailable = true;
        [SerializeField] private bool _isUncrouchAvailable = true;
        private float _detectionDistance;
        private Color _crouchGizmoColor = Color.green;
        [Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[SerializeField] private bool _isSprinting = false;
		[SerializeField] private bool _isSprintAvailable = true;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("If the jump timeout has completed")]
		[SerializeField] private bool _jumpTimeoutCompleted = true;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs or across uneven terrain")]
		public float FallTimeout = 0.15f;
        [Tooltip("If the character has been falling for long enough to be considered No Longer Grounded")]
        [SerializeField] private bool _fallTimeoutCompleted = false;
        [Header("Player Grounded")]
		[Tooltip("If the character is physically touching the ground or not at this instance. " +
				"Not part of the CharacterController built in grounded check. " +
				"Also doesn't consider the fallout timer.")]
		[SerializeField] private bool _literallyGrounded = true;
		[Tooltip("If the character is considered to be grounded AFTER considering the fallout timer. " +
			"This is the functional grounded state, and this state is returned to other scripts")]
		[SerializeField] private bool _logicallyGrounded = true;
		
        
        [Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;



		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private Vector3 _moveDirection;
		private float _speed;
		private float _rotationVelocity;
		[Header("Fall heights")]
		[SerializeField]private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;
		[SerializeField] private FootSoundType _landing = FootSoundType.landEasy;
		[SerializeField] private float _moderateLandingMinVelocity = 5f;
		[SerializeField] private float _heavyLandingMinVelocity = 9f;
		[SerializeField] private float _nastyLandingMinVelocity = 15f;


		// timers & time flags
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;
		private bool _jumpTriggered = false;




#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;


		public delegate void MovementEvent();
		public delegate void LandEvent(FootSoundType landingType);
		public event LandEvent OnLand;
		public event MovementEvent OnJump;
		public event MovementEvent OnUngrounded;
		public event MovementEvent OnRunEnter;
		public event MovementEvent OnRunExit;
		public event MovementEvent OnCrouchEnter;
		public event MovementEvent OnCrouchExit;



		//optional Compiles
		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}


		//monobehaviours
		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

			_originalPlayerHeight = _controller.height;
		}

		private void Update()
		{
			DetermineCharacterStates();
			ApplyVelocities();
			MoveCharacter();
			
		}

		private void LateUpdate()
		{
			CameraRotation();
		}


        private void OnDrawGizmos()
        {
            DrawCrouchHeightDetection();
        }

        private void OnDrawGizmosSelected()
        {

            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (_literallyGrounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }



        //internals
		private void DetermineCharacterStates()
		{
			UpdateGroundedStates();
			UpdateSprintState();
			UpdateLandingSound();
			ManageCrouch();
			
		}

        private void UpdateGroundedStates()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			_literallyGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_literallyGrounded)
            {
				//Were we previously falling?
				if (_fallTimeoutCompleted)
				{
					_fallTimeoutCompleted = false;
					_logicallyGrounded = true;
                    OnLand?.Invoke(_landing);
                }

                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                
            }
			else
			{
                // tick the fall timeout, if it hasn't yet completed
				if (!_fallTimeoutCompleted)
				{
                    _fallTimeoutDelta -= Time.deltaTime;

					if (_fallTimeoutDelta <= 0)
					{
						_fallTimeoutCompleted = true;
						_logicallyGrounded = false;
						OnUngrounded?.Invoke();
					}
                }

            }
        }
        private void UpdateSprintState()
        {
            //only spring if it's available and not using the crouch mechanic
            if (_isSprintAvailable && (!_isCrouched && !_isCrouchTransitioning))
            {
                _isSprinting = _input.sprint;
            }
            else
            {
                _isSprinting = false;
            }

        }
        private void UpdateLandingSound()
		{
            if (_verticalVelocity <= -_nastyLandingMinVelocity)
                _landing = FootSoundType.landNasty;

            else if (_verticalVelocity <= -_heavyLandingMinVelocity)
                _landing = FootSoundType.landHeavy;

            else if (_verticalVelocity <= -_moderateLandingMinVelocity)
                _landing = FootSoundType.landModerate;

            else _landing = FootSoundType.landEasy;
        }
        private void ManageCrouch()
        {
            //detect for any obstructions if the crouch mechanic is working
            if (_isCrouchTransitioning || _isCrouched)
            {
                _detectionDistance = _originalPlayerHeight - _controller.height;
                Vector3 topOfPlayer = transform.position + _controller.height * Vector3.up;
                Vector3 detectionEnd = topOfPlayer + Vector3.up * _detectionDistance;
                Collider[] detectedColliders = Physics.OverlapCapsule(topOfPlayer, detectionEnd, _controller.radius, GroundLayers);

                if (detectedColliders.Length > 0)
                {
                    _isUncrouchAvailable = false;
                    //Debug.Log($"Standup Unavailable: {detectedColliders.Length} colliders detected");
                }
                else
                {
                    _isUncrouchAvailable = true;
                    //Debug.Log("Overhead Cleared!");
                }


            }


            if (!_isCrouched)
            {
                //start crouching if
                //... the action is available,
                //... the button is being pressed,
                //... and we ARE NOT ALREADY TRANSITIONING INTO A CROUCH
                if (_isCrouchAvailable && _input.crouch && !_isCrouchTransitioning)
                {
                    _isCrouchTransitioning = true;
                }

                //keep transitioning into a crouch if the button is still pressed (and if we're still able to crouch) [OR IF WE SUDDENLY GOT BLOCKED]
                if (_isCrouchTransitioning && ((_input.crouch && _isCrouchAvailable) || !_isUncrouchAvailable))
                {
                    float newHeight = _controller.height + (-Time.deltaTime * _crouchSpeed);

                    //complete the crouch if we reached our desired crouch height
                    if (newHeight < _crouchHeight)
                    {
                        newHeight = _crouchHeight;
                        _isCrouchTransitioning = false;
                        _isCrouched = true;
                        OnCrouchEnter?.Invoke();
                    }

                    _controller.height = newHeight;
                }

                //raise back up if either 'the button got released' or 'we suddenly aren't allowed to crouch' [ALSO IF WE AREN'T BLOCKED]
                else if (_isCrouchTransitioning && (!_input.crouch || !_isCrouchAvailable) && _isUncrouchAvailable)
                {
                    float newHeight = _controller.height + Time.deltaTime * _crouchSpeed;

                    //end the transition if we've raised back to our original height
                    if (newHeight > _originalPlayerHeight)
                    {
                        newHeight = _originalPlayerHeight;
                        _isCrouchTransitioning = false;
                    }

                    _controller.height = newHeight;
                }
            }

            else
            {
                //start raising up if
                //... we have space to stand up,
                //... crouch got either 'released' or 'interrupted',
                //... and we ARE NOT ALREADY TRANSITIONING 
                if (_isUncrouchAvailable && (!_input.crouch || !_isCrouchAvailable) && !_isCrouchTransitioning)
                {
                    _isCrouchTransitioning = true;
                }

                //keep raising up if 1) we have space to stand, and 2) the player either can't crouch here or isn't pressing crouch
                if (_isCrouchTransitioning && _isUncrouchAvailable && (!_input.crouch || !_isCrouchAvailable))
                {
                    float newHeight = _controller.height + Time.deltaTime * _crouchSpeed;

                    //end the transition if we've raised back to our original height
                    if (newHeight > _originalPlayerHeight)
                    {
                        newHeight = _originalPlayerHeight;
                        _isCrouchTransitioning = false;
                        _isCrouched = false;
                        OnCrouchExit?.Invoke();
                    }

                    _controller.height = newHeight;
                }

                //else lower back down into position if we got blocked or the player wants to keep crouching
                else if (_isCrouchTransitioning && (!_isUncrouchAvailable || (_input.crouch && _isCrouchAvailable)))
                {
                    float newHeight = _controller.height + (-Time.deltaTime * _crouchSpeed);

                    //complete the crouch if we reached our desired crouch height
                    if (newHeight < _crouchHeight)
                    {
                        newHeight = _crouchHeight;
                        _isCrouchTransitioning = false;
                    }

                    _controller.height = newHeight;
                }
            }


        }

        private void Crouch()
        {
            if (_isCrouched)
            {
                //update isUncrouchAvailable.
                //Just capsuleCast on the player's position by the player's height.
                //it's available if nothing was detected.
            }


            //first manage the core crouch state
            if (!_isCrouched && _isCrouchAvailable && _input.crouch)
            {
                _isCrouched = true;
                
                //resize collider
                

                OnCrouchEnter?.Invoke();
            }
            else if (_isCrouched && (!_input.crouch || !_isCrouchAvailable) && _isUncrouchAvailable) //make sure we're actually able to uncrouch, too
            {
                _isCrouched = false;
                OnCrouchExit?.Invoke();
            }

            //next, 


        }




        private void ApplyVelocities()
		{
			ApplyGravity();
			ApplyHorizontalMovement();
			ManageJump();
			CalculateMoveDirection();
		}
        private void ApplyGravity()
        {
            if (_literallyGrounded) //NOT logically, but Literally
            {
                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

            }
            else
            {
                // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
                if (_verticalVelocity < _terminalVelocity)
                {
                    _verticalVelocity += Gravity * Time.deltaTime;
                }

            }
        }
		private void ApplyHorizontalMovement()
		{
            // set target speed based on sprint/crouch/standing state
            float targetSpeed = CalculateTargetSpeed();

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            

            
        }
        private float CalculateTargetSpeed()
        {
            if (_isSprinting)
                return SprintSpeed;
            else if (_isCrouched || _isCrouchTransitioning)
                return _crouchSpeed;
            else return MoveSpeed;
        }
        private void ManageJump()
        {
            if (_logicallyGrounded)
            {
                //jump if it's pressed and ready
                if (_jumpTimeoutCompleted && _input.jump)
                {
					_input.jump = false;

                    //reset the timer
                    _jumpTimeoutCompleted = false;

                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    //signal the jump
                    OnJump?.Invoke();
                }


                //else cooldown the jump timer (only when grounded)
                else
                {
                    //tick down the timeout if it isn't already completed
                    if (!_jumpTimeoutCompleted)
                    {
                        _jumpTimeoutDelta -= Time.deltaTime;

                        if (_jumpTimeoutDelta <= 0)
                        {
                            _jumpTimeoutCompleted = true;
                            _jumpTimeoutDelta = JumpTimeout;
                        }
                    }
                }
            }
        }
        private void CalculateMoveDirection()
        {
            // normalise input direction
            _moveDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                // move
                _moveDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }
        }




        private void MoveCharacter()
		{
            _controller.Move(_moveDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }




        private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }




        //gizmo-related
        private void DrawCrouchHeightDetection()
        {
            if (_controller != null)
            {
                Gizmos.color = _crouchGizmoColor;
                Vector3 topOfPlayer = transform.position + _controller.height * Vector3.up;
                Gizmos.DrawLine(topOfPlayer, topOfPlayer + Vector3.up * _detectionDistance);

            }
        }




        //Externals
        public float GetSpeed() { return _speed; }

		public bool IsUngrounded() { return !_logicallyGrounded; }

		public bool IsSprinting() { return _isSprinting; }

		public bool IsForwardsPressed() { return _input.move.y == 1; }

		public bool IsCrouched() { return _isCrouched; }
		public bool IsCrouchTransitioning() { return _isCrouchTransitioning; }
	}
}