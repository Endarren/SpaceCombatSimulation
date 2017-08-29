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

public class SpaceShipControler : MonoBehaviour, IKnowsEnemyTagAndtag, IDeactivatable
{
    public float ShootAngle = 30;
    public float TorqueMultiplier = 9;
    public float LocationAimWeighting = 1;
    public int StartDelay = 2;
    public float SlowdownWeighting = 10;
    public Rigidbody TargetMarker;
    public float LocationTollerance = 20;
    public float VelociyTollerance = 1;
    public Transform Engine;
    public Rigidbody Torquer;
    private List<Transform> _engines = new List<Transform>();
    private List<Rigidbody> _torquers = new List<Rigidbody>();

    public float AngularDragForTorquers = 20;

    private const float Fuel = Mathf.Infinity;
    private Rigidbody _marker;
    private SpaceshipRunner _runner;
    private Rigidbody _thisSpaceship;
    private bool _active = true;
    
    private IRocketEngineControl _engineControl;

    private string InactiveTag = "Untagged";
    public Transform VectorArrow;

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
        var _detector = new UnityTargetDetector()
        {
            EnemyTags = EnemyTags
        };

        var torqueApplier = new MultiTorquerTorqueAplier(_thisSpaceship, _torquers, TorqueMultiplier, AngularDragForTorquers);
        
        _engineControl = new RocketEngineControl(torqueApplier, _thisSpaceship, _engines, ShootAngle, Fuel, StartDelay)
        {
            LocationAimWeighting = LocationAimWeighting,
            SlowdownWeighting = SlowdownWeighting,
            VectorArrow = VectorArrow
        };

        _marker = Instantiate(TargetMarker);
        var chooser = new AverageTargetLocationDestinationChooser(_detector, _marker);

        _runner = new SpaceshipRunner(chooser, _engineControl, _marker)
        {
            LocationTollerance = LocationTollerance,
            VelociyTollerance = VelociyTollerance
        };

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
        _active = false;
        tag = InactiveTag;
    }

    public void RegisterEngine(Transform engine)
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
