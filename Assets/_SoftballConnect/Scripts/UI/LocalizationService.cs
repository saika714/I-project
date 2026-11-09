using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoftballConnect.UI
{
    public sealed class LocalizationService
    {
        [Serializable] sealed class Entry { public string key; public string en; public string ja; }
        [Serializable] sealed class Catalog { public Entry[] entries; }
        readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        public bool Japanese { get; private set; }
        public LocalizationService(TextAsset source)
        {
            foreach (var entry in JsonUtility.FromJson<Catalog>(source.text).entries) entries.Add(entry.key, entry);
        }
        public void Toggle() { Japanese = !Japanese; }
        public string Get(string key) => entries.TryGetValue(key, out var entry) ? (Japanese ? entry.ja : entry.en) : key;
    }
}
