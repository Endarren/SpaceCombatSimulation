﻿using Assets.Src.Interfaces;
using Assets.Src.ObjectManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Src.Controllers
{
    class WorldControler : MonoBehaviour, IKnowsEnemyTagAndtag
    {
        public string TarGetTag = "Enemy";
        public bool TagChildren = false;
        public bool ShouldSpawnDrones = true;
        public bool ShouldSetEnemyTag = false;
        private Camera _currentCamera;

        public string GetEnemyTag()
        {
            return TarGetTag;
        }

        public void SetEnemyTag(string newTag)
        {
            TarGetTag = newTag;
        }

        public Rigidbody Drone;
        public float Radius = 100;

        private int _reload = 0;
        public int LoadTime = 200;
        public float SpeedScaler = 0.1f;
        private IDestroyer _destroyer;
        public Rigidbody DeathExplosion;

        private int _activeCameraIndex = 0;
        public float ZoomMultiplier = 30;
        public float ZoomOrSwitchThreshold = 500;
        private bool _touchedInPreviousFrame;
        private Camera _activeCamera;


        // Use this for initialization
        void Start()
        {
            _destroyer = new WithChildrenDestroyer()
            {
                ExplosionEffect =
                DeathExplosion
            };

            DetectActiveCamera();
        }

        private void DetectActiveCamera()
        {
            var cameras = GameObject.FindGameObjectsWithTag("MainCamera")
                .Where(c => c.GetComponent("Camera") != null)
                .Select(c => c.GetComponent<Camera>()).ToList();

            for (int i = 0; i < cameras.Count(); i++)
            {
                var cam = cameras[i];
                if(_activeCamera != null)
                {
                    //if we already know the active camera, deactivate all others
                    cam.enabled = false;
                }
                else if (cam.enabled)
                {
                    _activeCamera = cam;
                    _activeCameraIndex = i;
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            ZoomCamera();

            SpawnDrones();

            HandleCameraCycling();

            MoveToAverageLoc();

        }

        private void MoveToAverageLoc()
        {
            var objects = GameObject.FindGameObjectsWithTag("SpaceShip");

            var averageXLocation = objects.Average(t => t.transform.position.x);
            var averageYLocation = objects.Average(t => t.transform.position.y);
            var averageZLocation = objects.Average(t => t.transform.position.z);
            transform.position = new Vector3(averageXLocation, averageYLocation, averageZLocation);
        }

        private void HandleCameraCycling()
        {
            if (Input.GetKeyUp(KeyCode.C))
            {
                CycleCameras();
            }
            if (Input.GetKeyUp(KeyCode.X))
            {
                CycleCameras(false);
            }
            
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Stationary)
                {
                    // Get movement of the finger since last frame
                    if (!_touchedInPreviousFrame && touch.position.x > ZoomOrSwitchThreshold)
                    {
                        CycleCameras();
                    }

                    _touchedInPreviousFrame = true;
                    return;
                }
            } 
            _touchedInPreviousFrame = false;
        }

        private void ZoomCamera()
        {
            if (_activeCamera != null)
            {
                var scroll = Input.GetAxis("Mouse ScrollWheel");

                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    var touch = Input.GetTouch(0);
                    // Get movement of the finger since last frame
                    Vector2 touchDeltaPosition = touch.deltaPosition;

                    if(touch.position.x < ZoomOrSwitchThreshold)
                    {
                        scroll += touchDeltaPosition.y/4;
                    }
                }
                
                _activeCamera.transform.position += scroll * ZoomMultiplier * _activeCamera.transform.forward;
            }
        }

        private void CycleCameras(bool forwards = true)
        {
            var cameras = GameObject.FindGameObjectsWithTag("MainCamera")
                .Where(c => c.GetComponent("Camera") != null)
                .Select(c => c.GetComponent<Camera>()).ToList();

            //var cameras = Camera.allCameras;

            //var currentCamera = Camera.current;
            //if(currentCamera != null)
            //{
            //    currentCamera.enabled = false;
            //}

            foreach (var cam in cameras)
            {
                cam.enabled = false;
            }

            if (forwards)
            {
                _activeCameraIndex++;
            }
            else
            {
                _activeCameraIndex--;
                _activeCameraIndex = _activeCameraIndex < 0 ? cameras.Count - 1 : _activeCameraIndex;
            }

            _activeCameraIndex = _activeCameraIndex % cameras.Count();

            _activeCamera = cameras[_activeCameraIndex];
            if (_activeCamera == null)
            {
                print("Could not find cam " + _activeCameraIndex + "activating 0 instead");
                _activeCameraIndex = 0;
                _activeCamera = cameras[_activeCameraIndex];
            }
            if (_activeCamera != null)
            {
                _activeCamera.enabled = true;
            }
        }

        private void SpawnDrones()
        {
            if (ShouldSpawnDrones)
            {
                if (_reload <= 0)
                {
                    var bearing = UnityEngine.Random.rotation;
                    var location = (bearing * new Vector3(0, 0, UnityEngine.Random.value * Radius)) + transform.position;
                    var drone = Instantiate(Drone, location, transform.rotation);

                    var velocity = SpeedScaler * UnityEngine.Random.insideUnitSphere;
                    drone.velocity = velocity;

                    if (ShouldSetEnemyTag) { drone.SendMessage("SetEnemyTag", TarGetTag); }
                    if (TagChildren) { drone.tag = tag; }

                    _reload = LoadTime;
                }
                else
                {
                    _reload--;
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            _destroyer.Destroy(other.gameObject, false);
        }
    }
}