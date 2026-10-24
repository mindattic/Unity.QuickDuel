using System;
using System.Collections.Generic;
using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.QuickDuel
{
    // Draws the duel from the controller's phase and result; it never decides anything. The whole UI is
    // built in code (placeholder fighters over the original arena art) on UI Toolkit (UQD-T31): a UIDocument reads
    // Resources/UI/DuelPanelSettings.asset (scale-with-screen-size, 1920x1080 reference, built by
    // DuelPanelBuilder) and Resources/UI/Duel.uss, and every label shares the font asset
    // Resources/UI/Fonts/Cinzel.ttf -- no more Resources.GetBuiltinResource("LegacyRuntime.ttf"). The panel
    // renders as a screen-space overlay (UI Toolkit has no "Screen Space - Camera" mode); the main camera
    // behind it only clears to its background colour, the way it always has. Centre-anchored content (the
    // fighters, every CreateLabel) lives on a fixed 1920x1080 "stage" kept centred in Root (see Build()),
    // so it stays centred at any aspect; screen-edge content (the arena backdrop, letterbox, the edge score
    // labels) is positioned by percentage straight off Root, which does track the real aspect.
    //
    // The backdrop is one of the original's 11 arenas (UQD-T33), the one the round's arena roll lands on
    // (ArenaDraw, drawn by the authority each round), scaled to cover the screen. On the cue the whole backdrop's
    // colours invert, as obj_arena did by drawing white over the view with an inverting blend
    // (draw_set_blend_mode_ext(bm_inv_dest_colour, bm_zero)): a real per-pixel invert, here a UI Toolkit invert
    // filter on the backdrop, not a recolouring of the boxes drawn over it.
    [RequireComponent(typeof(DuelController))]
    public sealed class DuelView : MonoBehaviour
    {
        private const float StageWidth = 1920f;
        private const float StageHeight = 1080f;
        private const float LetterboxHeight = 150f;
        private const float GroundY = -230f;

        // Shown behind the arena art, and in its place in a scene that carries no arenas.
        private static readonly Color Paper = new Color(0.93f, 0.88f, 0.78f);
        private static readonly Color Ink = new Color(0.10f, 0.08f, 0.08f);
        private static readonly Color LeftColor = new Color(0.62f, 0.12f, 0.10f);
        private static readonly Color RightColor = new Color(0.14f, 0.20f, 0.45f);
        private static readonly Color Steel = new Color(0.80f, 0.82f, 0.86f);

        // The original arenas, in load_arenas.gml's order; written by DuelSceneBuilder from the asset manifest.
        [SerializeField] private List<ArenaBackdrop> arenas = new List<ArenaBackdrop>();

        private DuelController controller;
        private UIDocument document;
        private Font font;
        private VisualElement stage;
        private VisualElement backdrop;
        private int? shownRoll;
        private VisualElement letterboxTop;
        private VisualElement letterboxBottom;
        private Fighter left;
        private Fighter right;
        private Label title;
        private Label countdown;
        private Label cue;
        private Label headline;
        private Label prompt;
        private Label leftReaction;
        private Label rightReaction;
        private float letterbox;
        private bool inverted;

        public bool Inverted => inverted;

        public IReadOnlyList<ArenaBackdrop> Arenas => arenas;

        // The arena on screen; null in a scene that carries none.
        public ArenaBackdrop Arena { get; private set; }

        // The backdrop element, whose filter carries the cue's invert.
        public VisualElement Backdrop => backdrop;

        // The scene builder's way in: the arenas this view draws from.
        public void SetArenas(IEnumerable<ArenaBackdrop> backdrops)
        {
            arenas = new List<ArenaBackdrop>(backdrops ?? throw new ArgumentNullException(nameof(backdrops)));
            shownRoll = null;
        }
        public float LetterboxAmount => letterbox;
        public string CountdownText => IsVisible(countdown) ? countdown.text : "";
        public string CueText => IsVisible(cue) ? cue.text : "";
        public string HeadlineText => IsVisible(headline) ? headline.text : "";
        public string PromptText => IsVisible(prompt) ? prompt.text : "";
        public string ReactionText(Side side) => (side == Side.Left ? leftReaction : rightReaction).text;
        public bool HasFallen(Side side) => (side == Side.Left ? left : right).Fallen;

        // Other components (DuelSession, DuelTaunt) hang their own labels off this same tree.
        public VisualElement Root { get; private set; }

        private void Awake()
        {
            controller = GetComponent<DuelController>();
            font = Resources.Load<Font>("UI/Fonts/Cinzel");
            Build();
            controller.RoundStarted += OnRoundStarted;
            controller.Resolved += OnResolved;
            Refresh(0f);
        }

        private void Update() => Refresh(Time.unscaledDeltaTime);

        private void OnRoundStarted()
        {
            left.Reset();
            right.Reset();
            leftReaction.text = "";
            rightReaction.text = "";
        }

        private void OnResolved(DuelResult result)
        {
            leftReaction.text = FormatReaction(result.LeftReaction);
            rightReaction.text = FormatReaction(result.RightReaction);
            if (result.Winner.HasValue)
            {
                var winner = result.Winner.Value;
                (winner == Side.Left ? right : left).Fall();
                (winner == Side.Left ? left : right).Raise();
            }
        }

        public static string FormatReaction(double? seconds)
        {
            if (!seconds.HasValue) return "no strike";
            if (seconds.Value < 0) return "too early";
            return $"{Mathf.RoundToInt((float)(seconds.Value * 1000))} ms";
        }

        // Who the right fighter is called in the headline; the session sets "AI" in vs-AI modes.
        public string RightName { get; set; } = "Right";

        public static string Headline(DuelResult result, string rightName = "Right")
        {
            switch (result.Outcome)
            {
                case DuelOutcome.Tie: return "Draw";
                case DuelOutcome.NoStrike: return "Nobody struck";
                default: return result.Winner == Side.Left ? "Left wins" : rightName + " wins";
            }
        }

        private void Refresh(float dt)
        {
            var phase = controller.Phase;
            ShowArena(controller.ArenaRoll);

            var countingDown = phase == DuelPhase.Countdown;
            SetVisible(countdown, countingDown);
            if (countingDown)
            {
                var number = controller.CountdownNumber;
                countdown.text = number > 0 ? number.ToString() : "";
            }

            var tense = phase == DuelPhase.Standoff || phase == DuelPhase.Strike;
            letterbox = Mathf.MoveTowards(letterbox, tense ? 1f : 0f, dt * 3f);
            if (dt == 0f) letterbox = tense ? 1f : 0f;
            letterboxTop.style.height = LetterboxHeight * letterbox;
            letterboxBottom.style.height = LetterboxHeight * letterbox;

            SetInverted(phase == DuelPhase.Strike);
            SetVisible(cue, phase == DuelPhase.Strike);

            var resolved = phase == DuelPhase.Resolved;
            SetVisible(headline, resolved);
            if (resolved) headline.text = controller.Result != null ? Headline(controller.Result, RightName) : AwaitingVerdictText;
            SetVisible(title, phase == DuelPhase.Ready);
            SetVisible(prompt, phase == DuelPhase.Ready || resolved);
            prompt.text = controller.IsOnline ? OnlinePrompt(phase) : phase == DuelPhase.Ready
                ? "Left: A / gamepad 1     Right: L / gamepad 2\nPress Space or Start to duel\nEsc / Select+Start: quit"
                : "Press Space or Start for the next round\nEsc / Select+Start: quit";

            left.Animate(dt);
            right.Animate(dt);
        }

        // The headline while this side's round is over but the host's verdict is still on its way (online only).
        public const string AwaitingVerdictText = "...";

        // Online, one seat is this machine's and only the host starts rounds, so the prompt says whose move it is.
        private string OnlinePrompt(DuelPhase phase)
        {
            var side = controller.LocalSide;
            var seat = side.HasValue ? $"You are {(side == Side.Left ? "Left" : "Right")}" : "Watching";
            const string strike = "Strike: A / L / gamepad / click";
            string next;
            if (controller.AwaitingVerdict)
                next = "Waiting for the verdict";
            else if (controller.CanBeginOnlineRound)
                next = phase == DuelPhase.Ready ? "Press Space or Start to duel" : "Press Space or Start for the next round";
            else if (controller.Online.IsHost && !controller.Online.Seated)
                next = "Waiting for a player to join";
            else
                next = "Waiting for the host to start the round";
            return $"{seat}     {strike}\n{next}\nEsc / Select+Start: quit";
        }

        private static bool IsVisible(VisualElement element) => element.style.display != DisplayStyle.None;

        private static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // The arena the roll lands on (ArenaDraw.Pick), redrawn only when the roll changes.
        private void ShowArena(int roll)
        {
            if (shownRoll == roll) return;
            shownRoll = roll;
            Arena = arenas.Count > 0 ? arenas[ArenaDraw.Pick(roll, arenas.Count)] : null;
            backdrop.style.backgroundImage = Arena?.Sprite != null
                ? new StyleBackground(Arena.Sprite)
                : new StyleBackground(StyleKeyword.None);
        }

        // A real colour invert of the whole backdrop (its art and the colour behind it), as obj_arena's
        // inverting blend over the view: every pixel becomes 1 - itself.
        private void SetInverted(bool value)
        {
            if (inverted == value) return;
            inverted = value;
            if (value)
            {
                var invert = new FilterFunction(FilterFunctionType.Invert);
                invert.AddParameter(new FilterParameter(1f));
                backdrop.style.filter = new List<FilterFunction> { invert };
            }
            else
            {
                backdrop.style.filter = StyleKeyword.Null;
            }
        }

        private void Build()
        {
            var documentObject = new GameObject("DuelDocument", typeof(UIDocument));
            documentObject.transform.SetParent(transform, false);
            document = documentObject.GetComponent<UIDocument>();
            document.panelSettings = Resources.Load<PanelSettings>("UI/DuelPanelSettings");

            Root = document.rootVisualElement;
            Root.AddToClassList("duel-root");
            if (font != null) Root.style.unityFontDefinition = FontDefinition.FromFont(font);
            var styleSheet = Resources.Load<StyleSheet>("UI/Duel");
            if (styleSheet != null) Root.styleSheets.Add(styleSheet);

            // Root's own resolved size is not fixed at the reference resolution (ScaleWithScreenSize keeps
            // it at the real device aspect, only the final render scale is reference-resolution-based), so
            // a fixed-size, always-centred "stage" is what gives centre-anchored content (the fighters,
            // every CreateLabel) a true 1920x1080, any-aspect-safe coordinate space -- the same way the
            // original's canvas-centre anchors kept them centred regardless of the real canvas rect size.
            // Things that must reach the real screen edges at any aspect (the backdrop, letterbox, the edge
            // score labels) stay on Root itself instead, sized/positioned by percentage, not by the stage.
            // The backdrop goes first so everything else draws over it.
            backdrop = Box("Backdrop", Root, Paper);
            backdrop.AddToClassList("backdrop");
            backdrop.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            backdrop.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            backdrop.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);

            stage = new VisualElement { name = "Stage", pickingMode = PickingMode.Ignore };
            stage.AddToClassList("stage");
            Root.Add(stage);

            left = new Fighter(this, "Left", -380f, LeftColor, 1f);
            right = new Fighter(this, "Right", 380f, RightColor, -1f);

            leftReaction = CreateLabel("LeftReaction", 56, new Vector2(-380f, GroundY - 80f), Ink);
            rightReaction = CreateLabel("RightReaction", 56, new Vector2(380f, GroundY - 80f), Ink);

            letterboxTop = Box("LetterboxTop", Root, Color.black);
            letterboxTop.AddToClassList("letterbox");
            letterboxTop.style.top = 0;
            letterboxTop.style.height = 0;
            letterboxBottom = Box("LetterboxBottom", Root, Color.black);
            letterboxBottom.AddToClassList("letterbox");
            letterboxBottom.style.bottom = 0;
            letterboxBottom.style.height = 0;

            title = CreateLabel("Title", 150, new Vector2(0, 260f), Ink);
            title.text = "QUICKDUEL";
            countdown = CreateLabel("Countdown", 320, new Vector2(0, 120f), Ink);
            cue = CreateLabel("Cue", 420, new Vector2(0, 120f), new Color(0.85f, 0.10f, 0.10f));
            cue.text = "!";
            headline = CreateLabel("Headline", 120, new Vector2(0, 370f), Ink);
            prompt = CreateLabel("Prompt", 44, new Vector2(0, -440f), Ink);
        }

        // A box filled with its own colour (an untextured placeholder shape); position and size are set by
        // the caller, same as the original RectTransform-based helper.
        internal VisualElement Box(string name, VisualElement parent, Color color)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.backgroundColor = color;
            parent.Add(element);
            return element;
        }

        // A label centred on `position` (the original's canvas-centre-relative anchored position, y up);
        // 1600 wide (as the original's fixed, overflow-allowed sizeDelta was) so long text never clips.
        public Label CreateLabel(string name, int size, Vector2 position, Color color)
        {
            var label = new Label { name = name, text = "", pickingMode = PickingMode.Ignore };
            label.AddToClassList("label");
            label.style.fontSize = size;
            label.style.color = color;
            PlaceOnStage(label, position.x, position.y, 1600f, size * 1.2f);
            stage.Add(label);
            return label;
        }

        // Pinned to the left (edge 0) or right (edge 1) screen edge so it stays visible at any aspect,
        // the way the original anchored EdgeLabel to that edge (of the real canvas rect, not the fixed
        // stage) instead of the canvas centre; `top: 50%` finds Root's real vertical centre at any aspect.
        public Label CreateEdgeLabel(string name, int size, float edge, float y, Color color)
        {
            var label = new Label { name = name, text = "", pickingMode = PickingMode.Ignore };
            label.AddToClassList("label");
            label.AddToClassList(edge < 0.5f ? "label--edge-left" : "label--edge-right");
            label.style.fontSize = size;
            label.style.color = color;
            var height = size * 1.2f;
            label.style.position = Position.Absolute;
            label.style.width = 600f;
            label.style.height = height;
            label.style.top = Length.Percent(50);
            label.style.marginTop = -(y + height / 2f);
            if (edge < 0.5f) label.style.left = 40f; else label.style.right = 40f;
            Root.Add(label);
            return label;
        }

        // Moves an already-placed (CreateLabel/CreateEdgeLabel) stage element to a new centre -- the same
        // stage-centred, y-up coordinates PlaceOnStage took -- keeping the width/height it already has.
        // DuelTaunt uses this to move its label to whichever side was tauntable, like the original moving
        // the Text's own RectTransform.anchoredPosition.
        public void Reposition(VisualElement element, float x, float y)
        {
            var width = element.resolvedStyle.width > 0f ? element.resolvedStyle.width : element.style.width.value.value;
            var height = element.resolvedStyle.height > 0f ? element.resolvedStyle.height : element.style.height.value.value;
            PlaceOnStage(element, x, y, width, height);
        }

        // Places an element on the 1920x1080 stage, `position` measured from the stage's centre with y up
        // (as the original's canvas-centred anchoredPosition was), pivoted at its own centre.
        private static void PlaceOnStage(VisualElement element, float x, float y, float width, float height)
        {
            element.style.position = Position.Absolute;
            element.style.width = width;
            element.style.height = height;
            element.style.left = StageWidth / 2f + x - width / 2f;
            element.style.top = StageHeight / 2f - y - height / 2f;
        }

        // Places an element relative to its own parent's origin (not the stage centre), y up, pivoted at
        // (pivotX, pivotY) -- 0 is that edge of the element, 1 the opposite edge; (0.5, 0.5) is centred.
        // Mirrors RectTransform's anchoredPosition + pivot for a parent anchored at its own (0, 0).
        private static void PlacePivoted(VisualElement element, float x, float y, float width, float height, float pivotX, float pivotY)
        {
            element.style.position = Position.Absolute;
            element.style.width = width;
            element.style.height = height;
            element.style.left = x - pivotX * width;
            element.style.top = -y - (1f - pivotY) * height;
        }

        // A placeholder samurai: a body, a head and a drawn blade, pivoted at the feet so it can fall.
        private sealed class Fighter
        {
            private readonly VisualElement root;
            private readonly VisualElement blade;
            private readonly float facing;
            private float targetAngle;
            private float bladeTarget;
            private float angle;
            private float bladeAngle;

            public Fighter(DuelView view, string name, float x, Color color, float facing)
            {
                this.facing = facing;
                root = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
                PlaceOnStage(root, x, GroundY, 0f, 0f);
                view.stage.Add(root);

                var body = view.Box("Body", root, color);
                PlacePivoted(body, 0f, 0f, 130f, 250f, 0.5f, 0f);

                var head = view.Box("Head", root, color);
                PlacePivoted(head, 0f, 310f, 96f, 96f, 0.5f, 0.5f);

                var topknot = view.Box("Topknot", root, color);
                PlacePivoted(topknot, -30f * facing, 370f, 30f, 40f, 0.5f, 0.5f);

                blade = view.Box("Blade", root, Steel);
                PlacePivoted(blade, 60f * facing, 150f, 14f, 200f, 0.5f, 0f);
                blade.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));

                Reset();
            }

            public bool Fallen => Mathf.Abs(targetAngle) > 1f;

            public void Reset()
            {
                targetAngle = 0;
                bladeTarget = 150f * facing;    // sheathed: pointing back and down
                angle = 0;
                bladeAngle = bladeTarget;
                Apply();
            }

            public void Fall() => targetAngle = 90f * facing;
            public void Raise() => bladeTarget = -20f * facing;

            public void Animate(float dt)
            {
                var step = dt == 0f ? 360f : dt * 360f;
                angle = Mathf.MoveTowardsAngle(angle, targetAngle, step);
                bladeAngle = Mathf.MoveTowardsAngle(bladeAngle, bladeTarget, step * 2f);
                Apply();
            }

            // UI Toolkit's `rotate` turns clockwise for a positive angle (it is y-down, like CSS); the
            // original's Z-euler turned counter-clockwise for a positive angle (it was y-up). Negating
            // keeps every angle above (and every caller of Fall/Raise) reading exactly as it did there.
            private void Apply()
            {
                root.style.rotate = new Rotate(Angle.Degrees(-angle));
                blade.style.rotate = new Rotate(Angle.Degrees(-bladeAngle));
            }
        }
    }
}
