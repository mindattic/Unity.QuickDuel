using System;
using UnityEngine;

namespace Unity.QuickDuel
{
    // One of the original arenas as the Duel scene carries it (UQD-T33): the manifest entry (Data/assets.json,
    // from load_arenas.gml) with its backdrop sprite resolved to the imported asset. Written by DuelSceneBuilder
    // from the manifest, never by hand; DuelView draws the one the round's arena roll lands on.
    [Serializable]
    public sealed class ArenaBackdrop
    {
        [SerializeField] private string id;
        [SerializeField] private string name;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite sprite;

        public ArenaBackdrop(string id, string name, string description, Sprite sprite)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.sprite = sprite;
        }

        public string Id => id;
        public string Name => name;
        public string Description => description;
        public Sprite Sprite => sprite;
    }
}
