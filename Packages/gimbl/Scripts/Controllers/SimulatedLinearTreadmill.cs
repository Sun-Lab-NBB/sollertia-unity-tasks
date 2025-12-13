/// <summary>
/// Provides the SimulatedLinearTreadmill class for keyboard-based treadmill simulation.
///
/// Extends the LinearTreadmill controller to accept keyboard input instead of MQTT
/// messages, enabling testing without physical hardware.
/// </summary>
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gimbl
{
    /// <summary>
    /// Simulates linear treadmill input using keyboard controls.
    /// </summary>
    public class SimulatedLinearTreadmill : LinearTreadmill
    {
        /// <summary>The stopwatch for measuring time between frames.</summary>
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        /// <summary>The current movement control value from input.</summary>
        private float moveControl;

        /// <summary>The elapsed time since last frame in milliseconds.</summary>
        private float passedTime;

        /// <summary>The Unity Input System action map for keyboard/mouse simulation.</summary>
        private SimulatedInput _input;

        /// <summary>Initializes the Input System for keyboard/mouse simulation on start.</summary>
        public void Start()
        {
            _input = new SimulatedInput();
            _input.Enable();
        }

        /// <summary>Cleans up the Input System resources when destroyed.</summary>
        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Disable();
                _input.Dispose();
            }
        }

        /// <summary>Processes simulated input and movement each frame.</summary>
        public new void Update()
        {
            GetSimulatedInput();
            ProcessMovement();
        }

        /// <summary>Reads keyboard input and converts it to treadmill movement values.</summary>
        public void GetSimulatedInput()
        {
            passedTime = (float)stopwatch.Elapsed.TotalMilliseconds;
            moveControl = _input.Player.Movement.ReadValue<Vector2>().y * passedTime * 0.008f;
            stopwatch.Restart();
            movement.Add(moveControl);
        }
    }
}
