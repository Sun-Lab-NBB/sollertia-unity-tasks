using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SharpDX.DirectInput;
using UnityEngine.InputSystem;

namespace Gimbl
{
    public class SimulatedLinearTreadmill : LinearTreadmill
    {
        public JoystickState state = new JoystickState();
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private float moveControl;
        private float passedTime;
        private bool[] buttonPresses = new bool[4];

        // Input System
        private SimulatedInput _input;

        public void Start()
        {
            // Initialize Input System for keyboard/mouse simulation.
            _input = new SimulatedInput();
            _input.Enable();

            // Acquire gamepad if selected.
            if (settings.gamepadSettings.selectedGamepad > 0) gamepad.Acquire(settings.gamepadSettings.selectedGamepad - 1);
            // Setup MQTT Channels for button presses.
            gamepad.SetupChannels(settings.buttonTopics);
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
            // Keyboard mouse.
            if (settings.gamepadSettings.selectedGamepad == 0)
            {
                moveControl = _input.Player.Movement.ReadValue<Vector2>().y * passedTime * 0.008f;
                // Check button presses.
                buttonPresses[0] = _input.Player.Fire1.WasPressedThisFrame();
                buttonPresses[1] = _input.Player.Fire2.WasPressedThisFrame();
                buttonPresses[2] = _input.Player.Fire3.WasPressedThisFrame();
                buttonPresses[3] = _input.Player.Jump.WasPressedThisFrame();
                if (this.Actor !=null) { gamepad.SendChannels(buttonPresses);  }
            }
            //Gamepad.
            else
            {
                gamepad.joystick.GetCurrentState(ref state);
                moveControl = -gamepad.normRange(state.Y) * passedTime * 0.15f;
                if (Mathf.Abs(moveControl) < 0.075) moveControl = 0;
                moveControl *= 0.05f;
                // Check button that have changed to On.
                if (this.Actor != null) { gamepad.SendChannels(gamepad.checkButtonChange(state)); }
            }
            stopwatch.Restart();
            movement.Add(moveControl, 0, 0);
        }

    }
}
