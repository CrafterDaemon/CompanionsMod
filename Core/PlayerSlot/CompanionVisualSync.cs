using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// Manually syncs a companion Player's visual armor/accessory slot IDs
/// and applies accessory effects (especially mobility accessories like wings,
/// boots, hooks, etc.), since Player.UpdateEquips() only fully runs for
/// Main.myPlayer. Without this, armor sprites won't render and accessory
/// effects won't activate on companion players.
/// </summary>
public static class CompanionVisualSync
{
    public static void SyncVisuals(Player companion)
    {
        if (companion == null)
            return;

        // Head armor visual
        var head = companion.armor[0];
        companion.head = (head != null && !head.IsAir && head.headSlot >= 0)
            ? head.headSlot
            : -1;

        // Body armor visual
        var body = companion.armor[1];
        companion.body = (body != null && !body.IsAir && body.bodySlot >= 0)
            ? body.bodySlot
            : -1;

        // Leg armor visual
        var legs = companion.armor[2];
        companion.legs = (legs != null && !legs.IsAir && legs.legSlot >= 0)
            ? legs.legSlot
            : -1;

        // Reset all accessory visuals first
        for (int i = 0; i < companion.hideVisibleAccessory.Length; i++)
            companion.hideVisibleAccessory[i] = false;

        // Apply accessory effects (armor slots 3-7 are accessories)
        ApplyAccessoryEffects(companion);

        // Held item visual — make sure the selected weapon shows
        var heldItem = companion.inventory[companion.selectedItem];
        if (heldItem != null && !heldItem.IsAir)
        {
            companion.HeldItem.netDefaults(heldItem.netID);
            if (companion.itemAnimation > 0)
            {
                companion.bodyFrame.Y = companion.bodyFrame.Height * 5; // swing frame
            }
        }
    }

    /// <summary>
    /// Manually applies accessory effects that Player.UpdateEquips() would
    /// normally handle. Focuses on mobility-related flags so companions can
    /// use wings, boots, hooks, movement accessories, etc.
    /// </summary>
    private static void ApplyAccessoryEffects(Player companion)
    {
        // Reset mobility flags each frame before re-applying from equipment
        ResetMobilityFlags(companion);

        // Scan all accessory slots (armor[3] through armor[7])
        for (int slot = 3; slot <= 7; slot++)
        {
            var acc = companion.armor[slot];
            if (acc == null || acc.IsAir)
                continue;

            // Apply vanilla accessory effects based on item type
            ApplyVanillaAccessoryEffect(companion, acc);

            // Apply modded accessory effects via ModItem.UpdateEquip
            if (acc.ModItem != null)
            {
                try
                {
                    acc.ModItem.UpdateEquip(companion);
                }
                catch
                {
                    // Silently handle if a mod's UpdateEquip has unexpected issues
                }
            }
        }

        // Also apply armor set bonus effects via ModItem.UpdateEquip on armor pieces
        for (int slot = 0; slot <= 2; slot++)
        {
            var armor = companion.armor[slot];
            if (armor == null || armor.IsAir || armor.ModItem == null)
                continue;

            try
            {
                armor.ModItem.UpdateEquip(companion);
            }
            catch { }
        }
    }

    private static void ResetMobilityFlags(Player companion)
    {
        companion.accRunSpeed = 0f;
        companion.rocketBoots = 0;
        companion.moveSpeed = 1f;
        companion.wingTimeMax = 0;
        companion.wingsLogic = 0;
        companion.jumpBoost = false;
        companion.noFallDmg = false;
        companion.iceSkate = false;
        companion.waterWalk = false;
        companion.waterWalk2 = false;
        companion.fireWalk = false;
        companion.lavaMax = 0;
        companion.accFlipper = false;
        companion.accDivingHelm = false;
        companion.accMerman = false;
        companion.arcticDivingGear = false;
        companion.dash = 0;
        companion.spikedBoots = 0;
        companion.blackBelt = false;
        companion.lavaRose = false;
        companion.longInvince = false;
        companion.pStone = false;
    }

