using System.Collections.Generic;
using Unity.QuickDuel.Core;
using UnityEngine;

namespace Unity.QuickDuel
{
    // The 2013 GameMaker build's sound beats: a beep on each of 3-2-1, a drumroll through the standoff,
    // the blade unsheathed on the cue, a slash when a strike settles the round. Clips are the original
    // Bushido Coda sounds, wired in by the scene builder.
    [RequireComponent(typeof(DuelController))]
    public sealed class DuelAudio : MonoBehaviour
    {
        public const string Beep = "beep";
        public const string Drumroll = "drumroll";
        public const string Unsheath = "unsheath";
        public const string Slash = "slash";

        [SerializeField] private AudioClip beep;
        [SerializeField] private AudioClip drumroll;
        [SerializeField] private AudioClip unsheath;
        [SerializeField] private AudioClip slash;

        private DuelController controller;
        private AudioSource oneShots;
        private AudioSource loop;
        private int lastCount;

        // Every cue played, in order, for tests and debugging.
        public List<string> Played { get; } = new List<string>();
        public bool DrumrollPlaying { get; private set; }

        public void SetClips(AudioClip beepClip, AudioClip drumrollClip, AudioClip unsheathClip, AudioClip slashClip)
        {
            beep = beepClip;
            drumroll = drumrollClip;
            unsheath = unsheathClip;
            slash = slashClip;
        }

        private void Awake()
        {
            controller = GetComponent<DuelController>();
            oneShots = gameObject.AddComponent<AudioSource>();
            oneShots.playOnAwake = false;
            loop = gameObject.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            controller.RoundStarted += () => lastCount = 0;
            controller.PhaseChanged += OnPhaseChanged;
            controller.Resolved += OnResolved;
        }

        private void Update()
        {
            if (controller.Phase != DuelPhase.Countdown)
                return;
            var count = Mathf.Max(1, Mathf.CeilToInt((float)controller.CountdownRemaining));
            if (count != lastCount)
            {
                lastCount = count;
                Play(Beep, beep);
            }
        }

        private void OnPhaseChanged(DuelPhase phase)
        {
            if (phase == DuelPhase.Standoff)
            {
                DrumrollPlaying = true;
                Played.Add(Drumroll);
                if (drumroll != null)
                {
                    loop.clip = drumroll;
                    loop.Play();
                }
                return;
            }

            StopDrumroll();
            if (phase == DuelPhase.Strike)
                Play(Unsheath, unsheath);
        }

        private void OnResolved(DuelResult result)
        {
            StopDrumroll();
            if (result.Outcome != DuelOutcome.NoStrike)
                Play(Slash, slash);
        }

        private void StopDrumroll()
        {
            DrumrollPlaying = false;
            loop.Stop();
        }

        private void Play(string name, AudioClip clip)
        {
            Played.Add(name);
            if (clip != null)
                oneShots.PlayOneShot(clip);
        }
    }
}
