using System.Collections.Generic;
using CompanionsMod.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace CompanionsMod.UI;

public class CompanionUISystem : ModSystem
{
    public override void UpdateUI(GameTime gameTime)
    {
        var cp = Main.LocalPlayer.GetModPlayer<CompanionPlayer>();

        // Toggle equipment panel via keybind
        if (CompanionKeybinds.OpenEquipmentPanel.JustPressed)
        {
            if (cp.ActiveCompanionId != null)
                CompanionEquipmentPanel.Toggle();
        }

        // Toggle order panel via keybind
        if (CompanionKeybinds.OpenOrderPanel.JustPressed)
        {
            if (cp.ActiveCompanionId != null)
                CompanionOrderPanel.Toggle();
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        // --- NPC chat companion panel ---
        int chatLayer = layers.FindIndex(l => l.Name.Equals("Vanilla: NPC Chat Buttons"));
        int insertAfter = chatLayer >= 0
            ? chatLayer
            : layers.FindIndex(l => l.Name.Equals("Vanilla: Inventory"));

        if (insertAfter >= 0)
        {
            layers.Insert(insertAfter + 1, new LegacyGameInterfaceLayer(
                "CompanionsMod: NPC Chat Panel",
                () =>
                {
                    CompanionNPCChatUI.Draw(Main.spriteBatch);
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }

        // --- Active-companion UI (health bar, equipment panel, order panel) ---
        var cp = Main.LocalPlayer?.GetModPlayer<CompanionPlayer>();
        if (cp?.ActiveCompanionId == null)
            return;

        int inventoryIndex = layers.FindIndex(l => l.Name.Equals("Vanilla: Inventory"));
        if (inventoryIndex == -1)
            return;

        var clientConfig = ModContent.GetInstance<CompanionClientConfig>();

        if (clientConfig?.ShowCompanionHealthBar != false)
        {
            layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                "CompanionsMod: Health Bar",
                () =>
                {
                    CompanionHealthBarLayer.Draw();
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }

        layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
            "CompanionsMod: Equipment Panel",
            () =>
            {
                if (CompanionEquipmentPanel.IsOpen)
                    CompanionEquipmentPanel.Draw();
                return true;
            },
            InterfaceScaleType.UI
        ));

        layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
            "CompanionsMod: Order Panel",
            () =>
            {
                CompanionOrderPanel.Draw();
                return true;
            },
            InterfaceScaleType.UI
        ));
    }
}
