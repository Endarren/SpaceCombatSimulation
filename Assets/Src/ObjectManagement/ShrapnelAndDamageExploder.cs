﻿using Assets.Src.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Src.ObjectManagement
{
    public class ShrapnelAndDamageExploder : IExploder
    {
        private Rigidbody _exploder;
        private int _shrapnelCOunt;
        private Rigidbody _shrapnel;
        public float ShrapnelSpeed = 100;
        public float ExplosionForce = 30;
        public float ExplosionBaseDamage = 100;
        public IEnumerable<string> EnemyTags;
        public bool SetEnemyTagOnShrapnel = false;
        public bool TagShrapnel = false;
        private readonly Rigidbody _explosionEffect;
        public float ExplosionRadius = 20;

        public ShrapnelAndDamageExploder(Rigidbody explodingRigidbody, Rigidbody shrapnel, Rigidbody explosionEffect, int shrapnelCount = 50)
        {
            _exploder = explodingRigidbody;
            _shrapnel = shrapnel;
            _shrapnelCOunt = shrapnelCount;
            _explosionEffect = explosionEffect;
        }

        public void ExplodeNow()
        {
            //Debug.Log(_exploder + " is exploding");
            //list all existing objects to be exploded.
            var gameObjects = UnityEngine.Object.FindObjectsOfType<Rigidbody>()
                .Where(r => r != _exploder && Vector3.Distance(r.position, _exploder.position) < ExplosionRadius);
            
            //explode everything.
            foreach (var explodedThing in gameObjects)
            {
                explodedThing.AddExplosionForce(ExplosionForce, _exploder.position, 100);
                var distance = Vector3.Distance(explodedThing.position, _exploder.position);
                var damage = distance > 1 ? ExplosionBaseDamage / (distance * distance) : ExplosionBaseDamage;
                explodedThing.transform.SendMessage("ApplyDamage", damage, SendMessageOptions.DontRequireReceiver);
            }

            if (_explosionEffect != null)
            {
                var explosion = UnityEngine.Object.Instantiate(_explosionEffect, _exploder.position, UnityEngine.Random.rotation);
                explosion.velocity = _exploder.velocity;
            }

            //add shrapnel to be exploded
            if (_shrapnelCOunt > 0 && _shrapnel != null)
            {
                for (int i = 0; i < _shrapnelCOunt; i++)
                {
                    var location = UnityEngine.Random.insideUnitSphere;
                    var fragment = UnityEngine.Object.Instantiate(_shrapnel, _exploder.position + location, _exploder.transform.rotation);
                    fragment.velocity = _exploder.velocity + (ShrapnelSpeed * location);
                    //gameObjects.Add(fragment);
                
                    if (SetEnemyTagOnShrapnel && EnemyTags != null && EnemyTags.Any())
                    {
                        fragment.SendMessage("SetEnemyTags", EnemyTags);
                    }

                    if (TagShrapnel)
                    {
                        fragment.tag = _exploder.tag;
                    }
                }
            }

            GameObject.Destroy(_exploder.gameObject);
        }

        public void SetExplodingObject(Rigidbody explodingRigidbody)
        {
            _exploder = explodingRigidbody;
        }
    }
}
