using System.Collections.Generic;
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
        private bool _wasSprintingPreviously = false;
        [SerializeField] private bool _isSprintAvailable = true;
        [Tooltip("Rotation speed of the character")]
        [SerializeField] private float _rotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        [SerializeField] private float _speedChangeRate = 10.0f;
        private Vector3 _moveDirection;
        private float _speed;
        private float _rotationVelocity;

        [Header("Parkour")]
        [SerializeField] private WallClimber _wallClimber;
        [Tooltip("Is the climbing mechanic enabled for the player to use")]
        [SerializeField] private bool _isClimbEnabled = true;
        [Space(10)]
        [SerializeField] private LedgeType _detectedLedgeType = LedgeType.unset;
        [SerializeField] private Vector3 _ledgePosition;
        [Space(10)]
        [SerializeField] private LedgeType _transitionType = LedgeType.unset;
        [SerializeField] private Vector3 _transitionEndPoint = Vector3.negativeInfinity;
        private Vector3 _transitionStartPoint;
        private bool _transitionComplete = false;
        [SerializeField] private float _highTransitionSpeed = 1;
        [SerializeField] private float _midTransitionSpeed = 1;
        [SerializeField] private float _lowTransitionSpeed = 1;
        [SerializeField] private bool _midwayPointReached = false;

        //Wall Hanging
        [Space(10)]
        [Tooltip("The point the player is offset from to emulate wall hanging. " +
            "This point will be aligned with the ledge point when a high grab occurs")]
        [SerializeField] private Transform _wallHangOffsetOrigin;
        [Tooltip("The vector that guides the player into a wall hang ")]
        private Vector3 _vectorFromWallHangOriginToHighLedge;
        [Tooltip("The player's origin while wall hanging")]
        private Vector3 _originalHangPosition;
        [Tooltip("How long transitioning into a wall hanging state should take")]
        [SerializeField] private float _enterHangDuration = .2f;
        [Tooltip("A buffer time before reading the player's next Input after entering a wall hang")]
        [SerializeField] private float _wallHangInputDelay = .2f;
        private bool _ignoreWallHangDelay = false;
        private bool _isWallHangInputDelayComplete = true;
        private float _currentEnterHangTime = 0;
        [Tooltip("Is the player hanging from a ledge?")]
        [SerializeField]private bool _isWallHanging = false;
        private bool _isClimbingOver = false;
        [Tooltip("Is the player ready to grab high ledges? This enters a cooldown when the player releases a ledge")]
        [SerializeField] private bool _isHighGrabReady = true;
        [Tooltip("How long the system waits before reenabling the player's ledge grab ability")]
        [SerializeField] private float _highGrabCooldownDuration = .33f;
        [Tooltip("How long the player takes to Peek over the ledge")]
        [SerializeField] private float _peekDuration = .33f;
        [Tooltip("How high the player lifts to peek over a ledge")]
        [SerializeField] private float _peekOffset = 1;
        private float _currentPeekTime = 0;
        private float _currentPeekOffset = 0;
        private Vector3 _wallNormal;
        private Vector3 _wallRightDirection;
        private List<Vector3> _nearbyLedgePoints = new();
        private Vector3 _horizontalClimbDirection = Vector3.zero;
        [SerializeField] private float _wallHangClimbSpeed = 1;
        [SerializeField] private List<Vector3> _detectedClimbingPoints = new();
        private List<Vector3> _tempClimbPointsList = new();



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
        private bool _ignoreNextLandingSound = false;

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

        public event MovementEvent OnLowTransitionEnter;
        public event MovementEvent OnLowTransitionExit;

        public event MovementEvent OnMidTransitionEnter;
        public event MovementEvent OnMidTransitionExit;

        public event MovementEvent OnWallHangEntered;
        public event MovementEvent OnWallClimbOverTriggered;
        public event MovementEvent OnWallHangExited;


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

        private void Update()
		{
			DetermineCharacterStates();
			ApplyVelocities();
            if (_movementState == MovementState.General)
			    MoveCharacter();
			
		}

        private void FixedUpdate()
        {
            if (_movementState == MovementState.Climbing)
            {
                Climb();
            }
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
		private void DetermineCharacterStates()
		{
            if ( _movementState == MovementState.General)
            {
                UpdateGroundedStates();
                UpdateSprintState();
                UpdateLandingSound();
                Crouch();
                UpdateCameraHeight();
                EnterClimb();
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
            if (_isSprintAvailable && _input.sprint && (!_isCrouched && !_isCrouchTransitioning))
            {
                
                _isSprinting = true;

                //set a flag that determines this sprint's start,
                //and raise the sprint's starting event
                if (!_wasSprintingPreviously)
                {
                    _wasSprintingPreviously = true;
                    OnRunEnter?.Invoke();
                }
            }
            else
            {
                _isSprinting = false;

                //reset the flag that determine's the sprint start,
                //and raise the sprint's ending event
                if (_wasSprintingPreviously)
                {
                    _wasSprintingPreviously = false;
                    OnRunExit?.Invoke();
                }
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

        
        private void InterruptGeneralMovementStateUtilities()
        {
            //reset sprint utils
            if (_isSprinting)
            {
                _isSprinting = false;
                _wasSprintingPreviously = false;
                OnRunExit?.Invoke();
            }

            //reset jump
            _jumpTimeoutCompleted = false;
            _jumpTimeoutDelta = _jumpTimeout;
        }
        private void InitBaseClimbStates()
        {
            
            //Change the movement state
            _movementState = MovementState.Climbing;

            //specify the transition state
            _transitionType = _detectedLedgeType;
            _transitionStartPoint = transform.position;
            _transitionEndPoint = _ledgePosition;
            _transitionComplete = false;
            _midwayPointReached = false;

            //Clear any velocity utils
            _controller.SimpleMove(Vector3.zero);
            _controller.enabled = false;
            
            //Reset the Jump utilities
            _verticalVelocity = 0;
            UpdateLandingSound();
        }


        private void EnterClimb()
        {
            //if the mechanic is enabled and we aren't already climbing
            if (_isClimbEnabled && _movementState != MovementState.Climbing)
            {
                //Check if a ledge is available to climb
                if (!_wallClimber.IsLedgeAvailable())
                    return;
                else
                {
                    _ledgePosition = _wallClimber.GetClosestLedgePoint();
                    _detectedLedgeType = _wallClimber.DetermineLedgeType(_ledgePosition);
                    _wallNormal = _wallClimber.GetFaceNormal(_ledgePosition, transform.TransformDirection(Vector3.forward));
                    
                    if (_wallNormal.y == float.NegativeInfinity)
                        Debug.LogWarning("Failed to catch the ledge's normal face. Therefore not calculating the ledge's horizontal move direction line");
                    else _wallRightDirection = Vector3.Cross(_wallNormal, Vector3.down);
                }

                //Detect Sprint-based Climb contexts
                //valid contexts: Low, Mid, Special High Case
                if (_isSprinting && _logicallyGrounded && _input.move.y == 1)
                {
                    //enter the low or mid if sprinting into the context
                    if (_detectedLedgeType == LedgeType.low || _detectedLedgeType == LedgeType.mid)
                    {
                        InitBaseClimbStates();

                        //Clear any other generalStates that shouldn't continue
                        InterruptGeneralMovementStateUtilities();

                        //trigger the respective enter event
                        if (_detectedLedgeType == LedgeType.low)
                            OnLowTransitionEnter?.Invoke();
                        else if (_detectedLedgeType == LedgeType.mid)
                            OnMidTransitionEnter?.Invoke();
                    }
                    
                    //enter the ledge climb context if also pressing jump (while not in ledgeGrab cooldown)
                    else if (_input.jump && _detectedLedgeType == LedgeType.high && _isHighGrabReady)
                    {
                        InitBaseClimbStates();

                        //Clear any other generalStates that shouldn't continue
                        InterruptGeneralMovementStateUtilities();

                        SetupLedgeGrab();

                        //ignore the input delay. Climbs are faster when not falling into them
                        _ignoreWallHangDelay = true;

                        //onEnter triggers when the player complete's the intro wallHang transition
                        //not here, not yet
                    }

                }

                //Detect Falling Climb Contexts
                //valid contexts: High (with an input delay) ===> Implement Mid ledges later 
                else if (!_logicallyGrounded && _verticalVelocity < 0)
                {
                    //enter the ledge climb context if we caught one during a fall
                    if (_detectedLedgeType == LedgeType.high && _isHighGrabReady)
                    {
                        InitBaseClimbStates();

                        //Clear any other generalStates that shouldn't continue
                        InterruptGeneralMovementStateUtilities();

                        SetupLedgeGrab();

                        //Climbs are slower when falling into them
                        _ignoreWallHangDelay = false;
                    }
                }
            }
        }

        private void SetupLedgeGrab()
        {
            //Calculate the offset from our set wallHangPosition to the detected ledge point
            _vectorFromWallHangOriginToHighLedge = _ledgePosition - _wallHangOffsetOrigin.position;

            _isWallHanging = false;
            _currentEnterHangTime = 0;

            

            //reset the input delay setting, to avoid unintended, auto ledge climbing
            _isWallHangInputDelayComplete = false;
            
        }

        private void CompleteWallHangInputDelay()
        {
            _isWallHangInputDelayComplete = true;
            //Debug.Log($"Input Delay Completed: Waited {_wallHangInputDelay} seconds");
        }

        private void Climb()
        {
            //low ledge transition
            if (_transitionType == LedgeType.low)
            {
                if (!_transitionComplete)
                {
                    MoveUpAndOver();
                }
                else
                {
                    _movementState = MovementState.General;
                    _transitionType = LedgeType.unset;
                    _transitionStartPoint = Vector3.negativeInfinity;
                    _transitionEndPoint = Vector3.negativeInfinity;
                    _controller.enabled = true;
                    OnLowTransitionExit?.Invoke();
                }
            }

            //mid ledge transisiton
            else if (_transitionType == LedgeType.mid)
            {
                if (!_transitionComplete)
                {
                    MoveUpAndOver();
                }
                else
                {
                    _movementState = MovementState.General;
                    _transitionType = LedgeType.unset;
                    _transitionStartPoint = Vector3.negativeInfinity;
                    _transitionEndPoint = Vector3.negativeInfinity;
                    _controller.enabled = true;
                    OnMidTransitionExit?.Invoke();
                }
            }

            //high ledge transition
            else if (_transitionType == LedgeType.high)
            {
                //transition into the wall hanging state first, before anything else
                if (_isWallHanging == false)
                {
                    _currentEnterHangTime += Time.deltaTime;

                    //move the player into the hanging position
                    transform.position = Vector3.Lerp(_transitionStartPoint, _transitionStartPoint + _vectorFromWallHangOriginToHighLedge, _currentEnterHangTime / _enterHangDuration);

                    if (_currentEnterHangTime >= _enterHangDuration)
                    {
                        _currentEnterHangTime = 0;
                        _isWallHanging = true;
                        _originalHangPosition = transform.position;
                        OnWallHangEntered?.Invoke();
                        

                        // provide the player some time to release the climb control
                        // in case they only want to ledge hang, or if they fell into it
                        if (_ignoreWallHangDelay)
                        {
                            _ignoreWallHangDelay = false;
                            _isWallHangInputDelayComplete = true;
                        }

                        else 
                            Invoke(nameof(CompleteWallHangInputDelay), _wallHangInputDelay); 
                    }
                }
                else
                {
                    //climb over if the player already triggered the climb over
                    if (_isClimbingOver)
                    {
                        //Lerp the player up and over the ledge over time
                        if (!_transitionComplete)
                            MoveUpAndOver();

                        // Exit the climb state.
                        else
                        {
                            _isClimbingOver = false;
                            _isWallHanging = false;
                            _isHighGrabReady = false;

                            //cancel any previous ticking input delays
                            CancelInvoke(nameof(CompleteWallHangInputDelay));

                            //trigger highgrab cooldown
                            Invoke(nameof(ReadyHighGrabCooldown), _highGrabCooldownDuration);

                            //leave the climbing state and fall
                            _movementState = MovementState.General;
                            _transitionType = LedgeType.unset;
                            _transitionStartPoint = Vector3.negativeInfinity;
                            _transitionEndPoint = Vector3.negativeInfinity;
                            _controller.enabled = true;

                            OnWallHangExited?.Invoke();
                        }
                    }

                    
                    //Only after the input delay completes
                    else if (_isWallHangInputDelayComplete)
                    {
                        //drop off the ledge if the player [moves backwards + sprint btn]
                        if (_input.move.y < 0 && _input.sprint)
                        {
                            _currentPeekTime = 0;
                            _isClimbingOver = false;
                            _isWallHanging = false;
                            _isHighGrabReady = false;

                            //cancel any previous ticking input delays
                            CancelInvoke(nameof(CompleteWallHangInputDelay));

                            //trigger highgrab cooldown
                            Invoke(nameof(ReadyHighGrabCooldown), _highGrabCooldownDuration);

                            //leave the climbing state and fall
                            _movementState = MovementState.General;
                            _transitionType = LedgeType.unset;
                            _transitionStartPoint = Vector3.negativeInfinity;
                            _transitionEndPoint = Vector3.negativeInfinity;
                            _controller.enabled = true;

                            OnWallHangExited?.Invoke();
                            return;
                        }

                        //trigger the climb over transition if the player [moves forwards + sprint btn]
                        else if (_input.move.y > 0 && _input.sprint && _wallClimber.IsPointStandable(_ledgePosition))
                        {
                            //Check if the spot ahead is clear before triggering climb up
                            _currentPeekTime = 0;
                            _isClimbingOver = true;
                            OnWallClimbOverTriggered?.Invoke();
                            return;
                        }

                        //pull the player up to peek over the ledge if moving forwards
                        if (_input.move.y > 0)
                        {
                            //are we NOT YET at the peak peek?
                            if (_currentPeekTime < _peekDuration)
                            {
                                _currentPeekTime += Time.deltaTime;

                                transform.position = Vector3.Lerp(_originalHangPosition, _originalHangPosition + new Vector3(0, _peekOffset, 0), _currentPeekTime / _peekDuration);
                            }
                            else
                            {
                                _currentPeekTime = _peekDuration;
                            }
                        }

                        //decline the player from the ledge if not moving forwards (downwards OR non-input)
                        else if (_input.move.y <= 0)
                        {
                            //are we NOT YET at the peak peek?
                            if (_currentPeekTime > 0)
                            {
                                _currentPeekTime -= Time.deltaTime;

                                transform.position = Vector3.Lerp(_originalHangPosition, _originalHangPosition + new Vector3(0, _peekOffset, 0), _currentPeekTime / _peekDuration);
                            }
                            else
                            {
                                _currentPeekTime = 0;
                            }
                        }

                        //Move while hanging if moving horizontally
                        if (_input.move.x > 0.1f || _input.move.x < -0.1f)
                        {

                            //identify the direction
                            if (_input.move.x > 0)
                                _horizontalClimbDirection = -_wallRightDirection;
                            else _horizontalClimbDirection = _wallRightDirection;


                            //Move in the movement direction, if there's a ledge in that direction
                            if (_wallClimber.IsHorizontalClimbingAvailable(_horizontalClimbDirection,_wallHangClimbSpeed,-_wallNormal,_ledgePosition,out _tempClimbPointsList))
                            {
                                
                                //accept all the new ledge points
                                foreach (Vector3 point in _tempClimbPointsList)
                                {
                                    if (!_detectedClimbingPoints.Contains(point))
                                        _detectedClimbingPoints.Add(point);
                                }

                                //Move in the climb direction
                                Vector3 movement = _horizontalClimbDirection * _wallHangClimbSpeed * Time.deltaTime;
                                transform.position += movement;

                                //update the climb utilities
                                _ledgePosition += movement;
                                _originalHangPosition += movement;
                                _transitionStartPoint += movement;
                                _transitionEndPoint += movement;
                            }


                        }

                        /* Old Ledge Scan
                        //visualize the player's current Hang Position
                        Debug.DrawLine(_originalHangPosition, _originalHangPosition + _wallNormal.normalized * 4, Color.blue, .5f);

                        //detect for adjacent ledgePoints
                        Vector3 startScanPoint = _ledgePosition + (-_wallRightDirection * 1f); // to the left of the origin by .5f  
                        Vector3 endScanPoint = _ledgePosition + (_wallRightDirection * 1f); // to the right of the origin by .5f
                        
                        //draw start boundary
                        Debug.DrawLine(startScanPoint, startScanPoint + _wallNormal.normalized * 4, Color.yellow, .5f);
                        //draw end boundary
                        Debug.DrawLine(endScanPoint, endScanPoint + _wallNormal.normalized * 4, Color.red, .5f);

                        //Fix this: Why are no ledge points being detected?
                        _nearbyLedgePoints = _wallClimber.ScanForLedgePointsAlongLine(startScanPoint, endScanPoint, -_wallNormal.normalized, 10);
                        Debug.Log($"Detected Ledge Points: {_nearbyLedgePoints.Count}");
                        */


                    }
                    

                }
                
            }
        }

        private void ReadyHighGrabCooldown()
        {
            _isHighGrabReady = true;
        }

        private void MoveUpAndOver()
        {

            Vector3 MidwayPoint = new Vector3(_transitionStartPoint.x, _transitionEndPoint.y, _transitionStartPoint.z);

            float transitionSpeed = 0;
            if (_detectedLedgeType == LedgeType.low)
                transitionSpeed = _lowTransitionSpeed * Time.deltaTime;
            else if (_detectedLedgeType == LedgeType.mid)
                transitionSpeed = _midTransitionSpeed * Time.deltaTime;
            else if (_detectedLedgeType == LedgeType.high)
                transitionSpeed = _highTransitionSpeed * Time.deltaTime;

            if (!_midwayPointReached)
            {
                transform.position = Vector3.MoveTowards(transform.position, MidwayPoint, transitionSpeed);

                if (transform.position == MidwayPoint)
                    _midwayPointReached = true;
            }
                
            else if (!_transitionComplete)
            {
                transform.position = Vector3.MoveTowards(transform.position, _transitionEndPoint, transitionSpeed);
                
                if (transform.position == _transitionEndPoint)
                    _transitionComplete = true;
            }

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