﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using Assets.Src.Interfaces;
using Assets.Src.Targeting;
using Assets.Src.Targeting.TargetPickers;
using Assets.Src.ObjectManagement;

public class ShipCam : MonoBehaviour, IKnowsCurrentTarget
{
    /// <summary>
    /// tag of a child object of a fhing to watch or follow.
    /// </summary>
    public List<string> MainTags = new List<string>{ "SpaceShip"};
    public List<string> SecondaryTags = new List<string>{ "Projectile" };
    private List<string> _tags = new List<string> { "SpaceShip", "Projectile" };

    /// <summary>
    /// Rotation speed multiplier
    /// </summary>
    public float RotationSpeed = 0.5f;

    /// <summary>
    /// transtlation speed multiplier. Higher values will be able to track faster objects, but may move from object to object too fast.
    /// </summary>
    public float TranslateSpeed = 2;

    /// <summary>
    /// This value times the speed of the followed object is added to the translate speed.
    /// </summary>
    public float FollowedObjectTranslateSpeedMultiplier = 0;

    /// <summary>
    /// rate at which the camera will zoom in and out.
    /// </summary>
    public float FocusMoveSpeed = 1;

    public Camera Camera;

    public float FocusAnglePower = -0.67f;
    public float FocusAngleMultiplier = 1000;
    public float SetbackIntercept = -70;
    public float SetBackMultiplier = 0.5f;
    
    public float ApproachTargetPickerWeighting = 20;

    /// <summary>
    /// Minimum mass of objects to follow or look at.
    /// </summary>
    public float MinimumMass = 0;

    /// <summary>
    /// added to the score of the currently followed object and other objectes with the same tag.
    /// Used when picking a target to look at, if the object being followed doensn't have its own target.
    /// </summary>
    public float AdditionalScoreForSameTagOrCurrentlyFllowed = -100000;

    /// <summary>
    /// The distance the camera is trying to zoom in to to see well.
    /// Should be private, but exposed for debuging reasons.
    /// </summary>
    public float _focusDistance = 0;

    /// <summary>
    /// when the parent is within this angle of looking at the watched object, the camera tself starts tracking.
    /// </summary>
    public float NearlyAimedAngle = 3;

    public float MinShowDistanceDistance = 20;

    private Rigidbody _rigidbody;
    private ITargetDetector _detector;

    private PotentialTarget _followedTarget;
    private PotentialTarget _targetToWatch;

    private ITargetPicker _watchPicker;
    private ITargetPicker _followPicker;

    private HasTagTargetPicker _tagPicker;
    private PreviousTargetPicker _currentlyFollowingPicker;
    public float DefaultFocusDistance = 200;
    public float IdleRotationSpeed = -0.05f;
    
    public Texture ReticleTexture;
    public Texture HealthFGTexture;
    public Texture HealthBGTexture;

    public ReticleState ShowReticles = ReticleState.ALL;

    public PotentialTarget CurrentTarget
    {
        get
        {
            return _followedTarget;
        }

        set
        {
            _followedTarget = value;
        }
    }

