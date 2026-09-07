using Terraria;
using Terraria.ModLoader;

namespace HeadshotMod.Common.Players
{
    public class HeadshotPlayer : ModPlayer
    {
        public int ComboCount { get; private set; }
        public int ComboTimer { get; private set; }
        public float HitPulse { get; set; } // Controla o efeito "pop-in" do Guilty Gear

        public override void PostUpdateMiscEffects()
        {
            if (ComboTimer > 0)
            {
                ComboTimer--;
                if (ComboTimer <= 0)
                {
                    ComboCount = 0;
                }
            }

            // Suaviza o impacto visual da animação até retornar ao tamanho normal
            if (HitPulse > 0f)
            {
                HitPulse *= 0.85f;
            }
        }

        public void RegisterHeadshot()
        {
            ComboCount++;
            ComboTimer = 180; // 3 segundos de duração para manter o combo
            HitPulse = 0.6f; // Força o salto inicial de escala (efeito Pop-In)
        }

        public override void OnRespawn()
        {
            ComboCount = 0;
            ComboTimer = 0;
            HitPulse = 0f;
        }
    }
}
