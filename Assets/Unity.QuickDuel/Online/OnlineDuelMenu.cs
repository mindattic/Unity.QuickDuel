using System;
using Unity.Multiplayer;      // NetBootstrap
using Unity.Multiplayer.Core; // HostAddress, JoinCodeProvider
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Unity.QuickDuel.Online
{
    // The Duel scene's Host / Join menu (UQD-T21): a small panel at the left edge, under the left score, built in code on DuelView's
    // canvas, shown between rounds. Host starts a listen server on the scene's NetBootstrap (this player duels on the
    // left); Join dials an address ("192.168.1.20" or "192.168.1.20:7777") and plays the right; Leave ends either and
    // drops back to the local game. The session flow itself is OnlineDuelLobby's; this only draws it and turns clicks
    // into its calls. DuelSceneBuilder adds it, wired to the scene's NetBootstrap and the OnlineDuel prefab.
    [RequireComponent(typeof(DuelController), typeof(DuelView))]
    public sealed class OnlineDuelMenu : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.10f, 0.08f, 0.08f);
        private static readonly Color Panel = new Color(0.10f, 0.08f, 0.08f, 0.82f);
        private static readonly Color Paper = new Color(0.93f, 0.88f, 0.78f);
        private static readonly Color Accent = new Color(0.62f, 0.12f, 0.10f);

        [SerializeField] private NetBootstrap bootstrap;
        [SerializeField] private GameObject duelPrefab;

        private DuelController controller;
        private DuelView view;
        private RectTransform panel;
        private Button hostButton;
        private Button joinButton;
        private Button leaveButton;
        private InputField address;
        private Text status;

        public OnlineDuelLobby Lobby { get; private set; }

        public bool IsShown => panel != null && panel.gameObject.activeSelf;
        public string StatusText => status != null ? status.text : "";
        public string Address
        {
            get => address != null ? address.text : "";
            set { if (address != null) address.text = value; }
        }

        // Called by DuelSceneBuilder (and tests) in place of Inspector wiring.
        public void Wire(NetBootstrap netBootstrap, GameObject onlineDuelPrefab)
        {
            bootstrap = netBootstrap;
            duelPrefab = onlineDuelPrefab;
        }

        private void Start()
        {
            controller = GetComponent<DuelController>();
            view = GetComponent<DuelView>();
            if (bootstrap == null)
                bootstrap = NetBootstrap.Instance;
            Build();
            if (bootstrap == null || bootstrap.Session == null)
            {
                status.text = "Online play is not set up in this scene.";
                hostButton.interactable = joinButton.interactable = false;
                return;
            }

            // One opponent, joined by address: no join-code service is set up for QuickDuel.
            if (!bootstrap.Session.IsActive)
                bootstrap.Configure(c => { c.MaxPlayers = 2; c.JoinCodes = JoinCodeProvider.None; });

            Lobby = new OnlineDuelLobby(bootstrap.Session, duelPrefab, controller);
            Lobby.Changed += Redraw;
            Redraw();
        }

        private void OnDestroy()
        {
            if (Lobby != null)
            {
                Lobby.Changed -= Redraw;
                Lobby.Dispose();
                Lobby = null;
            }
        }

        private void Update()
        {
            // Shown between rounds only, never over a round being played.
            if (panel != null)
                panel.gameObject.SetActive(controller.CanBeginRound);
        }

        // Hosts a listen server; this machine duels on the left. False when it could not start.
        public bool Host() => Try(() => bootstrap.StartHost(), "Starting the host...");

        // Joins the host at `text` ("address" or "address:port"); this machine duels on the right.
        public bool Join(string text)
        {
            if (Lobby == null)
                return false;
            var port = bootstrap.BuildConfig().Port;
            if (!HostAddress.TryParse((text ?? "").Trim(), port, out var target, out var error))
            {
                Lobby.Failed(error);
                return false;
            }
            return Try(() => bootstrap.JoinByAddress(target.Host, target.Port), $"Joining {target}...");
        }

        public bool Leave() => bootstrap != null && bootstrap.Leave();

        private bool Try(Func<bool> start, string starting)
        {
            if (Lobby == null)
                return false;
            try
            {
                Lobby.Starting(starting);
                if (start())
                    return true;
                Lobby.Failed("Could not start: is a game already running?");
            }
            catch (Exception ex)
            {
                Lobby.Failed(ex.Message);
            }
            return false;
        }

        private void Redraw()
        {
            if (panel == null || Lobby == null)
                return;
            var active = Lobby.IsActive;
            hostButton.gameObject.SetActive(!active);
            joinButton.gameObject.SetActive(!active);
            address.gameObject.SetActive(!active);
            leaveButton.gameObject.SetActive(active);
            status.text = Lobby.Status;
        }

        private void Build()
        {
            EnsureEventSystem();

            panel = new GameObject("OnlineMenu", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(view.Root, false);
            // Pinned to the left edge at the scoreboard's height (which is centre-anchored), just under the left score.
            panel.anchorMin = panel.anchorMax = new Vector2(0, 0.5f);
            panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(40, 440);
            panel.sizeDelta = new Vector2(340, 240);
            panel.gameObject.AddComponent<Image>().color = Panel;   // also keeps clicks on it from striking

            Text(panel, "Heading", "ONLINE", 30, new Vector2(20, -12), new Vector2(300, 36), Paper);
            hostButton = MakeButton(panel, "Host", "Host", new Vector2(20, -56), new Vector2(300, 48), () => Host());
            address = MakeField(panel, new Vector2(20, -114), new Vector2(190, 48));
            joinButton = MakeButton(panel, "Join", "Join", new Vector2(220, -114), new Vector2(100, 48), () => Join(address.text));
            leaveButton = MakeButton(panel, "Leave", "Leave", new Vector2(20, -56), new Vector2(300, 48), () => Leave());
            status = Text(panel, "Status", "", 22, new Vector2(20, -172), new Vector2(300, 60), Paper);
            status.horizontalOverflow = HorizontalWrapMode.Wrap;
            status.alignment = TextAnchor.UpperLeft;
            leaveButton.gameObject.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
                return;
            var events = new GameObject("EventSystem", typeof(EventSystem));
            events.AddComponent<InputSystemUIInputModule>();
        }

        private Text Text(RectTransform parent, string name, string value, int size, Vector2 at, Vector2 box, Color color)
        {
            var rect = Place(new GameObject(name, typeof(RectTransform)), parent, at, box);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = view.Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private Button MakeButton(RectTransform parent, string name, string label, Vector2 at, Vector2 box, Action onClick)
        {
            var rect = Place(new GameObject(name, typeof(RectTransform)), parent, at, box);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Paper;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var caption = Text(rect, "Label", label, 26, Vector2.zero, box, Ink);
            caption.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(() =>
            {
                // Let go of the button, so Space (also the UI's Submit) starts a round instead of clicking it again.
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
                onClick();
            });
            return button;
        }

        private InputField MakeField(RectTransform parent, Vector2 at, Vector2 box)
        {
            var rect = Place(new GameObject("Address", typeof(RectTransform)), parent, at, box);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var field = rect.gameObject.AddComponent<InputField>();
            field.targetGraphic = image;
            var inner = new Vector2(box.x - 16, box.y);
            var text = Text(rect, "Text", "", 24, new Vector2(8, 0), inner, Ink);
            text.supportRichText = false;
            var hint = Text(rect, "Placeholder", "host address", 24, new Vector2(8, 0), inner, new Color(Ink.r, Ink.g, Ink.b, 0.4f));
            hint.fontStyle = FontStyle.Italic;
            field.textComponent = text;
            field.placeholder = hint;
            field.lineType = InputField.LineType.SingleLine;
            field.caretColor = Accent;
            field.text = "127.0.0.1";
            return field;
        }

        private static RectTransform Place(GameObject go, RectTransform parent, Vector2 at, Vector2 box)
        {
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = at;
            rect.sizeDelta = box;
            return rect;
        }
    }
}
