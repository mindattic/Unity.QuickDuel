using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.QuickDuel
{
    // The original's obj_mainmenu bound Escape (and its Quit button) to game_end(); the Windows build
    // had nothing standing in for either. Escape, or gamepad Select held while Start is pressed, now
    // calls Application.Quit, gated the same way DuelSession gates Tab/Select: only between rounds
    // (Duel == null or Resolved), so a round in flight can't be cut short.
    [RequireComponent(typeof(DuelController))]
    public sealed class DuelQuit : MonoBehaviour
    {
        private DuelController controller;
        private InputAction escape;
        private InputAction padStart;

        // Replaced by tests so quitting doesn't tear down the test process.
        public Action OnQuit { get; set; } = Application.Quit;

        private void Awake()
        {
            controller = GetComponent<DuelController>();

            escape = new InputAction("QuitEscape", InputActionType.Button, "<Keyboard>/escape");
            // Start alone is DuelInput's "next round"; read Select's live state directly (rather than a
            // modifier composite) so holding it with Start quits instead, on either connected pad.
            padStart = new InputAction("QuitPadStart", InputActionType.Button, "<Gamepad>/start");

            escape.performed += _ => TryQuit();
            padStart.performed += ctx =>
            {
                if (ctx.control.device is Gamepad pad && pad.selectButton.isPressed)
                    TryQuit();
            };
        }

        private void OnEnable()
        {
            escape.Enable();
            padStart.Enable();
        }

        private void OnDisable()
        {
            escape.Disable();
            padStart.Disable();
        }

        private void OnDestroy()
        {
            escape.Dispose();
            padStart.Dispose();
        }

        // Between rounds only; false (and the player stays up) while a duel is in progress.
        public bool TryQuit()
        {
            if (!controller.CanBeginRound)
                return false;
            OnQuit();
            return true;
        }
    }
}
