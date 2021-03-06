﻿using Assets.Src.Interfaces;
using Assets.Src.Rocket;
using Assets.Src.Targeting;
using Assets.Src.Targeting.TargetPickers;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.Src.Pilots;
using Assets.Src.ObjectManagement;

public class RocketController : MonoBehaviour, IKnowsEnemyTagAndtag, IKnowsCurrentTarget
{
    public float ShootAngle = 10;
    public float TorqueMultiplier = 1f;
    public float LocationAimWeighting = 3f;
    public int StartDelay = 10;
    public int TurningStartDelay = 2;

    public float TimeToTargetForDetonation = 0.5f;
    public Rigidbody Shrapnel;
    public Rigidbody ExplosionEffect;
    public int ShrapnelCount = 10;
    public float ExplosionForce = 1;
    public float ShrapnelSpeed = 100;
    public float ExplosionDamage = 10000;
    public float ExplosionRadius = 20;
    //public bool ExplodeOnAnyCollision = true;

    private ITargetDetector _detector;
    private ITargetPicker _targetPicker;
    private IPilot _pilot;

    private Rigidbody _rigidbody;
    
    private IRocketRunner _runner;
    private IDetonator _detonator;
    public bool TagShrapnel = false;
    public bool SetEnemyTagOnShrapnel = false;
    public Transform VectorArrow;
    
    public List<EngineControler> Engines;
    
    #region TargetPickerVariables
    public float PickerDistanceMultiplier = 1;
    public float PickerInRangeBonus = 0;
    public float PickerRange = 500;
    public float PickerAimedAtMultiplier = 100;
    public float MinimumMass = 0;
    public float PickerMasMultiplier = 1;
    public float PickerOverMinMassBonus = 10000;
    public float PickerApproachWeighting = 20;
    #endregion

    #region EnemyTags
    public void AddEnemyTag(string newTag)
    {
        var tags = EnemyTags.ToList();
        tags.Add(newTag);
        EnemyTags = tags.Distinct().ToList();
    }

    public string GetFirstEnemyTag()
    {
        return EnemyTags.FirstOrDefault();
    }

    public void SetEnemyTags(List<string> allEnemyTags)
    {
        EnemyTags = allEnemyTags;
    }

    public List<string> GetEnemyTags()
    {
        return EnemyTags;
    }

    public List<string> EnemyTags;

    /// <summary>
    /// Rocket with detonate after this time.
    /// </summary>
    public float TimeToLive = Mathf.Infinity;
    #endregion
    
    #region knowsCurrentTarget
    public PotentialTarget CurrentTarget { get; set; }
    #endregion

    // Use this for initialization
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _detector = new MultiTagTargetDetector()
        {
            EnemyTags = EnemyTags
        };

        var pickers = new List<ITargetPicker>
        {
            new ProximityTargetPicker(_rigidbody){
                DistanceMultiplier = PickerDistanceMultiplier,
                InRangeBonus = PickerInRangeBonus,
                Range = PickerRange
            },
            new LookingAtTargetPicker(_rigidbody)
            {
                Multiplier = PickerAimedAtMultiplier
            },
            new ApproachingTargetPicker(_rigidbody, PickerApproachWeighting)
        };

        if (MinimumMass > 0 || PickerMasMultiplier != 0)
        {
            pickers.Add(new MassTargetPicker
            {
                MinMass = MinimumMass,
                MassMultiplier = PickerMasMultiplier,
                OverMinMassBonus = PickerOverMinMassBonus
            });
        }

        _targetPicker = new CombinedTargetPicker(pickers);

        var initialAngularDrag = _rigidbody.angularDrag;
        var torqueApplier = new MultiTorquerTorqueAplier(_rigidbody, TorqueMultiplier, initialAngularDrag);

        _pilot = new RocketPilot(torqueApplier, _rigidbody, Engines, ShootAngle, StartDelay)
        {
            LocationAimWeighting = LocationAimWeighting,
            TurningStartDelay = TurningStartDelay,
            VectorArrow = VectorArrow
        };

        var exploder = new ShrapnelAndDamageExploder(_rigidbody, Shrapnel, ExplosionEffect, ShrapnelCount)
        {
            ExplosionForce = ExplosionForce,
            EnemyTags = EnemyTags,
            TagShrapnel = TagShrapnel,
            SetEnemyTagOnShrapnel = SetEnemyTagOnShrapnel,
            ExplosionBaseDamage = ExplosionDamage,
            ShrapnelSpeed = ShrapnelSpeed,
            ExplosionRadius = ExplosionRadius
        };

        _detonator = new ProximityApproachDetonator(exploder, _rigidbody, TimeToTargetForDetonation, ShrapnelSpeed);

        _runner = new RocketRunner(_detector, _targetPicker, _pilot, _detonator, this)
        {
            name = transform.name
        };
        
        //Debug.Log("starting");
    }

    // Update is called once per frame
    void Update()
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
        TimeToLive--;
    }
}
