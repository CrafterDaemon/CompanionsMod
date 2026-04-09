using Terraria;

namespace CompanionsMod.Core.PlayerSlot;

/// <summary>
/// Creates and initializes a companion Player object in a Main.player[] slot.
/// </summary>
public static class CompanionPlayerInit
{
    public static Player CreateCompanionPlayer(Player owner, string companionId, int slotIndex)
    {
        var def = CompanionRegistry.GetCompanion(companionId);
        if (def == null)
            return null;

        var player = new Player();
        player.whoAmI = slotIndex;
        Main.player[slotIndex] = player;

        // Basic setup
        player.name = def.DisplayName ?? companionId;
        player.active = true;
        player.dead = false;

        // Position at owner
        player.position = owner.position;
        player.velocity = Microsoft.Xna.Framework.Vector2.Zero;
        player.direction = owner.direction;
        player.width = owner.width;
        player.height = owner.height;

        // Stats from definition
        player.statLifeMax = def.BaseStats.BaseHealth;
        player.statLifeMax2 = def.BaseStats.BaseHealth;
        player.statLife = def.BaseStats.BaseHealth;
        player.statDefense += def.BaseStats.BaseDefense;
        player.statManaMax = 20;
        player.statMana = 20;

        // Apply appearance
        var appearance = def.Appearance ?? new CompanionAppearance();
        player.hair = appearance.HairStyle;
        player.hairColor = appearance.HairColor;
        player.skinColor = appearance.SkinColor;
        player.eyeColor = appearance.EyeColor;
        player.shirtColor = appearance.ShirtColor;
        player.underShirtColor = appearance.UnderShirtColor;
        player.pantsColor = appearance.PantsColor;
        player.shoeColor = appearance.ShoeColor;

        // Prevent vanilla systems from treating this as a client-controlled player
        // The player should be on the same team as the owner
        player.team = owner.team;

        // Initialize inventory with empty items
        for (int i = 0; i < player.inventory.Length; i++)
        {
            if (player.inventory[i] == null)
                player.inventory[i] = new Item();
        }

        for (int i = 0; i < player.armor.Length; i++)
        {
            if (player.armor[i] == null)
                player.armor[i] = new Item();
        }

        // Sync equipment from the companion's equipment storage
        var companionPlayer = owner.GetModPlayer<CompanionPlayer>();
        var equipment = companionPlayer.GetEquipment(companionId);
        EquipmentBridge.SyncEquipmentToPlayer(equipment, player);

        return player;
    }
}
