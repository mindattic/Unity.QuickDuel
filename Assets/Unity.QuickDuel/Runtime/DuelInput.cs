using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.QuickDuel
{
    // Two players on one machine: A / gamepad 1 south strike for the left duelist, L / gamepad 2 south
    // for the right. A mouse click or touchscreen press is also a left-side strike, matching the
    // original single-player duel's click-to-strike (a holdover from its Android touch build). Space
    // or Start begins the next round. Each strike carries its input event's own timestamp
    // (CallbackContext.time, on the Time.realtimeSinceStartup timeline), so the frame that happens to
    // process it doesn't change the reaction time.
    [RequireComponent(typeof(DuelController))]
    public sealed class DuelInput : MonoBehaviour
    {
        private DuelController controller;
        private InputAction leftKey;
        private InputAction rightKey;
        private InputAction padStrike;
        private InputAction pointerStrike;
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

            // <Pointer>/press covers Mouse's left button and Touchscreen's contact alike.
            pointerStrike = new InputAction("PointerStrike", InputActionType.Button, "<Pointer>/press");

            nextRound = new InputAction("NextRound", InputActionType.Button);
            nextRound.AddBinding("<Keyboard>/space");
            nextRound.AddBinding("<Gamepad>/start");

            // Keys typed into the online menu's address field, and clicks on its buttons, are the menu's (UQD-T21).
            leftKey.performed += ctx => { if (!UiFocus.IsTyping) controller.Strike(Side.Left, ctx.time); };
            rightKey.performed += ctx => { if (RightIsHuman && !UiFocus.IsTyping) controller.Strike(Side.Right, ctx.time); };
            padStrike.performed += OnPadStrike;
            pointerStrike.performed += ctx => { if (!UiFocus.PointerOverUi) controller.Strike(Side.Left, ctx.time); };
            // A Start press is "next round", unless Select is held with it — that chord is DuelQuit's.
            nextRound.performed += ctx =>
            {
                if (ctx.control.device is Gamepad pad && pad.selectButton.isPressed)
                    return;
                if (ctx.control.device is Keyboard && UiFocus.IsTyping)
                    return;
                controller.BeginRound();
            };
        }

        private void OnEnable()
        {
            leftKey.Enable();
            rightKey.Enable();
            padStrike.Enable();
            pointerStrike.Enable();
            nextRound.Enable();
        }

        private void OnDisable()
        {
            leftKey.Disable();
            rightKey.Disable();
            padStrike.Disable();
            pointerStrike.Disable();
            nextRound.Disable();
        }

        private void OnDestroy()
        {
            leftKey.Dispose();
            rightKey.Dispose();
            padStrike.Dispose();
            pointerStrike.Dispose();
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
