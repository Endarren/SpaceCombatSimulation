﻿using Assets.Src.Interfaces;
using Assets.Src.ObjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.src.Evolution
{
    public class ShipBuilder : IKnowsEnemyTagAndtag
    {
        public string EnemyTag = "Enemy";
        public int MaxModules = 20;
        private int _modulesAdded = 0;

        public string GetEnemyTag()
        {
            return EnemyTag;
        }

        public void SetEnemyTag(string newTag)
        {
            EnemyTag = newTag;
        }

        private string _genome;

        /// <summary>
        /// Module 0 should be the only one with its own spawnPoints
        /// </summary>
        public Rigidbody Module0, Module1, Module2, Module3, Module4,
            Module5, Module6, Module7, Module8, Module9;

        private int _genomePosition = 0;
        private float _r;
        private float _g;
        private float _b;

        public ShipBuilder(string genome, Rigidbody module0, Rigidbody module1, Rigidbody module2, Rigidbody module3, Rigidbody module4,
            Rigidbody module5, Rigidbody module6, Rigidbody module7, Rigidbody module8, Rigidbody module9)
        {
            Module0 = module0;
            Module1 = module1;
            Module2 = module2;
            Module3 = module3;
            Module4 = module4;
            Module5 = module5;
            Module6 = module6;
            Module7 = module7;
            Module8 = module8;
            Module9 = module9;
            _genome = genome;
        }

        // Use this for initialization
        void BuildShip(Transform shipToBuildOn)
        {
            _r = GetNumberFromGenome(0);
            _g = GetNumberFromGenome(2);
            _b = GetNumberFromGenome(4);

            shipToBuildOn.SetColor(_r,_g,_b);
            SpawnModules(shipToBuildOn);
        }

        public void SetGenome(string genome)
        {
            _genome = genome;
        }

        private void SpawnModules(Transform currentHub)
        {
            var _spawnPoints = GetSpawnPoints(currentHub);
            foreach (var spawnPoint in _spawnPoints)
            {
                if (_genomePosition < _genome.Length && _modulesAdded < MaxModules)
                {
                    var letter = _genome.ElementAt(_genomePosition);
                    _genomePosition++;

                    var moduleToAdd = SelectModule(letter);
                    if (moduleToAdd != null)
                    {
                        var addedModule = GameObject.Instantiate(moduleToAdd, spawnPoint.position, spawnPoint.rotation, spawnPoint);
                        _modulesAdded++;
                        addedModule.transform.parent = currentHub;
                        addedModule.GetComponent<FixedJoint>().connectedBody = currentHub.GetComponent<Rigidbody>();
                        addedModule.SendMessage("SetEnemyTag", EnemyTag, SendMessageOptions.DontRequireReceiver);

                        addedModule.tag = currentHub.tag;

                        SpawnModules(addedModule.transform);    //spawn modules on this module

                        addedModule.transform.SetColor(_r,_g,_b);
                    }
                }
            }
        }

        private float GetNumberFromGenome(int fromStart)
        {
            var simplified = _genome.Replace(" ", "");
            if (simplified.Length > fromStart)
            {
                simplified = simplified + "  ";
                var stringNumber = simplified.Substring(fromStart, 2);
                int number;
                if (int.TryParse(stringNumber, out number))
                {
                    return number / 99f;
                }
            }
            return 1;
        }

        private List<Transform> GetSpawnPoints(Transform currentHub)
        {
            var _spawnPoints = new List<Transform>();
            var childCount = currentHub.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = currentHub.GetChild(i);
                if (child.name.Contains("SP"))
                {
                    _spawnPoints.Add(child);
                }
            }
            return _spawnPoints;
        }

        private Rigidbody SelectModule(char letter)
        {
            switch (letter)
            {
                case '0':
                    return Module0;
                case '1':
                    return Module1;
                case '2':
                    return Module2;
                case '3':
                    return Module3;
                case '4':
                    return Module4;
                case '5':
                    return Module5;
                case '6':
                    return Module6;
                case '7':
                    return Module7;
                case '8':
                    return Module8;
                case '9':
                    return Module9;
                default:
                    return null;
            }
        }

    }
}

