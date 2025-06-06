using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public enum InteractionSound
{
    unset,
    fleshy
}

public class InteractionFeedbackController : MonoBehaviour
{
    //Declarations
    [SerializeField] private InteractionAudioData _interactionAudioData;
    [SerializeField] private FirstPersonController _fpController;
    [SerializeField] private AudioSource _rightHandAudioSource;
    [SerializeField] private AudioSource _leftHandAudioSource;
    private AudioSource _coreAudioSource; //plays any audio sources not specifically bound to a single hand

    private Transform _rightHandAudioSourceTransform;
    private Transform _leftHandAudioSourceTransform;
    
    [SerializeField] private Transform _rightHand;
    [SerializeField] private Transform _leftHand;





    //monobehaviors
    private void Awake()
    {
        _rightHandAudioSourceTransform =_rightHandAudioSource.transform;
        _leftHandAudioSourceTransform = _leftHandAudioSource.transform;

        _coreAudioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        //_fpController.OnMidTransitionEnter += TriggerDoubleHandSound;
        _fpController.OnWallHangEntered += TriggerLeftHandSound;
    }

    private void OnDisable()
    {
        //_fpController.OnMidTransitionEnter -= TriggerDoubleHandSound;
        _fpController.OnWallHangEntered -= TriggerLeftHandSound;
    }


    private void Update()
    {
        BindAudioSourcesToHands();
    }


    //internals
    private void BindAudioSourcesToHands()
    {
        //keep the audio sources bound to the hand's positions
        _rightHandAudioSourceTransform.position = _rightHand.position;
        _leftHandAudioSourceTransform.position = _leftHand.position;
    }

    private void TriggerDoubleHandSound()
    {

        _coreAudioSource.clip= _interactionAudioData.GetFleshyMultihitClip();
        _coreAudioSource.Play();
    }

    private void TriggerRightHandSound()
    {

        _rightHandAudioSource.clip = _interactionAudioData.GetFleshyAudioClip();
        _rightHandAudioSource.Play();
    }

    private void TriggerLeftHandSound()
    {
        _leftHandAudioSource.clip = _interactionAudioData.GetFleshyAudioClip();
        _leftHandAudioSource.Play();
    }



    //Externals







}
