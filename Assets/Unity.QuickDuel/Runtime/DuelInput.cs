using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.QuickDuel
{
    // Two players on one machine: A / gamepad 1 south strike for the left duelist, L / gamepad 2 south
    // for the right. A mouse click or touchscreen press is also a left-side strike, matching the
    // original single-player duel's click-to-strike (a holdover from its Android touch build). During the
    // startup the click instead begins the round at once, as obj_game's global left press did (DuelStartup,
    // UQD-T66), and so does a pad's South, the pad's stand-in for it. Online, the host's Space or Start
    // begins the next round. Each strike carries its input event's own timestamp (CallbackContext.time, on
    // the Time.realtimeSinceStartup timeline), so the frame that happens to process it doesn't change the
    // reaction time.
    [RequireComponent(typeof(DuelController))]
    public sealed class DuelInput : MonoBehaviour
    {
        private DuelController controller;
        private DuelStartup startup;
        private DuelFade fade;
        private DuelVerdict verdict;
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
            startup = GetComponent<DuelStartup>();

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
            pointerStrike.performed += ctx =>
            {
                if (UiFocus.PointerOverUi || Skipped()) return;
                controller.Strike(Side.Left, ctx.time);
            };
            // Online only: the host's Start or Space begins the next round, unless Select is held with Start — that
            // chord is DuelQuit's. A local round begins by itself (DuelStartup).
            nextRound.performed += ctx =>
            {
                if (!controller.IsOnline)
                    return;
                if (ctx.control.device is Gamepad pad && pad.selectButton.isPressed)
                    return;
                if (ctx.control.device is Keyboard && UiFocus.IsTyping)
                    return;
                // The aftermath plays out and the loading fade resets the battle before the next round (UQD-T42).
                if (fade == null) fade = GetComponent<DuelFade>();
                if (fade != null && fade.Pending)
                    return;
                // The verdict screen's button, not Space or Start, leaves a finished match (UQD-T48).
                if (verdict == null) verdict = GetComponent<DuelVerdict>();
                if (verdict != null && verdict.Shown)
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

        // The startup's skip (obj_game's global left press in "startup"): true when it began the round.
        private bool Skipped() => startup != null && startup.TrySkip();

        private void OnPadStrike(InputAction.CallbackContext ctx)
        {
            if (!ctx.ReadValueAsButton())
                return;
            if (SideOfGamepad(ctx.control.device).HasValue && Skipped())
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
