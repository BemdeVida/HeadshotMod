using HeadshotMod.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace HeadshotMod.Content.Buffs
{
    public class HeadshotBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            HeadshotPlayer modPlayer = player.GetModPlayer<HeadshotPlayer>();

            // Limita os acúmulos para aplicar atributos entre 1x e 5x
            int stacks = System.Math.Clamp(modPlayer.ComboCount, 1, 5);

            player.GetAttackSpeed(DamageClass.Generic) += 0.10f * stacks;
            player.moveSpeed += 0.08f * stacks;

            int dustChance = System.Math.Max(1, 6 - stacks);

            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(dustChance))
            {
                int dust = Dust.NewDust(
                    player.position,
                    player.width,
                    player.height,
                    DustID.Electric,
                    0f,
                    0f,
                    100,
                    default,
                    0.6f + (stacks * 0.1f)
                );
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }
    }
}
