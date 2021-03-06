﻿using Assets.Src.Interfaces;
using Assets.Src.ObjectManagement;
using Assets.Src.Rocket;
using Assets.Src.SpaceShip;
using Assets.Src.Targeting;
using Assets.Src.Targeting.TargetPickers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Assets.Src.Pilots;

public class SpaceShipControler : MonoBehaviour, IKnowsEnemyTagAndtag, IDeactivatable, IKnowsCurrentTarget
{
    public float ShootAngle = 30;
    public float TorqueMultiplier = 9;
    public int StartDelay = 2;
    //public Rigidbody TargetMarker;

    public float SlowdownWeighting = 10;
    public float RadialSpeedThreshold = 10;
    public float MaxRange = 100;
    public float MinRange = 20;
    public float LocationAimWeighting = 1;
    public float MaxTangentialVelocity = 10;
    public float MinTangentialVelocity = 0;
    public float TangentialSpeedWeighting = 1;

    public EngineControler Engine;
    public Rigidbody Torquer;
    private List<EngineControler> _engines = new List<EngineControler>();
    private List<Rigidbody> _torquers = new List<Rigidbody>();

    public float AngularDragForTorquers = 20;

    private const float Fuel = Mathf.Infinity;
    private SpaceshipRunner _runner;
    private Rigidbody _thisSpaceship;
    private bool _active = true;

    private IPilot _pilot;

    private string InactiveTag = "Untagged";
    public Transform VectorArrow;

    #region TargetPickerVariables
    public float PickerDistanceMultiplier = 1;
    public float MinimumMass = 80;
    public float PickerMasMultiplier = 1;
    public float PickerOverMinMassBonus = 10000;
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
    #endregion

    #region knowsCurrentTarget
    public PotentialTarget CurrentTarget {get;set;}
    #endregion

    // Use this for initialization
    void Start()
    {
        if(Engine != null)
        {
            _engines.Add(Engine);
        }
        if (Torquer != null)
        {
            _torquers.Add(Torquer);
        }
        Initialise();
    }

    private void Initialise()
    {
        _thisSpaceship = GetComponent<Rigidbody>();
        var _detector = new MultiTagTargetDetector()
        {
            EnemyTags = EnemyTags
        };

        var torqueApplier = new MultiTorquerTorqueAplier(_thisSpaceship, _torquers, TorqueMultiplier, AngularDragForTorquers);
        
        _pilot = new SpaceshipPilot(torqueApplier, _thisSpaceship, _engines, ShootAngle, Fuel)
        {
            StartDelay = StartDelay,
            SlowdownWeighting = SlowdownWeighting,
            TangentialSpeedWeighting = TangentialSpeedWeighting,
            LocationAimWeighting = LocationAimWeighting,
            VectorArrow = VectorArrow,
            MaxRange = MaxRange,
            MinRange = MinRange,
            MaxTangentialSpeed = MaxTangentialVelocity,
            MinTangentialSpeed = MinTangentialVelocity,
            RadialSpeedThreshold = RadialSpeedThreshold
        };

        var pickers = new List<ITargetPicker>
        {
            new ProximityTargetPicker(_thisSpaceship){
                DistanceMultiplier = PickerDistanceMultiplier
            }
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

        var picker = new CombinedTargetPicker(pickers);
        
        _runner = new SpaceshipRunner(_detector, picker, _pilot, this);

        foreach (var engine in _engines)
        {
            engine.tag = tag;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_active && _runner != null)
            _runner.RunSpaceship();
    }
    
    public void Deactivate()
    {
        //Debug.Log("Deactivating " + name);
        _active = false;
        tag = InactiveTag;
    }

    public void RegisterEngine(EngineControler engine)
    {
        //Debug.Log("Registering engine");
        _engines.Add(engine);
        Initialise();
        //_engineControl.SetEngine(Engine);

    }

    public void RegisterTorquer(Transform torquer)
    {
        _torquers.Add(torquer.GetComponent<Rigidbody>());
        Initialise();
        //_engineControl.SetEngine(Engine);
    }
}
