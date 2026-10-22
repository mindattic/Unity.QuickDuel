using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.QuickDuel
{
    // Two players on one machine: A / gamepad 1 south strike for the left duelist, L / gamepad 2 south
    // for the right. Space or Start begins the next round. Each strike carries its input event's own
    // timestamp (CallbackContext.time, on the Time.realtimeSinceStartup timeline), so the frame that
    // happens to process it doesn't change the reaction time.
    [RequireComponent(typeof(DuelController))]
    public sealed class DuelInput : MonoBehaviour
    {
        private DuelController controller;
        private InputAction leftKey;
        private InputAction rightKey;
        private InputAction padStrike;
        private InputAction nextRound;

        // False while the computer plays the right side; L and gamepad 2 then do nothing.
        public bool RightIsHuman { get; set; } = true;

        private void Awake()
        {
            controller = GetComponent<DuelController>();

            leftKey = new InputAction("LeftStrike", InputActionType.Button, "<Keyboard>/a");
            rightKey = new InputAction("RightStrike", InputActionType.Button, "<Keyboard>/l");

            // Pass-through so two pads pressing together each report, rather than one action
            // disambiguating them away.
            padStrike = new InputAction("PadStrike", InputActionType.PassThrough, "<Gamepad>/buttonSouth");

            nextRound = new InputAction("NextRound", InputActionType.Button);
            nextRound.AddBinding("<Keyboard>/space");
            nextRound.AddBinding("<Gamepad>/start");

            leftKey.performed += ctx => controller.Strike(Side.Left, ctx.time);
            rightKey.performed += ctx => { if (RightIsHuman) controller.Strike(Side.Right, ctx.time); };
            padStrike.performed += OnPadStrike;
            nextRound.performed += _ => controller.BeginRound();
        }

        private void OnEnable()
        {
            leftKey.Enable();
            rightKey.Enable();
            padStrike.Enable();
            nextRound.Enable();
        }

        private void OnDisable()
        {
            leftKey.Disable();
            rightKey.Disable();
            padStrike.Disable();
            nextRound.Disable();
        }

        private void OnDestroy()
        {
            leftKey.Dispose();
            rightKey.Dispose();
            padStrike.Dispose();
            nextRound.Dispose();
        }

        private void OnPadStrike(InputAction.CallbackContext ctx)
        {
            if (!ctx.ReadValueAsButton())
                return;
            var side = SideOfGamepad(ctx.control.device);
            if (side.HasValue && (side.Value == Side.Left || RightIsHuman))
                controller.Strike(side.Value, ctx.time);
        }

        // Gamepad 1 (the first connected) is the left duelist, gamepad 2 the right; any others sit out.
        public static Side? SideOfGamepad(InputDevice device)
        {
            var pads = Gamepad.all;
            if (pads.Count > 0 && pads[0] == device) return Side.Left;
            if (pads.Count > 1 && pads[1] == device) return Side.Right;
            return null;
        }
    }
}
