﻿using Assets.Src.Interfaces;
using Assets.Src.ObjectManagement;
using Assets.Src.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Src.Pilots
{
    public abstract class BasePilot : IPilot
    {
        public float CloseEnoughAngle = 0;

        public float LocationAimWeighting { get; set; }
        public Transform VectorArrow;

        public int StartDelay
        {
            get
            {
                return _startDelay;
            }
            set
            {
                _startDelay = value;
            }
        }

        public int TurningStartDelay
        {
            get
            {
                return _turningStartDelay;
            }
            set
            {
                _turningStartDelay = value;
            }
        }
        private int _startDelay = 0;
        private int _turningStartDelay;

        protected List<EngineControler> _engines = new List<EngineControler>();

        protected bool ShouldTurn()
        {
            TurningStartDelay--;
            StartDelay--;
            return TurningStartDelay <= 0;
        }

        protected float AngleTollerance;

        protected ITorqueApplier _torqueApplier;

        protected Rigidbody _pilotObject;

        protected bool HasStarted()
        {
            //Debug.Log("RemainignFule:" + RemainingFuel);
            var hasFuel = StartDelay <= 0;
            if (!hasFuel)
            {
                _torqueApplier.Deactivate();
            }
            else
            {
                _torqueApplier.Activate();
            }
            return hasFuel;
        }

        protected Vector3 ReletiveLocationInWorldSpace(PotentialTarget target)
        {
            if (_pilotObject != null && target != null && target.TargetTransform.IsValid())
            {
                var location = target.TargetTransform.position - _pilotObject.position;
                return location;
            }

            //if (target == null || target.TargetTransform.IsInvalid())
            //{
            //    Debug.Log("Target transform is invalid");
            //}
            if (_pilotObject == null)
            {
                Debug.Log("_pilotObject is null");
            }
            return Vector3.zero;
        }

        protected Vector3 VectorToCancelLateralVelocityInWorldSpace(PotentialTarget target)
        {
            var vectorTowardsTarget = ReletiveLocationInWorldSpace(target);
            var targetReletiveVelocity = WorldSpaceReletiveVelocityOfTarget(target);

            return targetReletiveVelocity.ComponentPerpendicularTo(vectorTowardsTarget);
        }

        protected Vector3 WorldSpaceReletiveVelocityOfTarget(PotentialTarget target)
        {
            if (target == null)
            {
                return Vector3.zero;
            }
            var targetsVelocity = target.TargetRigidbody == null ? Vector3.zero : target.TargetRigidbody.velocity;
            var ownVelocity = _pilotObject.velocity;
            return targetsVelocity - ownVelocity;
        }

        protected void SetFlightVectorOnEngines(Vector3? FlightVector)
        {
            foreach (var engine in _engines)
            {
                engine.FlightVector = FlightVector;
            }
        }

        protected void RemoveNullEngines()
        {
            _engines = _engines.Where(t => t != null).Distinct().ToList();
        }

        protected bool IsAimedAtWorldVector(Vector3 worldSpaceVector)
        {
            if (_pilotObject != null)
            {
                var angle = Vector3.Angle(_pilotObject.transform.forward, worldSpaceVector);
                return angle < AngleTollerance;
            }

            //Debug.Log("No Engines (IsAimedAtWorldVector)");
            return false;
        }
        
        public void AddEngine(EngineControler engine)
        {
            _engines.Add(engine);
        }

        public abstract void Fly(PotentialTarget target);
    }
}
