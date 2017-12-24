using Assets.Src.Interfaces;
using Assets.Src.Rocket;
using Assets.Src.Targeting;
using Assets.Src.Targeting.TargetPickers;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.Src.Pilots;
using Assets.Src.ObjectManagement;
using System;
using System.Collections;

public class RocketController : MonoBehaviour
{
    public TargetChoosingMechanism TargetChoosingMechanism;
    public float ShootAngle = 10;
    public float TorqueMultiplier = 1f;
    public float LocationAimWeighting = 3f;

    [Tooltip("Delay until engines (including RCS) will start and warhead will arm")]
    public float StartDelay = 0.2f;

    [Tooltip("Delay until non-engine torquers will start")]
    public float TurningStartDelay = 0.1f;

    public float TimeToTargetForDetonation = 0.5f;
    public Rigidbody Shrapnel;
    public Rigidbody ExplosionEffect;
    public int ShrapnelCount = 10;
    
    public float ShrapnelSpeed = 100;
    
    private IPilot _pilot;

    private Rigidbody _rigidbody;
    
    private IRocketRunner _runner;
    private IDetonator _detonator;
    public bool TagShrapnel = false;
    public bool SetEnemyTagOnShrapnel = false;
    public Transform VectorArrow;
    
    public List<EngineControler> Engines;

    [Tooltip("Check for best targets every frame if true, otherwise only on target loss")]
    public bool ContinuallyCheckForTargets = false;

    [Tooltip("If set to true a target will be aquired once only, once lost the rocket will deactivate." +
        " Emulates rockets being told their target by their launcher at launch.")]
    public bool NeverRetarget = false;
    
    /// <summary>
    /// Rocket with detonate after this time.
    /// </summary>
    /// 
    [Tooltip("Time for the rocket to live in seconds.")]
    public float TimeToLive = Mathf.Infinity;

    [Tooltip("Time to friendly collision to activate maximum evasion")]
    public float TimeThresholdForMaximumEvasion = 2;

    [Tooltip("Time to friendly collision to activate medium evasion")]
    public float TimeThresholdForMediumEvasion = 4;

    [Tooltip("Time to friendly collision to activate minimal evasion")]
    public float TimeThresholdForMinimalEvasion = 6;

    [Tooltip("Frames to stay in evasion mode after no longer being on a collision course with a friendly.")]
    public float EvasionModeTime = 30;

    [Tooltip("Distance in front of the rocket to start looking for friendlies on a collision cource - useful to avoid detecting itself.")]
    public float MinimumFriendlyDetectionDistance = 4;

    public FuelTank Tank;
	/// <summary>
	/// The delay between each update.  If zero, no delay is used.
	/// </summary>
	public float updateEvery = 0f;
    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
        var initialAngularDrag = _rigidbody.angularDrag;
        var torqueApplier = new MultiTorquerTorqueAplier(_rigidbody, TorqueMultiplier, initialAngularDrag);

        _pilot = new RocketPilot(torqueApplier, _rigidbody, Engines, StartDelay)
        {
            LocationAimWeighting = LocationAimWeighting,
            TurningStartDelay = TurningStartDelay,
            VectorArrow = VectorArrow,
            TimeThresholdForMaximumEvasion = TimeThresholdForMaximumEvasion,
            TimeThresholdForMediumEvasion = TimeThresholdForMediumEvasion,
            TimeThresholdForMinimalEvasion = TimeThresholdForMinimalEvasion,
            EvasionModeTime = EvasionModeTime,
            MinimumFriendlyDetectionDistance = MinimumFriendlyDetectionDistance
        };

        var exploder = new ShrapnelExploder(_rigidbody, Shrapnel, ExplosionEffect, ShrapnelCount)
        {
            EnemyTags = TargetChoosingMechanism.EnemyTags,
            TagShrapnel = TagShrapnel,
            SetEnemyTagOnShrapnel = SetEnemyTagOnShrapnel,
            ShrapnelSpeed = ShrapnelSpeed
        };

        _detonator = new ProximityApproachDetonator(exploder, _rigidbody, TimeToTargetForDetonation, ShrapnelSpeed);

        _runner = new RocketRunner(TargetChoosingMechanism, _pilot, _detonator, Tank)
        {
            name = transform.name
        };
		StartCoroutine(RocketRoutine());
    }
	/// <summary>
	/// This coroutine does what the RocketController used to do in FixedUpdate.  It will not be updated as frequently, but it reduces the performace cost of doing it in FixedUpdate.
	/// </summary>
	/// <returns></returns>
	IEnumerator RocketRoutine ()
	{
		while(TimeToLive > 0f)
		{
			if (_runner != null)
			{
				_runner.RunRocket();
				TimeToLive -= Time.deltaTime;
				if (updateEvery == 0f)
					yield return null;
				else
					yield return new WaitForSeconds(updateEvery); //Coroutine waits for the number of seconds before resuming.  Allows other code to be processed while waiting.
			}
			else
			{
				Debug.Log("Runner is null! " + transform.name);
			}
		}
		_detonator.DetonateNow();

	}
	/*
    // Update is called once per frame
    void FixedUpdate()
    {
        if(_runner != null)
        {
            _runner.RunRocket();
        } else
        {
            Debug.Log("Runner is null! " + transform.name);
        }

        if(TimeToLive < 0)
        {
            _detonator.DetonateNow();
        }
        TimeToLive -= Time.deltaTime;
    }
	*/
}