    // Use this for initialization
    void Start () {
        _rigidbody = GetComponent("Rigidbody") as Rigidbody;
        _detector = new ChildTagTargetDetector
        {
            Tags = _tags
        };

        _tagPicker = new HasTagTargetPicker(null);
        _currentlyFollowingPicker = new PreviousTargetPicker(this)
        {
            AdditionalScore = AdditionalScoreForSameTagOrCurrentlyFllowed
        };

        var watchPickers = new List<ITargetPicker>
        {
            _tagPicker,
            _currentlyFollowingPicker,
            new ProximityTargetPicker(transform)
        };

        if(_rigidbody != null)
        {
            watchPickers.Add(new LookingAtTargetPicker(_rigidbody));
        }

        if (MinimumMass > 0)
        {
            watchPickers.Add(new MassTargetPicker{
                MinMass = MinimumMass
            });
        }

        _watchPicker = new CombinedTargetPicker(watchPickers);
        
        var followPickers = new List<ITargetPicker>
        {
            new ProximityTargetPicker(transform)
        };

        if (_rigidbody != null)
        {
            followPickers.Add(new ApproachingTargetPicker(_rigidbody, ApproachTargetPickerWeighting));
        }

        if (MinimumMass > 0)
        {
            followPickers.Add(new MassTargetPicker
            {
                MinMass = MinimumMass
            });
        }
        _followPicker = new CombinedTargetPicker(followPickers);
    }
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyUp(KeyCode.R))
        {
            CycleReticleState();
        }
        if (Input.GetKeyUp(KeyCode.Z))
        {
            PickRandomToFollow();
        }
        else if(_followedTarget == null || _followedTarget.TargetTransform.IsInvalid())
        {
            PickBestTargetToFollow();
        }

        if (_followedTarget != null)
        {
            //Debug.Log("following " + _followedTarget.TargetTransform);
            var totalTranslateSpeed = TranslateSpeed;
            if (_followedTarget.TargetRigidbody != null && FollowedObjectTranslateSpeedMultiplier != 0)
            {
                totalTranslateSpeed += FollowedObjectTranslateSpeedMultiplier * _followedTarget.TargetRigidbody.velocity.magnitude;
            }
            transform.position = Vector3.Slerp(transform.position, _followedTarget.TargetTransform.position, Time.deltaTime * totalTranslateSpeed);

            PickTargetToWatch();
            if (_targetToWatch != null && _followedTarget.TargetTransform != _targetToWatch.TargetTransform && _targetToWatch.TargetTransform.IsValid())
            {
                //Debug.Log("Following " + _followedTarget.TargetTransform.name + ", Watching " + _targetToWatch.TargetTransform.name);
                //rotate enpty parent
                var direction = (_targetToWatch.TargetTransform.position - transform.position).normalized;
                var lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * RotationSpeed);

                //move the focus
                _focusDistance = Mathf.Lerp(_focusDistance, Vector3.Distance(transform.position, _targetToWatch.TargetTransform.position), Time.deltaTime * FocusMoveSpeed);

                if (Quaternion.Angle(lookRotation, transform.rotation) < NearlyAimedAngle)
                {
                    //rotate the camera itself - only if the parent is looking in vaguely the right direction.
                    direction = (_targetToWatch.TargetTransform.position - Camera.transform.position).normalized;
                    lookRotation = Quaternion.LookRotation(direction);
                    Camera.transform.rotation = Quaternion.Slerp(Camera.transform.rotation, lookRotation, Time.deltaTime * RotationSpeed * 0.3f);
                }
            }
            else
            {
                //Debug.Log("Nothing to watch");
                IdleRotation();
            }
        } else {
            //Debug.Log("Nothing to follow");
            IdleRotation();
        }
        var angle = Clamp((float)(FocusAngleMultiplier * Math.Pow(_focusDistance, FocusAnglePower)), 1, 90);
        Camera.fieldOfView = angle;
        var setBack = SetbackIntercept - _focusDistance * SetBackMultiplier;
        var camPosition = Camera.transform.localPosition;
        camPosition.z = setBack;
        Camera.transform.localPosition = camPosition;


        //DrawHealthBars();
    }

    public void OnGUI()
    {
        DrawHealthBars();
    }

    private void DrawHealthBars()
    {
        if(ShowReticles != ReticleState.NONE)
        {
            var targets = _detector.DetectTargets();

            foreach (var target in targets)
            {
                DrawSingleLable(target);
            }
        }
    }

    private void DrawSingleLable(PotentialTarget target)
    {
        // Find the 2D position of the object using the main camera
        Vector3 boxPosition = Camera.main.WorldToScreenPoint(target.TargetTransform.position);
        if (boxPosition.z > 0)
        {
            var distance = Vector3.Distance(transform.position, target.TargetTransform.position);

            // "Flip" it into screen coordinates
            boxPosition.y = Screen.height - boxPosition.y;

            //Draw the distance from the followed object to this object - only if it's suitably distant, and has no parent.
            if (distance > MinShowDistanceDistance && target.TargetTransform.parent == null)
            {
                GUI.Box(new Rect(boxPosition.x - 20, boxPosition.y + 25, 40, 40), Math.Round(distance).ToString());
            }

            var rect = new Rect(boxPosition.x - 50, boxPosition.y - 50, 100, 100);
            if (ReticleTexture != null)
                GUI.DrawTexture(rect, ReticleTexture);

            var healthControler = target.TargetTransform.GetComponent("HealthControler") as HealthControler;
            if (healthControler != null && healthControler.IsDamaged)
            {
                if (HealthBGTexture != null)
                    GUI.DrawTexture(rect, HealthBGTexture);
                if (HealthFGTexture != null)
                {
                    rect.width *= healthControler.HealthProportion;
                    GUI.DrawTexture(rect, HealthFGTexture);
                }
                //Debug.Log(boxPosition.z + "--x--" + boxPosition.x + "----y--" + boxPosition.y);
            }
        }
    }

    private void IdleRotation()
    {
        //Debug.Log("IdleRotation");
        transform.rotation *= Quaternion.Euler(transform.up * IdleRotationSpeed);
        _focusDistance = Mathf.Lerp(_focusDistance, DefaultFocusDistance, Time.deltaTime * FocusMoveSpeed);
    }

    private void PickTargetToWatch()
    {
        //Debug.Log("to watch");
        var knower = _followedTarget.TargetTransform.GetComponent("IKnowsCurrentTarget") as IKnowsCurrentTarget;
        if (knower != null && knower.CurrentTarget != null)
        {
            _targetToWatch = knower.CurrentTarget;
            //Debug.Log("Watching followed object's target: " + _targetToWatch.TargetTransform.name);
        } else
        {
            var targets = _detector.DetectTargets()
                .Where(t => t.TargetTransform.parent == null);  //Don't watch anything that still has a parent.
            targets = _watchPicker.FilterTargets(targets)
                .OrderByDescending(s => s.Score);
            //foreach (var item in targets)
            //{
            //    Debug.Log(item.TargetTransform.name + ": " + item.Score);
            //}
        
            _targetToWatch = targets
                .FirstOrDefault();
            //Debug.Log("Watching picked target: " + _targetToWatch.TargetTransform.name);
        }
    }

    private void PickBestTargetToFollow()
    {
        //Debug.Log("To Follow");
        var targets = _detector.DetectTargets()
            .Where(t => t.TargetTransform.parent == null);  //Don't follow anything that still has a parent.
        targets = _followPicker.FilterTargets(targets)
            .OrderByDescending(s => s.Score);
        //foreach (var item in targets)
        //{
        //    Debug.Log(item.TargetTransform.name + ": " + item.Score);
        //}

        _followedTarget = targets
            .FirstOrDefault();

        if(_followedTarget != null)
        {
            _tagPicker.Tag = _followedTarget.TargetTransform.tag;
        }
    }

    private void PickRandomToFollow()
    {
        _followedTarget = _detector
            .DetectTargets()
            .Where(s => s.TargetTransform.parent == null && s.TargetTransform != _followedTarget.TargetTransform)
            .OrderBy(s => UnityEngine.Random.value)
            .FirstOrDefault();
    }

    public static float Clamp(float value, float min, float max)
    {
        return (value < min) ? min : (value > max) ? max : value;
    }
    
    private void CycleReticleState()
    {
        switch (ShowReticles)
        {
            case ReticleState.NONE:
                ShowReticles = ReticleState.ALL;
                _tags = new List<string>();
                _tags.AddRange(MainTags);
                _tags.AddRange(SecondaryTags);
                _detector = new ChildTagTargetDetector
                {
                    Tags = _tags
                };
                break;
            case ReticleState.ALL:
                ShowReticles = ReticleState.MAIN;
                _tags = MainTags;
                _detector = new ChildTagTargetDetector
                {
                    Tags = _tags
                };
                break;
            case ReticleState.MAIN:
                ShowReticles = ReticleState.NONE;
                break;
        }
        Debug.Log(ShowReticles);
    }

    public enum ReticleState
    {
        NONE,MAIN,ALL
    }
}
