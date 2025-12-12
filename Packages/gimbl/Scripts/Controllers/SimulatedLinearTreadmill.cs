using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gimbl
{
    public class SimulatedLinearTreadmill : LinearTreadmill
    {
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private float moveControl;
        private float passedTime;

        // Input System
        private SimulatedInput _input;

        public void Start()
        {
            // Initialize Input System for keyboard/mouse simulation.
            _input = new SimulatedInput();
            _input.Enable();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Disable();
                _input.Dispose();
            }
        }

        public new void Update()
        {
            GetSimulatedInput();
            ProcessMovement();
        }

        public void GetSimulatedInput()
        {
            passedTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            moveControl = _input.Player.Movement.ReadValue<Vector2>().y * passedTime * 0.008f;
            stopwatch.Restart();
            movement.Add(moveControl);
        }
    }
}
