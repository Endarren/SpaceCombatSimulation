﻿using Assets.Src.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Src.Rocket
{
    class RocketRunner : IRocketRunner
    {
        public int DetonateWithLessThanXRemainingFuel = -100;
        private ITargetDetector _targetDetector;
        private ITargetPicker _targetPicker;
        private IRocketEngineControl _engineControl;
        private readonly RocketController _rocketController;
        private string _previousTarget;
        private IDetonator _detonator;

        public RocketRunner(ITargetDetector targetDetector, ITargetPicker targetPicker, IRocketEngineControl engineControl, IDetonator detonator)
        {
            _targetDetector = targetDetector;
            _targetPicker = targetPicker;
            _engineControl = engineControl;
            _detonator = detonator;
        }

        public void RunRocket()
        {
            var allTargets = _targetDetector.DetectTargets();
            var bestTarget = _targetPicker.FilterTargets(allTargets).OrderByDescending(t => t.Score).FirstOrDefault();

            if (bestTarget != null)
            {
                _engineControl.FlyAtTargetMaxSpeed(bestTarget);
            }

            if(_engineControl.RemainingFuel < DetonateWithLessThanXRemainingFuel)
            {
                _detonator.DetonateNow();
            } else if(_engineControl.StartDelay <= 0)
            {
                _detonator.AutoDetonate(bestTarget);
            }
        }
    }
}
