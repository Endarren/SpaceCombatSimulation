﻿using Assets.Src.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Src.Targeting;
using UnityEngine;
using Assets.Src.ObjectManagement;

namespace Assets.Src.Rocket
{
    class RocketEngineControl : IRocketEngineControl
    {
        public float RemainingFuel { get; private set; }
        public float SlowdownWeighting { get; set; }
        public float LocationAimWeighting { get; set; }


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

        private List<Rigidbody> _engines = new List<Rigidbody>();
        private List<ParticleSystem> _enginePlumes = new List<ParticleSystem>();

        private Vector3 _mainEngineForce;
        private float _tanShootAngle;

        public int _startDelay = 0;
        private int _turningStartDelay;

        private ITorqueApplier _torqueApplier;

        public void AddEngine(Rigidbody engine, bool alsoATorqer = true)
        {
            _engines.Add(engine);
            var plume = engine.transform.Find("EnginePlume").GetComponent<ParticleSystem>();
            _enginePlumes.Add(plume);
            plume.Stop();
            if(alsoATorqer)
                _torqueApplier.AddTorquer(engine);
        }

        public RocketEngineControl(ITorqueApplier torqueApplier, Rigidbody engine, float tanShootAngle, float engineForce, float fuel, int startDelay)
        {
            _torqueApplier = torqueApplier;
            _tanShootAngle = tanShootAngle;
            _mainEngineForce = new Vector3(0, 0, engineForce);
            RemainingFuel = fuel;
            _startDelay = startDelay;
            SlowdownWeighting = 10;
            LocationAimWeighting = 1;

            AddEngine(engine);
        }

        public RocketEngineControl(ITorqueApplier torqueApplier, List<Rigidbody> engines, float tanShootAngle, float engineForce, float fuel, int startDelay)
        {
            _torqueApplier = torqueApplier;
            _tanShootAngle = tanShootAngle;
            _mainEngineForce = new Vector3(0, 0, engineForce);
            RemainingFuel = fuel;
            _startDelay = startDelay;
            SlowdownWeighting = 10;
            LocationAimWeighting = 1;

            foreach (var engine in engines.ToList())
            {
                AddEngine(engine);
            }
        }

        public void FlyAtTargetMaxSpeed(PotentialTarget target)
        {
            RemoveNullEngines();
            if (ShouldTurn())
            {
                var reletiveLocation = VectorTowardsTargetInWorldSpace(target);
                var cancelationVector = VectorToCancelLateralVelocityInWorldSpace(target);

                var turningVector = cancelationVector + (reletiveLocation * LocationAimWeighting);

                _torqueApplier.TurnToVectorInWorldSpace(turningVector);

                if (HasFuel())
                {
                    //try firing the main engine even with no fuel to turn it off if there is no fuel.
                    FireMainEngine(IsAimedAtWorldVector(turningVector));
                    return;
                }

                //use fuel for counter of frames with no fuel.
                RemainingFuel--;
            }
        }

        public void FlyToTarget(PotentialTarget target, float approachVelocity = 0, float absoluteLocationTollerance = 20, float velocityTollerance = 1)
        {
            RemoveNullEngines();
            if (ShouldTurn())
            {
                var reletiveLocation = VectorTowardsTargetInWorldSpace(target);
                var needsToMoveToTarget = reletiveLocation.magnitude > absoluteLocationTollerance;

                var moveTowardsTargetVector = needsToMoveToTarget ? reletiveLocation : Vector3.zero;

                var targetsVelosity = WorldSpaceReletiveVelocityOfTarget(target);
                var targetsSpeed = targetsVelosity.magnitude;
                var needsSlowdown = targetsSpeed > (approachVelocity + velocityTollerance);
                var slowdownVecor = needsSlowdown ? targetsVelosity : Vector3.zero;

                var closeEnough = !needsToMoveToTarget && !needsSlowdown;

                var turningVector = closeEnough
                    ? reletiveLocation
                    : (SlowdownWeighting * slowdownVecor) + VectorToCancelLateralVelocityInWorldSpace(target) + (moveTowardsTargetVector * LocationAimWeighting);

                //Debug.Log(
                //    "needsToMoveToTarget: " + needsToMoveToTarget +
                //    ", needsSlowdown: " + needsSlowdown +
                //    ", closeEnough: " + closeEnough +
                //    ", slowdownVecor: " + slowdownVecor +
                //    ", velocityTollerance: " + velocityTollerance +
                //    ", reletiveLocation: " + reletiveLocation +
                //    ", turningVector: " + turningVector);

                _torqueApplier.TurnToVectorInWorldSpace(turningVector);

                if (HasFuel())
                {
                    //try firing the main engine even with no fuel to turn it off if there is no fuel.
                    FireMainEngine(IsAimedAtWorldVector(turningVector) && !closeEnough);

                    return;
                }

                //use fuel for counter of frames with no fuel.
                RemainingFuel--;
            }
        }

