using System;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif



public enum MovementState
{
    Unset,
    General,
    Climbing
}

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif



	public class FirstPersonController : MonoBehaviour
	{
        [Header("Core Movement")]
        [SerializeField] private MovementState _movementState = MovementState.Unset;
        [SerializeField] private float _moveSpeed = 4.0f;
        [SerializeField] private float _sprintSpeed = 6.0f;
        [SerializeField] private bool _isSprinting = false;
        [SerializeField] private bool _isSprintAvailable = true;
        [Tooltip("Rotation speed of the character")]
        [SerializeField] private float _rotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        [SerializeField] private float _speedChangeRate = 10.0f;
        private Vector3 _moveDirection;
        private float _speed;
        private float _rotationVelocity;

        [Header("Parkour")]
        [SerializeField] LedgeDetectionManager _ledgeDetectManager;
        [Tooltip("Is the climbing mechanic enabled for the player to use")]
        [SerializeField] private bool _isClimbEnabled = true;
        [Tooltip("Is the player within a valid context to climb")]
        [SerializeField] private bool _isClimbAvailable = false;
        [SerializeField] private LedgeType _detectedLedgeType = LedgeType.unset;
        [SerializeField] private Vector3 _ledgePosition;


        [Header("Crouching")]
        [SerializeField] private float _cameraHeightTransitionSpeed = 2.0f;
        [SerializeField] private bool _isCrouched = false;
        private bool _isCrouchTransitioning = false;
        private float _originalPlayerHeight;
        [SerializeField] private float _crouchHeight = 1;
        [SerializeField] private bool _isCrouchAvailable = true;
        [SerializeField] private bool _isUncrouchAvailable = true;
        [SerializeField] private float _crouchCastTweak;
        [SerializeField] private DrawRectGizmo _crouchGizmo;


        [Header("Jumping & Falling")]
        [Tooltip("The height the player can jump")]
        [SerializeField] private float _jumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        [SerializeField] private float _gravity = -15.0f;
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        [SerializeField] private float _jumpTimeout = 0.1f;
        [Tooltip("If the jump timeout has completed")]
        [SerializeField] private bool _jumpTimeoutCompleted = true;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs or across uneven terrain")]
        [SerializeField] private float _fallTimeout = 0.15f;
        [Tooltip("If the character has been falling for long enough to be considered No Longer Grounded")]
        [SerializeField] private bool _fallTimeoutCompleted = false;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        [SerializeField] private FootSoundType _landing = FootSoundType.landEasy;
        [SerializeField] private float _moderateLandingMinVelocity = 5f;
        [SerializeField] private float _heavyLandingMinVelocity = 9f;
        [SerializeField] private float _nastyLandingMinVelocity = 15f;

        [Space(10)]
        [Tooltip("If the character is physically touching the ground or not at this instance. " +
                "Not part of the CharacterController built in grounded check. " +
                "Also doesn't consider the fallout timer.")]
        [SerializeField] private bool _literallyGrounded = true;
        [Tooltip("If the character is considered to be grounded AFTER considering the fallout timer. " +
            "This is the functional grounded state, and this state is returned to other scripts")]
        [SerializeField] private bool _logicallyGrounded = true;
        [Tooltip("Useful for rough ground")]
        [SerializeField] private float _groundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        [SerializeField] private float _groundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        [SerializeField] private LayerMask _groundLayers;


        
        


        [Header("Camera Settings")]
        [SerializeField] private Transform _playerCameraRoot;
        [SerializeField] private Transform _camRootParent;
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		[SerializeField] private float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		[SerializeField] private float BottomClamp = -90.0f;
        private float _cinemachineTargetPitch;
        private const float _threshold = 0.01f;


        [Header("Gizmo Settings")]
        [SerializeField] private Color _successColor;
        [SerializeField] private Color _missColor;



        //references
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;
        

		


        //events
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
			_jumpTimeoutDelta = _jumpTimeout;
			_fallTimeoutDelta = _fallTimeout;

            // track our expected height
			_originalPlayerHeight = _controller.height;

            //setup other initial states
            _movementState = MovementState.General;
            _ledgePosition = Vector3.zero;
		}

        private void OnEnable()
        {
            _ledgeDetectManager.OnLedgeDetected += ListenForAvailableLedges;
        }

        private void OnDisable()
        {
            _ledgeDetectManager.OnLedgeDetected -= ListenForAvailableLedges;
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

        private void OnDrawGizmosSelected()
        {

            DrawUncrouchAvailability();

            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (_literallyGrounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z), _groundedRadius);
        }



        //internals
        private void ListenForAvailableLedges(LedgeType type, Vector3 position)
        {
            _detectedLedgeType = type;
            _ledgePosition = position;
        }

		private void DetermineCharacterStates()
		{
            if ( _movementState == MovementState.General)
            {
                UpdateGroundedStates();
                UpdateSprintState();
                UpdateLandingSound();
                Crouch();
                UpdateCameraHeight();
                DetermineClimbAvailability();
                EnterClimb();
            }
            else if (_movementState == MovementState.Climbing)
            {
                Climb();
            }
		}

        private void UpdateGroundedStates()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);
			_literallyGrounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);

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
                _fallTimeoutDelta = _fallTimeout;

                
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
        private void Crouch()
        {
            //first, if we're crouching, make sure something isnt above to prevent us from standing up
            if (_isCrouched)
            {
                Vector3 playerMiddle = transform.position + Vector3.up * (_originalPlayerHeight / 2 + 0.01f);
                Vector3 castSize = new Vector3((_controller.radius + _crouchCastTweak ) / 2, (_originalPlayerHeight) / 2, (_controller.radius + _crouchCastTweak) / 2);
                Collider[] detectedColliders = Physics.OverlapBox(playerMiddle, castSize, transform.rotation, _groundLayers);

                if (detectedColliders.Length > 0)
                {
                    _isUncrouchAvailable = false;
                }
                   
                else _isUncrouchAvailable = true;

            }


            //now determine the core crouch state
            if (!_isCrouched && _isCrouchAvailable && _input.crouch)
            {
                _isCrouched = true;
                
                //resize collider
                _controller.height = _crouchHeight;
                _controller.center = Vector3.up * _crouchHeight / 2;

                OnCrouchEnter?.Invoke();
            }
            else if (_isCrouched && (!_input.crouch || !_isCrouchAvailable) && _isUncrouchAvailable) //make sure we're actually able to uncrouch, too
            {
                _isCrouched = false;

                //resize collider
                _controller.height = _originalPlayerHeight;
                _controller.center = Vector3.up * 0.93f;

                OnCrouchExit?.Invoke();
            }

        }
        private void DetermineClimbAvailability()
        {
            if ( _isClimbEnabled)
            {
                //are we sprinting(while grounded) or are we Falling? WHILE NOT ALREADY CLIMBING
                if ( (_isSprinting && _logicallyGrounded || !_logicallyGrounded && _verticalVelocity < 0) && _movementState != MovementState.Climbing)
                {
                    _isClimbAvailable = true;
                }
                else
                {
                    _isClimbAvailable = false;
                }
            }
            else
                _isClimbAvailable = false;
             
        }

        private void EnterClimb()
        {
            if (_isClimbAvailable && _ledgeDetectManager.IsLedgeStillValid(_detectedLedgeType, _ledgePosition))
            {
                Debug.Log("Pretend we entered the climb state");
            }
        }

        private void Climb()
        {

        }





        private void ApplyVelocities()
		{
            if (_movementState == MovementState.General)
            {
                ApplyGravity();
                ApplyHorizontalMovement();
                ManageJump();
                CalculateMoveDirection();
            }
			
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
                    _verticalVelocity += _gravity * Time.deltaTime;
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
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * _speedChangeRate);

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
                return _sprintSpeed;
            else if (_isCrouched || _isCrouchTransitioning)
                return _cameraHeightTransitionSpeed;
            else return _moveSpeed;
        }
        private void ManageJump()
        {
            if (_logicallyGrounded)
            {
                //jump if it's pressed and ready
                if (_jumpTimeoutCompleted && _input.jump)
                {
                    //reset the timer
                    _jumpTimeoutCompleted = false;

                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

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
                            _jumpTimeoutDelta = _jumpTimeout;
                        }
                    }
                }
            }

            //reset the jump input. It doesn't reset by itself for some reason
            _input.jump = false;
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
            if (_movementState == MovementState.General)
                _controller.Move(_moveDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }




        private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * _rotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * _rotationSpeed * deltaTimeMultiplier;

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
        private void UpdateCameraHeight()
        {
            //is character crouched but not at the crouch height?
            if (_isCrouched & _camRootParent.localPosition.y  > _crouchHeight )
            {

                Vector3 newHeight = _camRootParent.transform.localPosition + Vector3.down * (Mathf.Round(Time.deltaTime * 100)/100) *_cameraHeightTransitionSpeed;

                if (Mathf.Round(newHeight.y * 100) < Mathf.Round(_crouchHeight * 100))
                    _camRootParent.localPosition = new Vector3(_camRootParent.transform.localPosition.x, _crouchHeight, _camRootParent.transform.localPosition.z);
                else
                    _camRootParent.localPosition = newHeight;

            }

            //revert the cam to the player's original height
            else if (!_isCrouched && _camRootParent.localPosition.y < _originalPlayerHeight)
            {
                Vector3 newHeight = _camRootParent.transform.localPosition + Vector3.up * (Mathf.Round(Time.deltaTime * 100) / 100) * _cameraHeightTransitionSpeed;

                if (Mathf.Round(newHeight.y * 100) > Mathf.Round(_originalPlayerHeight * 100))
                    _camRootParent.localPosition = new Vector3(_camRootParent.transform.localPosition.x, _originalPlayerHeight, _camRootParent.transform.localPosition.z);
                else
                    _camRootParent.localPosition = newHeight;
            }
        }







        //gizmo-related
        private void DrawUncrouchAvailability()
        {
            if (_controller != null && _crouchGizmo != null)
            {
                if (_isUncrouchAvailable)
                    _crouchGizmo.SetColor(_successColor);
                else _crouchGizmo.SetColor(_missColor);


                Vector3 playerMiddle = transform.position + Vector3.up * _originalPlayerHeight / 2;
                Vector3 castSize = new Vector3((_controller.radius + _crouchCastTweak), _originalPlayerHeight, (_controller.radius + _crouchCastTweak));
                //Gizmos.DrawWireCube(playerMiddle, castSize *2);
                _crouchGizmo.SetSize(castSize);
                _crouchGizmo.SetPosition(playerMiddle);
                

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