    /// <summary>
    /// Applies known vanilla accessory effects by checking the item type.
    /// This covers the most commonly used mobility accessories.
    /// </summary>
    private static void ApplyVanillaAccessoryEffect(Player companion, Item acc)
    {
        int type = acc.type;

        // --- Wings ---
        // Wings are detected by their wingSlot value
        if (acc.wingSlot > 0)
        {
            companion.wingsLogic = acc.wingSlot;
            companion.wingTimeMax = GetWingFlightTime(acc.wingSlot);
            companion.noFallDmg = true;
        }

        // --- Boots & Run Speed ---
        if (type == ItemID.HermesBoots || type == ItemID.FlurryBoots || type == ItemID.SailfishBoots)
        {
            companion.accRunSpeed = 6f;
        }
        else if (type == ItemID.SpectreBoots)
        {
            companion.accRunSpeed = 6f;
            companion.rocketBoots = 2;
        }
        else if (type == ItemID.LightningBoots)
        {
            companion.accRunSpeed = 6.75f;
            companion.rocketBoots = 2;
            companion.moveSpeed += 0.08f;
        }
        else if (type == ItemID.FrostsparkBoots)
        {
            companion.accRunSpeed = 6.75f;
            companion.rocketBoots = 3;
            companion.moveSpeed += 0.08f;
            companion.iceSkate = true;
        }
        else if (type == ItemID.TerrasparkBoots)
        {
            companion.accRunSpeed = 6.75f;
            companion.rocketBoots = 3;
            companion.moveSpeed += 0.08f;
            companion.iceSkate = true;
            companion.waterWalk = true;
            companion.fireWalk = true;
            companion.lavaMax += 420;
            companion.noFallDmg = true;
        }
        else if (type == ItemID.RocketBoots)
        {
            companion.rocketBoots = 1;
        }

        // --- Jump accessories ---
        else if (type == ItemID.CloudinaBottle || type == ItemID.BlizzardinaBottle
            || type == ItemID.SandstorminaBottle || type == ItemID.TsunamiInABottle
            || type == ItemID.FartinaJar)
        {
            companion.jumpBoost = true;
        }
        else if (type == ItemID.BalloonHorseshoeFart || type == ItemID.BalloonHorseshoeHoney
            || type == ItemID.BalloonHorseshoeSharkron || type == ItemID.BlueHorseshoeBalloon
            || type == ItemID.WhiteHorseshoeBalloon || type == ItemID.YellowHorseshoeBalloon
            || type == ItemID.BundleofBalloons)
        {
            companion.jumpBoost = true;
            companion.noFallDmg = true;
        }

        // --- Shield of Cthulhu / dash ---
        else if (type == ItemID.EoCShield)
        {
            companion.dash = 2;
        }
        else if (type == ItemID.Tabi)
        {
            companion.dash = 1;
        }
        else if (type == ItemID.MasterNinjaGear)
        {
            companion.dash = 1;
            companion.blackBelt = true;
            companion.spikedBoots = 2;
        }

        // --- Climbing ---
        else if (type == ItemID.ShoeSpikes)
        {
            companion.spikedBoots++;
        }
        else if (type == ItemID.ClimbingClaws)
        {
            companion.spikedBoots++;
        }
        else if (type == ItemID.TigerClimbingGear)
        {
            companion.spikedBoots = 2;
        }

        // --- Water / Lava ---
        else if (type == ItemID.WaterWalkingBoots || type == ItemID.ObsidianWaterWalkingBoots)
        {
            companion.waterWalk = true;
            if (type == ItemID.ObsidianWaterWalkingBoots)
                companion.fireWalk = true;
        }
        else if (type == ItemID.Flipper)
        {
            companion.accFlipper = true;
        }
        else if (type == ItemID.DivingHelmet)
        {
            companion.accDivingHelm = true;
        }
        else if (type == ItemID.NeptunesShell)
        {
            companion.accMerman = true;
        }
        else if (type == ItemID.MoonShell)
        {
            companion.accMerman = true;
        }
        else if (type == ItemID.CelestialShell)
        {
            companion.accMerman = true;
        }
        else if (type == ItemID.ArcticDivingGear)
        {
            companion.arcticDivingGear = true;
            companion.accFlipper = true;
            companion.accDivingHelm = true;
            companion.iceSkate = true;
        }
        else if (type == ItemID.LavaCharm || type == ItemID.MoltenCharm)
        {
            companion.lavaMax += 420;
        }
        else if (type == ItemID.LavaWaders)
        {
            companion.waterWalk = true;
            companion.fireWalk = true;
            companion.lavaMax += 420;
        }

        // --- Survivability ---
        else if (type == ItemID.CobaltShield || type == ItemID.ObsidianShield
            || type == ItemID.PaladinsShield || type == ItemID.AnkhShield
            || type == ItemID.HeroShield)
        {
            companion.noKnockback = true;
            if (type == ItemID.ObsidianShield || type == ItemID.AnkhShield)
                companion.fireWalk = true;
        }
        else if (type == ItemID.CrossNecklace)
        {
            companion.longInvince = true;
        }
        else if (type == ItemID.StarCloak)
        {
            companion.longInvince = true;
        }

        // --- Generic defense from any accessory ---
        if (acc.defense > 0)
        {
            companion.statDefense += acc.defense;
        }
    }

    /// <summary>
    /// Returns approximate flight time for a given wing slot.
    /// These are simplified values; vanilla calculates them more precisely
    /// but this covers the common tiers.
    /// </summary>
    private static int GetWingFlightTime(int wingSlot)
    {
        // Wing tiers by approximate flight time
        // Pre-hardmode wings: ~80 ticks
        // Early hardmode: ~100-120
        // Mid hardmode: ~140-160
        // Late hardmode: ~180+
        // Endgame: ~200+
        return wingSlot switch
        {
            // Angel/Demon/Leaf Wings
            1 or 2 or 3 => 100,
            // Jetpack, Butterfly, Fairy, Frozen, Flame, etc.
            4 or 5 or 6 or 7 or 8 => 120,
            // Beetle, Hoverboard
            9 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20 => 150,
            // Fishron, Nebula, Vortex, Solar, Stardust
            21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or 30 => 180,
            // Celestial Starboard, Empress
            _ => 200,
        };
    }
}
