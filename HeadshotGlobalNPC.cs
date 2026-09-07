using HeadshotMod.Common.Configs;
using HeadshotMod.Common.Players;
using HeadshotMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace HeadshotMod
{
    public class HeadshotGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private bool pendingHeadshot;
        private int pendingProjectileIdentity = -1;
        private int pendingProjectileOwner = -1;
        private Vector2 pendingImpactPosition;

        public override void ModifyHitByProjectile(
            NPC npc,
            Projectile projectile,
            ref NPC.HitModifiers modifiers
        )
        {
            ResetPendingHit();

            if (!CanReceiveHeadshot(npc) || !IsValidHeadshotProjectile(projectile))
                return;

            HeadshotConfig config = ModContent.GetInstance<HeadshotConfig>();
            Vector2 impactPos = GetApproximateImpactPosition(npc, projectile);
            float headBottomY = npc.Hitbox.Top + (npc.Hitbox.Height * config.HeadAreaPercentage);

            if (impactPos.Y <= headBottomY)
            {
                pendingHeadshot = true;
                pendingProjectileIdentity = projectile.identity;
                pendingProjectileOwner = projectile.owner;
                pendingImpactPosition = impactPos;

                // Aplica o multiplicador de dano e garante o acerto crítico
                modifiers.SourceDamage *= config.DamageMultiplier;
                modifiers.SetCrit();
            }
        }

        public override void OnHitByProjectile(
            NPC npc,
            Projectile projectile,
            NPC.HitInfo hit,
            int damageDone
        )
        {
            if (
                pendingHeadshot
                && projectile.identity == pendingProjectileIdentity
                && projectile.owner == pendingProjectileOwner
            )
            {
                pendingHeadshot = false;

                if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                {
                    Player player = Main.player[projectile.owner];

                    if (player.active && !player.dead)
                    {
                        HeadshotConfig config = ModContent.GetInstance<HeadshotConfig>();

                        // Oculta o texto de dano padrão para desenhar o texto crítico customizado
                        hit.HideCombatText = true;

                        // Registra o Headshot no jogador (ativa o pulso e o contador na UI)
                        HeadshotPlayer modPlayer = player.GetModPlayer<HeadshotPlayer>();
                        modPlayer.RegisterHeadshot();

                        player.AddBuff(ModContent.BuffType<HeadshotBuff>(), 180);

                        SpawnHeadshotEffects(
                            npc,
                            pendingImpactPosition,
                            damageDone,
                            player,
                            config
                        );
                    }
                }
            }

            ResetPendingHit();
        }

        private static bool CanReceiveHeadshot(NPC npc)
        {
            return npc.active
                && !npc.friendly
                && !npc.dontTakeDamage
                && npc.lifeMax > 5
                && npc.realLife < 0;
        }

        private static bool IsValidHeadshotProjectile(Projectile projectile)
        {
            if (!projectile.active || !projectile.friendly || projectile.hostile)
                return false;

            bool isMelee = projectile.CountsAsClass(DamageClass.Melee);
            bool isRanged = projectile.CountsAsClass(DamageClass.Ranged);
            bool isMagic = projectile.CountsAsClass(DamageClass.Magic);

            return isMelee || isRanged || isMagic;
        }

        private static Vector2 GetApproximateImpactPosition(NPC npc, Projectile projectile)
        {
            Rectangle intersection = Rectangle.Intersect(npc.Hitbox, projectile.Hitbox);
            if (intersection.Width > 0 && intersection.Height > 0)
                return new Vector2(intersection.Center.X, intersection.Center.Y);

            return new Vector2(
                MathHelper.Clamp(projectile.Center.X, npc.Hitbox.Left, npc.Hitbox.Right),
                MathHelper.Clamp(projectile.Center.Y, npc.Hitbox.Top, npc.Hitbox.Bottom)
            );
        }

        private static void SpawnHeadshotEffects(
            NPC npc,
            Vector2 impactPosition,
            int damageDone,
            Player player,
            HeadshotConfig config
        )
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Sangue
            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(
                    impactPosition,
                    6,
                    6,
                    DustID.Blood,
                    Main.rand.NextFloat(-3f, 3f),
                    Main.rand.NextFloat(-3f, 1f)
                );
            }

            // Hitmarker
            if (config.EnableHitmarker)
            {
                SpawnHitmarker(impactPosition, config);
            }

            // Rastro elétrico até o alvo
            SpawnHeadshotTrail(player.Center, impactPosition);

            // Som com alteração de tom baseada no combo atual
            HeadshotPlayer modPlayer = player.GetModPlayer<HeadshotPlayer>();
            float pitchBoost = MathHelper.Clamp(modPlayer.ComboCount * 0.04f, 0f, 0.3f);
            SoundEngine.PlaySound(
                SoundID.NPCHit4 with
                {
                    Volume = 0.9f,
                    Pitch = 0.2f + pitchBoost,
                },
                impactPosition
            );

            // Trepidação de tela (Screen Shake)
            if (config.EnableScreenShake && player.whoAmI == Main.myPlayer)
            {
                int safeDuration = System.Math.Max(1, config.ScreenShakeDuration);
                Vector2 shakeDirection = Main.rand.NextVector2Circular(1f, 1f);

                if (shakeDirection != Vector2.Zero)
                {
                    Main.instance.CameraModifiers.Add(
                        new PunchCameraModifier(
                            impactPosition,
                            shakeDirection,
                            config.ScreenShakeIntensity,
                            8f,
                            safeDuration,
                            -1f
                        )
                    );
                }
            }

            // Exibe o número de dano sobre o NPC com estilo de crítico (Dourado/Amarelo Grande)
            Rectangle damageArea = npc.getRect();
            CombatText.NewText(
                damageArea,
                new Color(255, 215, 0),
                damageDone.ToString(),
                dramatic: true
            );
        }

        private static void SpawnHitmarker(Vector2 impactPosition, HeadshotConfig config)
        {
            int centerDust = Dust.NewDust(
                impactPosition,
                0,
                0,
                DustID.Electric,
                0f,
                0f,
                50,
                Color.Cyan,
                config.HitmarkerScale * 1.5f
            );
            Main.dust[centerDust].noGravity = true;
            Main.dust[centerDust].velocity = Vector2.Zero;

            Vector2[] directions = new Vector2[]
            {
                new Vector2(-1, -1),
                new Vector2(1, -1),
                new Vector2(-1, 1),
                new Vector2(1, 1),
            };

            foreach (Vector2 dir in directions)
            {
                for (int i = 1; i <= 3; i++)
                {
                    Vector2 velocity = dir * (config.HitmarkerSpeed * (i * 0.4f));
                    int dustIndex = Dust.NewDust(
                        impactPosition,
                        0,
                        0,
                        DustID.Torch,
                        velocity.X,
                        velocity.Y,
                        100,
                        default,
                        config.HitmarkerScale
                    );

                    Dust dust = Main.dust[dustIndex];
                    dust.noGravity = true;
                    dust.fadeIn = 0.4f;
                    dust.velocity *= 0.85f;
                }
            }
        }

        private static void SpawnHeadshotTrail(Vector2 startPos, Vector2 impactPosition)
        {
            Vector2 direction = impactPosition - startPos;
            float distance = direction.Length();
            direction.Normalize();

            for (float d = 0; d < distance; d += 18f)
            {
                Vector2 particlePos = startPos + direction * d;
                int dustIndex = Dust.NewDust(
                    particlePos,
                    0,
                    0,
                    DustID.Electric,
                    0f,
                    0f,
                    100,
                    default,
                    0.5f
                );
                Dust dust = Main.dust[dustIndex];
                dust.noGravity = true;
                dust.velocity = Vector2.Zero;
            }
        }

        private void ResetPendingHit()
        {
            pendingHeadshot = false;
            pendingProjectileIdentity = -1;
            pendingProjectileOwner = -1;
            pendingImpactPosition = Vector2.Zero;
        }
    }
}
