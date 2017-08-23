﻿using System;
using Assets.Src.Interfaces;
using UnityEngine;
using System.Linq;
using Assets.src.interfaces;
using Assets.src.targeting;
using Assets.Src.ObjectManagement;

namespace Assets.Src.Targeting
{
    public class ProximityDetonator : IDetonator
    {
        private readonly Rigidbody _exploderRigidbody;
        private readonly float _detonationDistance;

        private IExploder _exploder;

        [Obsolete("Use Dependency injection constructor instead.")]
        public ProximityDetonator(Rigidbody thisExploder, float detonationDistance, Rigidbody shrapnel, Rigidbody explosionEffect, string explodableTag, int shrapnelCount = 50)
        {
            _exploderRigidbody = thisExploder;
            _detonationDistance = detonationDistance;

            _exploder = new ShrapnelAndDamageExploder(_exploderRigidbody, shrapnel, explosionEffect, shrapnelCount);
        }
        public ProximityDetonator(IExploder exploder, Rigidbody exploderRigidBody, float detonationDistance)
        {
            _exploderRigidbody = exploderRigidBody;
            _detonationDistance = detonationDistance;
            _exploder = exploder;
        }

        public void AutoDetonate(PotentialTarget target)
        {
            if (ShouldDetonate(target))
            {
                DetonateNow();
            }
        }

        private bool ShouldDetonate(PotentialTarget target)
        {
            if(target == null || target.Target.IsInvalid())
            {
                return false;
            }
            var distance = target.DistanceToTurret(_exploderRigidbody.transform);
            return distance <= _detonationDistance;
        }

        public void DetonateNow()
        {
            _exploder.ExplodeNow();
        }
    }
}