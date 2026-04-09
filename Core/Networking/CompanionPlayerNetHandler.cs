using System.IO;
using CompanionsMod.Core.AI.Brain;
using CompanionsMod.Core.PlayerSlot;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CompanionsMod.Core.Networking;

/// <summary>
/// Network handler for the player-based companion system.
/// The owning client runs the AI; the server coordinates slot assignments
/// and relays position/state updates to other clients.
/// </summary>
public static class CompanionPlayerNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var packetType = (CompanionPlayerPacketType)reader.ReadByte();
        HandlePacketWithType(packetType, reader, whoAmI);
    }

    public static void HandlePacketWithType(CompanionPlayerPacketType packetType, BinaryReader reader, int whoAmI)
    {
        switch (packetType)
        {
            case CompanionPlayerPacketType.SpawnCompanionPlayer:
                ReceiveSpawn(reader, whoAmI);
                break;
            case CompanionPlayerPacketType.DespawnCompanionPlayer:
                ReceiveDespawn(reader, whoAmI);
                break;
            case CompanionPlayerPacketType.SyncCompanionPosition:
                ReceivePosition(reader, whoAmI);
                break;
            case CompanionPlayerPacketType.SyncCompanionEquipment:
                ReceiveEquipment(reader, whoAmI);
                break;
            case CompanionPlayerPacketType.SyncCompanionHealth:
                ReceiveHealth(reader, whoAmI);
                break;
            case CompanionPlayerPacketType.SyncCompanionOrder:
                ReceiveOrder(reader, whoAmI);
                break;
        }
    }

    // --- Spawn ---

    public static void SendSpawnCompanionPlayer(int ownerIndex, string companionId, int slotIndex)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPlayerPacketType.SpawnCompanionPlayer);
        packet.Write(ownerIndex);
        packet.Write(companionId);
        packet.Write(slotIndex);
        packet.Send();
    }

    private static void ReceiveSpawn(BinaryReader reader, int whoAmI)
    {
        int ownerIndex = reader.ReadInt32();
        string companionId = reader.ReadString();
        int slotIndex = reader.ReadInt32();

        if (Main.netMode == NetmodeID.Server)
        {
            // Server: relay to all other clients
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPlayerPacketType.SpawnCompanionPlayer);
            relay.Write(ownerIndex);
            relay.Write(companionId);
            relay.Write(slotIndex);
            relay.Send(-1, whoAmI);
        }
        else
        {
            // Client: create the companion player entity for a remote player
            var owner = Main.player[ownerIndex];
            if (owner == null || !owner.active)
                return;

            var companionPlayer = CompanionPlayerInit.CreateCompanionPlayer(owner, companionId, slotIndex);
            if (companionPlayer != null)
            {
                // Remote companions don't need a brain — they receive position updates
                CompanionSlotManager.ClaimSlotDirect(slotIndex, ownerIndex, companionId, null);
            }
        }
    }

    // --- Despawn ---

    public static void SendDespawnCompanionPlayer(int slotIndex)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPlayerPacketType.DespawnCompanionPlayer);
        packet.Write(slotIndex);
        packet.Send();
    }

    private static void ReceiveDespawn(BinaryReader reader, int whoAmI)
    {
        int slotIndex = reader.ReadInt32();

        CompanionSlotManager.ReleaseSlot(slotIndex);

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPlayerPacketType.DespawnCompanionPlayer);
            relay.Write(slotIndex);
            relay.Send(-1, whoAmI);
        }
    }

    // --- Position Sync ---

    public static void SendPosition(int slotIndex, Vector2 position, Vector2 velocity, int direction)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPlayerPacketType.SyncCompanionPosition);
        packet.Write(slotIndex);
        packet.Write(position.X);
        packet.Write(position.Y);
        packet.Write(velocity.X);
        packet.Write(velocity.Y);
        packet.Write(direction);
        packet.Send();
    }

    private static void ReceivePosition(BinaryReader reader, int whoAmI)
    {
        int slotIndex = reader.ReadInt32();
        float posX = reader.ReadSingle();
        float posY = reader.ReadSingle();
        float velX = reader.ReadSingle();
        float velY = reader.ReadSingle();
        int direction = reader.ReadInt32();

        if (CompanionSlotManager.IsCompanionSlot(slotIndex))
        {
            var player = Main.player[slotIndex];
            if (player != null && player.active)
            {
                player.position = new Vector2(posX, posY);
                player.velocity = new Vector2(velX, velY);
                player.direction = direction;
            }
        }

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPlayerPacketType.SyncCompanionPosition);
            relay.Write(slotIndex);
            relay.Write(posX);
            relay.Write(posY);
            relay.Write(velX);
            relay.Write(velY);
            relay.Write(direction);
            relay.Send(-1, whoAmI);
        }
    }

    // --- Equipment Sync ---

    public static void SendEquipment(int slotIndex, int ownerIndex, string companionId)
    {
        var owner = Main.player[ownerIndex];
        var cp = owner?.GetModPlayer<CompanionPlayer>();
        var equip = cp?.GetEquipment(companionId);
        if (equip == null) return;

        var tag = equip.Save();
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPlayerPacketType.SyncCompanionEquipment);
        packet.Write(slotIndex);
        packet.Write(ownerIndex);
        packet.Write(companionId);
        Terraria.ModLoader.IO.TagIO.Write(tag, packet);
        packet.Send();
    }

    private static void ReceiveEquipment(BinaryReader reader, int whoAmI)
    {
        int slotIndex = reader.ReadInt32();
        int ownerIndex = reader.ReadInt32();
        string companionId = reader.ReadString();
        var tag = Terraria.ModLoader.IO.TagIO.Read(reader);

        var owner = Main.player[ownerIndex];
        var cp = owner?.GetModPlayer<CompanionPlayer>();
        if (cp != null)
        {
            var def = CompanionRegistry.GetCompanion(companionId);
            var layout = def?.EquipmentLayout ?? new Equipment.EquipmentSlotLayout();
            cp.CompanionEquipments[companionId] = Equipment.CompanionEquipment.Load(tag, layout);

            // Re-sync equipment to the companion player entity
            if (CompanionSlotManager.IsCompanionSlot(slotIndex))
            {
                var companionPlayer = Main.player[slotIndex];
                if (companionPlayer != null && companionPlayer.active)
                    EquipmentBridge.SyncEquipmentToPlayer(cp.GetEquipment(companionId), companionPlayer);
            }
        }

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPlayerPacketType.SyncCompanionEquipment);
            relay.Write(slotIndex);
            relay.Write(ownerIndex);
            relay.Write(companionId);
            Terraria.ModLoader.IO.TagIO.Write(tag, relay);
            relay.Send(-1, whoAmI);
        }
    }

    // --- Health Sync ---

    public static void SendHealth(int slotIndex, int health, int maxHealth)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPlayerPacketType.SyncCompanionHealth);
        packet.Write(slotIndex);
        packet.Write(health);
        packet.Write(maxHealth);
        packet.Send();
    }

    private static void ReceiveHealth(BinaryReader reader, int whoAmI)
    {
        int slotIndex = reader.ReadInt32();
        int health = reader.ReadInt32();
        int maxHealth = reader.ReadInt32();

        if (CompanionSlotManager.IsCompanionSlot(slotIndex))
        {
            var player = Main.player[slotIndex];
            if (player != null && player.active)
            {
                player.statLife = health;
                player.statLifeMax = maxHealth;
                player.statLifeMax2 = maxHealth;
            }
        }

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPlayerPacketType.SyncCompanionHealth);
            relay.Write(slotIndex);
            relay.Write(health);
            relay.Write(maxHealth);
            relay.Send(-1, whoAmI);
        }
    }

    // --- Order Sync ---

    public static void SendOrder(int slotIndex, CompanionOrder order, Vector2 targetPosition)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPlayerPacketType.SyncCompanionOrder);
        packet.Write(slotIndex);
        packet.Write((byte)order);
        packet.Write(targetPosition.X);
        packet.Write(targetPosition.Y);
        packet.Send();
    }

    private static void ReceiveOrder(BinaryReader reader, int whoAmI)
    {
        int slotIndex = reader.ReadInt32();
        var order = (CompanionOrder)reader.ReadByte();
        float targetX = reader.ReadSingle();
        float targetY = reader.ReadSingle();

        var info = CompanionSlotManager.GetSlotInfo(slotIndex);
        if (info?.Brain != null)
        {
            info.Brain.CurrentOrder = order;
            info.Brain.OrderTargetPosition = new Vector2(targetX, targetY);
        }

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPlayerPacketType.SyncCompanionOrder);
            relay.Write(slotIndex);
            relay.Write((byte)order);
            relay.Write(targetX);
            relay.Write(targetY);
            relay.Send(-1, whoAmI);
        }
    }
}
