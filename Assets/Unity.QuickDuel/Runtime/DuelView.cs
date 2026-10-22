using System.Collections.Generic;
using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.QuickDuel
{
    // Draws the duel from the controller's phase and result; it never decides anything. The whole UI is
    // built in code (placeholder silhouettes, no art assets), on a Screen Space - Camera canvas so the
    // main camera's render is the full picture.
    [RequireComponent(typeof(DuelController))]
    public sealed class DuelView : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float LetterboxHeight = 150f;
        private const float GroundY = -230f;

        private static readonly Color Paper = new Color(0.93f, 0.88f, 0.78f);
        private static readonly Color Ground = new Color(0.55f, 0.42f, 0.30f);
        private static readonly Color Ink = new Color(0.10f, 0.08f, 0.08f);
        private static readonly Color LeftColor = new Color(0.62f, 0.12f, 0.10f);
        private static readonly Color RightColor = new Color(0.14f, 0.20f, 0.45f);
        private static readonly Color Steel = new Color(0.80f, 0.82f, 0.86f);

        private readonly List<(Graphic graphic, Color color)> tinted = new List<(Graphic, Color)>();
        private DuelController controller;
        private Font font;
        private RectTransform letterboxTop;
        private RectTransform letterboxBottom;
        private Fighter left;
        private Fighter right;
        private Text title;
        private Text countdown;
        private Text cue;
        private Text headline;
        private Text prompt;
        private Text leftReaction;
        private Text rightReaction;
        private float letterbox;
        private bool inverted;

        public bool Inverted => inverted;
        public float LetterboxAmount => letterbox;
        public string CountdownText => countdown.enabled ? countdown.text : "";
        public string CueText => cue.enabled ? cue.text : "";
        public string HeadlineText => headline.enabled ? headline.text : "";
        public string PromptText => prompt.enabled ? prompt.text : "";
        public string ReactionText(Side side) => (side == Side.Left ? leftReaction : rightReaction).text;
        public bool HasFallen(Side side) => (side == Side.Left ? left : right).Fallen;

        // Other components (the taunt, a scoreboard) hang their own text off this canvas.
        public RectTransform Root { get; private set; }
        public Font Font => font;

        private void Awake()
        {
            controller = GetComponent<DuelController>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        public static string Headline(DuelResult result)
        {
            switch (result.Outcome)
            {
                case DuelOutcome.Tie: return "Draw";
                case DuelOutcome.NoStrike: return "Nobody struck";
                default: return result.Winner == Side.Left ? "Left wins" : "Right wins";
            }
        }

        private void Refresh(float dt)
        {
            var phase = controller.Phase;

            var countingDown = phase == DuelPhase.Countdown;
            countdown.enabled = countingDown;
            if (countingDown)
                countdown.text = Mathf.Max(1, Mathf.CeilToInt((float)controller.CountdownRemaining)).ToString();

            var tense = phase == DuelPhase.Standoff || phase == DuelPhase.Strike;
            letterbox = Mathf.MoveTowards(letterbox, tense ? 1f : 0f, dt * 3f);
            if (dt == 0f) letterbox = tense ? 1f : 0f;
            letterboxTop.sizeDelta = new Vector2(0, LetterboxHeight * letterbox);
            letterboxBottom.sizeDelta = new Vector2(0, LetterboxHeight * letterbox);

            SetInverted(phase == DuelPhase.Strike);
            cue.enabled = phase == DuelPhase.Strike;

            var resolved = phase == DuelPhase.Resolved;
            headline.enabled = resolved;
            if (resolved) headline.text = Headline(controller.Result);
            title.enabled = phase == DuelPhase.Ready;
            prompt.enabled = phase == DuelPhase.Ready || resolved;
            prompt.text = phase == DuelPhase.Ready
                ? "Left: A / gamepad 1     Right: L / gamepad 2\nPress Space or Start to duel"
                : "Press Space or Start for the next round";

            left.Animate(dt);
            right.Animate(dt);
        }

        private void SetInverted(bool value)
        {
            if (inverted == value) return;
            inverted = value;
            foreach (var (graphic, color) in tinted)
                graphic.color = value ? new Color(1 - color.r, 1 - color.g, 1 - color.b, color.a) : color;
        }

        private void Build()
        {
            var canvasObject = new GameObject("DuelCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            var camera = Camera.main;
            if (camera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.matchWidthOrHeight = 0.5f;
            Root = (RectTransform)canvasObject.transform;

            Stretch(Box("Sky", Root, Paper));
            var ground = Box("Ground", Root, Ground);
            ground.anchorMin = new Vector2(0, 0);
            ground.anchorMax = new Vector2(1, 0.5f);
            ground.offsetMin = Vector2.zero;
            ground.offsetMax = new Vector2(0, GroundY);

            left = new Fighter(this, "Left", -380f, LeftColor, 1f);
            right = new Fighter(this, "Right", 380f, RightColor, -1f);

            leftReaction = Label("LeftReaction", 56, new Vector2(-380f, GroundY - 80f), Ink);
            rightReaction = Label("RightReaction", 56, new Vector2(380f, GroundY - 80f), Ink);

            letterboxTop = Box("LetterboxTop", Root, Color.black, tint: false);
            letterboxTop.anchorMin = new Vector2(0, 1);
            letterboxTop.anchorMax = new Vector2(1, 1);
            letterboxTop.pivot = new Vector2(0.5f, 1);
            letterboxBottom = Box("LetterboxBottom", Root, Color.black, tint: false);
            letterboxBottom.anchorMin = new Vector2(0, 0);
            letterboxBottom.anchorMax = new Vector2(1, 0);
            letterboxBottom.pivot = new Vector2(0.5f, 0);

            title = Label("Title", 150, new Vector2(0, 260f), Ink);
            title.text = "QUICKDUEL";
            countdown = Label("Countdown", 320, new Vector2(0, 120f), Ink);
            cue = Label("Cue", 420, new Vector2(0, 120f), new Color(0.85f, 0.10f, 0.10f), tint: false);
            cue.text = "!";
            headline = Label("Headline", 120, new Vector2(0, 370f), Ink);
            prompt = Label("Prompt", 44, new Vector2(0, -440f), Ink);
        }

        internal RectTransform Box(string name, Transform parent, Color color, bool tint = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (tint) tinted.Add((image, color));
            return (RectTransform)go.transform;
        }

        public Text Label(string name, int size, Vector2 position, Color color, bool tint = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(Root, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            text.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(1600, size * 1.2f);
            rect.anchoredPosition = position;
            if (tint) tinted.Add((text, color));
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // A placeholder samurai: a body, a head and a drawn blade, pivoted at the feet so it can fall.
        private sealed class Fighter
        {
            private readonly RectTransform root;
            private readonly RectTransform blade;
            private readonly float facing;
            private float targetAngle;
            private float bladeTarget;

            public Fighter(DuelView view, string name, float x, Color color, float facing)
            {
                this.facing = facing;
                root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(view.Root, false);
                root.anchoredPosition = new Vector2(x, GroundY);
                root.sizeDelta = Vector2.zero;

                var body = view.Box("Body", root, color);
                body.pivot = new Vector2(0.5f, 0);
                body.sizeDelta = new Vector2(130, 250);
                body.anchoredPosition = Vector2.zero;

                var head = view.Box("Head", root, color);
                head.sizeDelta = new Vector2(96, 96);
                head.anchoredPosition = new Vector2(0, 310);

                var topknot = view.Box("Topknot", root, color);
                topknot.sizeDelta = new Vector2(30, 40);
                topknot.anchoredPosition = new Vector2(-30 * facing, 370);

                blade = view.Box("Blade", root, Steel);
                blade.pivot = new Vector2(0.5f, 0);
                blade.sizeDelta = new Vector2(14, 200);
                blade.anchoredPosition = new Vector2(60 * facing, 150);
                Reset();
            }

            public bool Fallen => Mathf.Abs(targetAngle) > 1f;

            public void Reset()
            {
                targetAngle = 0;
                bladeTarget = 150f * facing;    // sheathed: pointing back and down
                root.localRotation = Quaternion.identity;
                blade.localRotation = Quaternion.Euler(0, 0, bladeTarget);
            }

            public void Fall() => targetAngle = 90f * facing;
            public void Raise() => bladeTarget = -20f * facing;

            public void Animate(float dt)
            {
                var step = dt == 0f ? 360f : dt * 360f;
                var angle = Mathf.MoveTowardsAngle(root.localEulerAngles.z, targetAngle, step);
                root.localRotation = Quaternion.Euler(0, 0, angle);
                var b = Mathf.MoveTowardsAngle(blade.localEulerAngles.z, bladeTarget, step * 2f);
                blade.localRotation = Quaternion.Euler(0, 0, b);
            }
        }
    }
}
