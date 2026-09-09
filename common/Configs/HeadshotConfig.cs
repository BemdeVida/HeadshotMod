using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace HeadshotMod.Common.Configs
{
    public class HeadshotConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Interface")]
        [DefaultValue(true)]
        public bool EnableComboUI;

        [Header("Combat")]
        [DefaultValue(1.5f)]
        [Range(1.0f, 50.0f)]
        [Increment(0.2f)]
        [Slider]
        public float DamageMultiplier;

        [DefaultValue(0.25f)]
        [Range(0.05f, 1f)]
        [Increment(0.01f)]
        [Slider]
        public float HeadAreaPercentage;

        [Header("Visuals")]
        [DefaultValue(true)]
        public bool EnableHitmarker;

        [DefaultValue(1.0f)]
        [Range(0.5f, 3.0f)]
        [Increment(0.1f)]
        [Slider]
        public float HitmarkerScale;

        [DefaultValue(8.0f)]
        [Range(1.0f, 20.0f)]
        [Increment(1.0f)]
        [Slider]
        public float HitmarkerSpeed;

        [DefaultValue(true)]
        public bool EnableScreenShake;

        [DefaultValue(6)]
        [Range(1, 30)]
        [Increment(1)]
        [Slider]
        public int ScreenShakeDuration;

        [DefaultValue(12.0f)]
        [Range(1.0f, 30.0f)]
        [Increment(1.0f)]
        [Slider]
        public float ScreenShakeIntensity;
    }
}