        private bool ShouldTurn()
        {
            _turningStartDelay--;
            return _turningStartDelay <= 0;
        }

        private Vector3 CalculateSlowdownVector(PotentialTarget target, float approachVelocity, float absoluteLocationTollerance)
        {
            var targetsVelosity = WorldSpaceReletiveVelocityOfTarget(target);
            var targetsSpeed = targetsVelosity.magnitude;

            return targetsVelosity;
        }

        private bool HasFuel()
        {
            //Debug.Log("startDelay:" + StartDelay + ", RemainignFule:" + RemainingFuel);
            var hasFuel = RemainingFuel > 0 && StartDelay <= 0;
            if (!hasFuel)
            {
                //if(_torquers != _engines)
                //{
                //    Debug.Log("drag to 0, RF:" + RemainingFuel + ", sd:" + StartDelay);
                //}
                StartDelay--;
                _torqueApplier.Deactivate();
            }
            else
            {
                //Debug.Log("drag on");
                _torqueApplier.Activate();
            }
            return hasFuel;
        }

        private Vector3 VectorTowardsTargetInWorldSpace(PotentialTarget target)
        {
            if (_engines.FirstOrDefault() != null &&_engines.First().transform.IsValid() && target.Target.IsValid())
            {

                var location = target.Target.position - _engines.First().position;
                return location;
            }

            if (target.Target.IsInvalid())
            {
                Debug.Log("Target transform is invalid");
            }
            //if (_engines.FirstOrDefault() == null)
            //{
            //    Debug.Log("No Engines (VectorTowardsTargetInWorldSpace) ");
            //}
            return Vector3.zero;
        }

        private Vector3 VectorToCancelLateralVelocityInWorldSpace(PotentialTarget target)
        {
            var vectorTowardsTarget = VectorTowardsTargetInWorldSpace(target);
            var targetReletiveVelocity = WorldSpaceReletiveVelocityOfTarget(target);

            //https://math.stackexchange.com/questions/1455740/resolve-u-into-components-that-are-parallel-and-perpendicular-to-any-other-nonze

            var numerator = Vector3.Dot(targetReletiveVelocity, vectorTowardsTarget);
            var denominator = Vector3.Dot(vectorTowardsTarget, vectorTowardsTarget);
            var division = numerator / denominator;

            var perpendicularComponent = targetReletiveVelocity - (division * vectorTowardsTarget);

            return perpendicularComponent;
        }

        private Vector3 WorldSpaceReletiveVelocityOfTarget(PotentialTarget target)
        {
            var targetRigidBody = target.Target.GetComponent("Rigidbody") as Rigidbody;


            var targetsVelocity = targetRigidBody == null ? Vector3.zero : targetRigidBody.velocity;
            var ownVelocity = _engines.FirstOrDefault() != null ? _engines.First().velocity : Vector3.zero;
            return targetsVelocity - ownVelocity;
        }

        private void FireMainEngine(bool fire)
        {
            if (fire && HasFuel())
            {
                foreach (var engine in _engines)
                {
                    engine.AddRelativeForce(_mainEngineForce);
                    //every engine uses 1 fuel
                    RemainingFuel -= 1;
                }
                foreach (var plume in _enginePlumes)
                {
                    plume.Play();
                }
                return;
            }

            foreach (var plume in _enginePlumes)
            {
                plume.Stop();
            }
        }

        private void RemoveNullEngines()
        {
            _engines = _engines.Where(t => t != null).Distinct().ToList();
            _enginePlumes = _enginePlumes.Where(t => t != null).Distinct().ToList();
        }

        private bool IsAimedAtWorldVector(Vector3 worldSpaceVector)
        {
            if (_engines.FirstOrDefault() != null)
            {
                var localSpaceVector = _engines.First().transform.InverseTransformVector(worldSpaceVector);
                if (localSpaceVector.z < 0)
                {
                    //rocket is pointed away from target
                    return false;
                }
                var distance = localSpaceVector.z;
                localSpaceVector.z = 0;
                return localSpaceVector.magnitude < _tanShootAngle * distance;
            }

            //Debug.Log("No Engines (IsAimedAtWorldVector)");
            return false;
        }
    }
}
