using System;
using UnityEngine;

namespace SoftballConnect.Content
{
    [Serializable]
    public sealed class PlayerViewSettings
    {
        [Tooltip("Metres from home plate. +Z faces the pitcher; +Y is eye height.")]
        public Vector3 EyePosition = new Vector3(-0.72f, 1.65f, -0.80f);
        [Range(50, 100)] public float DesktopFieldOfView = 82;
        [Range(0.01f, 0.5f)] public float MouseSensitivity = 0.10f;
    }
}
