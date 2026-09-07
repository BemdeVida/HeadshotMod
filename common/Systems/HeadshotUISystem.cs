using System;
using System.Collections.Generic;
using HeadshotMod.Common.Configs;
using HeadshotMod.Common.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace HeadshotMod.Common.Systems
{
    public class HeadshotUISystem : ModSystem
    {
        private static bool isDragging = false;
        private static Vector2 dragOffset;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int resourceBarIndex = layers.FindIndex(layer =>
                layer.Name.Equals("Vanilla: Resource Bars")
            );
            if (resourceBarIndex != -1)
            {
                layers.Insert(
                    resourceBarIndex + 1,
                    new LegacyGameInterfaceLayer(
                        "HeadshotMod: ComboUIEditor",
                        delegate
                        {
                            DrawAndEditComboUI(Main.spriteBatch);
                            return true;
                        },
                        InterfaceScaleType.UI
                    )
                );
            }
        }

        private static void DrawAndEditComboUI(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            HeadshotConfig config = ModContent.GetInstance<HeadshotConfig>();
            if (!config.EnableComboUI)
                return;

            HeadshotPlayer modPlayer = player.GetModPlayer<HeadshotPlayer>();

            int displayCombo =
                modPlayer.ComboCount > 0 ? modPlayer.ComboCount : (Main.playerInventory ? 5 : 0);
            if (displayCombo <= 0)
                return;

            Vector2 position = new Vector2(
                Main.screenWidth * config.ComboUIX,
                Main.screenHeight * config.ComboUIY
            );
            string comboText = $"{displayCombo} HITS!";

            string subText = displayCombo switch
            {
                >= 5 => "OVERKILL!",
                >= 3 => "PUNISH!",
                _ => "BEAT!",
            };

            float baseScale = 1.15f;
            float totalScale = baseScale + modPlayer.HitPulse;
            float rotation = -0.08f;

            DynamicSpriteFont font = FontAssets.DeathText.Value;

            Vector2 textSize = font.MeasureString(comboText) * totalScale;
            Rectangle uiHitbox = new Rectangle(
                (int)position.X,
                (int)position.Y - 20,
                (int)textSize.X,
                (int)textSize.Y + 20
            );

            Vector2 mousePos = Main.MouseScreen;

            // Lógica de Arraste (Editor)
            if (Main.playerInventory || modPlayer.ComboCount > 0)
            {
                if (Main.mouseLeft && uiHitbox.Contains(mousePos.ToPoint()) && !isDragging)
                {
                    isDragging = true;
                    dragOffset = mousePos - position;
                }

                if (isDragging)
                {
                    if (Main.mouseLeft)
                    {
                        Vector2 newPos = mousePos - dragOffset;
                        config.ComboUIX = MathHelper.Clamp(newPos.X / Main.screenWidth, 0f, 0.9f);
                        config.ComboUIY = MathHelper.Clamp(newPos.Y / Main.screenHeight, 0f, 0.9f);
                    }
                    else
                    {
                        isDragging = false;
                    }
                }
            }

            // Paleta de Cores Dinâmica
            float colorProgress = MathHelper.Clamp((displayCombo - 1) / 4f, 0f, 1f);
            Color baseColor = Color.Lerp(
                new Color(255, 215, 0),
                new Color(255, 30, 0),
                colorProgress
            );

            if (displayCombo >= 5)
            {
                float pulse = (float)(Math.Sin(Main.GameUpdateCount * 0.2f) * 0.5 + 0.5);
                baseColor = Color.Lerp(baseColor, Color.Magenta, pulse);
            }

            if (Main.playerInventory)
            {
                Utils.DrawBorderString(
                    spriteBatch,
                    "[Arraste para Mover]",
                    position - new Vector2(0, 45f),
                    Color.LightGreen * 0.9f,
                    0.8f
                );
            }

            // 1. Subtexto Estilo Fighting Game
            DynamicSpriteFontExtensionMethods.DrawString(
                spriteBatch,
                font,
                subText,
                position - new Vector2(0, 26f),
                Color.OrangeRed,
                rotation,
                Vector2.Zero,
                0.45f,
                SpriteEffects.None,
                0f
            );

            // 2. Aura de Brilho / Glow (Efeito Neon ao fundo)
            Vector2[] glowOffsets = new Vector2[]
            {
                new Vector2(-2, -2),
                new Vector2(2, -2),
                new Vector2(-2, 2),
                new Vector2(2, 2),
            };

            foreach (Vector2 offset in glowOffsets)
            {
                DynamicSpriteFontExtensionMethods.DrawString(
                    spriteBatch,
                    font,
                    comboText,
                    position + offset,
                    baseColor * 0.4f,
                    rotation,
                    Vector2.Zero,
                    totalScale * 1.03f,
                    SpriteEffects.None,
                    0f
                );
            }

            // 3. Sombra de Profundidade
            DynamicSpriteFontExtensionMethods.DrawString(
                spriteBatch,
                font,
                comboText,
                position + new Vector2(4f, 4f),
                Color.Black * 0.85f,
                rotation,
                Vector2.Zero,
                totalScale,
                SpriteEffects.None,
                0f
            );

            // 4. Texto Principal
            Color finalTextColor = isDragging ? Color.Cyan : baseColor;
            DynamicSpriteFontExtensionMethods.DrawString(
                spriteBatch,
                font,
                comboText,
                position,
                finalTextColor,
                rotation,
                Vector2.Zero,
                totalScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
