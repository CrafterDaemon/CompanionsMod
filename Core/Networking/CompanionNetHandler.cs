using System.IO;
using CompanionsMod.Core.Quests;
using Terraria;
using Terraria.ID;

namespace CompanionsMod.Core.Networking;

public static class CompanionNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var packetType = (CompanionPacketType)reader.ReadByte();
        HandlePacketWithType(packetType, reader, whoAmI);
    }

    public static void HandlePacketWithType(CompanionPacketType packetType, BinaryReader reader, int whoAmI)
    {
        switch (packetType)
        {
            case CompanionPacketType.SyncSpawn:
                ReceiveSpawn(reader, whoAmI);
                break;
            case CompanionPacketType.SyncDespawn:
                ReceiveDespawn(reader, whoAmI);
                break;
            case CompanionPacketType.SyncEquipment:
                ReceiveEquipmentSync(reader, whoAmI);
                break;
            case CompanionPacketType.SyncHealth:
                ReceiveHealthSync(reader, whoAmI);
                break;
            case CompanionPacketType.SyncQuestState:
                ReceiveQuestSync(reader, whoAmI);
                break;
            case CompanionPacketType.SyncRecruit:
                ReceiveRecruit(reader, whoAmI);
                break;
            case CompanionPacketType.SyncRespawn:
                ReceiveRespawn(reader, whoAmI);
                break;
            case CompanionPacketType.FullSync:
                ReceiveFullSync(reader, whoAmI);
                break;
        }
    }

    public static void SendSpawn(int playerIndex, string companionId)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPacketType.SyncSpawn);
        packet.Write(playerIndex);
        packet.Write(companionId);
        packet.Send();
    }

    private static void ReceiveSpawn(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();
        string companionId = reader.ReadString();

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();

        if (Main.netMode == NetmodeID.Server)
        {
            companionPlayer.TrySummon(companionId);

            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPacketType.SyncSpawn);
            relay.Write(playerIndex);
            relay.Write(companionId);
            relay.Send(-1, whoAmI);
        }
        else
        {
            companionPlayer.ActiveCompanionId = companionId;
        }
    }

    public static void SendDespawn(int playerIndex)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPacketType.SyncDespawn);
        packet.Write(playerIndex);
        packet.Send();
    }

    private static void ReceiveDespawn(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();

        if (Main.netMode == NetmodeID.Server)
        {
            companionPlayer.Dismiss();

            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPacketType.SyncDespawn);
            relay.Write(playerIndex);
            relay.Send(-1, whoAmI);
        }
        else
        {
            companionPlayer.Dismiss();
        }
    }

    public static void SendEquipmentSync(int playerIndex, string companionId)
    {
        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();
        var equip = companionPlayer.GetEquipment(companionId);
        var tag = equip.Save();

        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPacketType.SyncEquipment);
        packet.Write(playerIndex);
        packet.Write(companionId);
        Terraria.ModLoader.IO.TagIO.Write(tag, packet);
        packet.Send();
    }

    private static void ReceiveEquipmentSync(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();
        string companionId = reader.ReadString();
        var tag = Terraria.ModLoader.IO.TagIO.Read(reader);

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();
        var def = CompanionRegistry.GetCompanion(companionId);
        var layout = def?.EquipmentLayout ?? new Equipment.EquipmentSlotLayout();
        companionPlayer.CompanionEquipments[companionId] = Equipment.CompanionEquipment.Load(tag, layout);

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPacketType.SyncEquipment);
            relay.Write(playerIndex);
            relay.Write(companionId);
            Terraria.ModLoader.IO.TagIO.Write(tag, relay);
            relay.Send(-1, whoAmI);
        }
    }

    public static void SendHealthSync(int playerIndex, string companionId, int health)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPacketType.SyncHealth);
        packet.Write(playerIndex);
        packet.Write(companionId);
        packet.Write(health);
        packet.Send();
    }

    private static void ReceiveHealthSync(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();
        string companionId = reader.ReadString();
        int health = reader.ReadInt32();

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();
        companionPlayer.CompanionHealths[companionId] = health;

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPacketType.SyncHealth);
            relay.Write(playerIndex);
            relay.Write(companionId);
            relay.Write(health);
            relay.Send(-1, whoAmI);
        }
    }

    public static void SendQuestSync(int playerIndex, string questId, QuestProgress progress)
    {
        var tag = progress.Save();

        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPacketType.SyncQuestState);
        packet.Write(playerIndex);
        packet.Write(questId);
        Terraria.ModLoader.IO.TagIO.Write(tag, packet);
        packet.Send();
    }

    private static void ReceiveQuestSync(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();
        string questId = reader.ReadString();
        var tag = Terraria.ModLoader.IO.TagIO.Read(reader);

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();
        companionPlayer.QuestStates[questId] = QuestProgress.Load(tag);

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPacketType.SyncQuestState);
            relay.Write(playerIndex);
            relay.Write(questId);
            Terraria.ModLoader.IO.TagIO.Write(tag, relay);
            relay.Send(-1, whoAmI);
        }
    }

    public static void SendRecruit(int playerIndex, string companionId)
    {
        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPacketType.SyncRecruit);
        packet.Write(playerIndex);
        packet.Write(companionId);
        packet.Send();
    }

    private static void ReceiveRecruit(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();
        string companionId = reader.ReadString();

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();
        companionPlayer.UnlockedCompanions.Add(companionId);

        if (Main.netMode == NetmodeID.Server)
        {
            var relay = CompanionsMod.Instance.GetPacket();
            relay.Write((byte)CompanionPacketType.SyncRecruit);
            relay.Write(playerIndex);
            relay.Write(companionId);
            relay.Send(-1, whoAmI);
        }
    }

    private static void ReceiveRespawn(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();
        string companionId = reader.ReadString();
        int delay = reader.ReadInt32();

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();
        companionPlayer.DeadCompanionId = companionId;
        companionPlayer.RespawnTimer = delay;
    }

    public static void SendFullSync(int playerIndex, int toWho)
    {
        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();

        var packet = CompanionsMod.Instance.GetPacket();
        packet.Write((byte)CompanionPacketType.FullSync);
        packet.Write(playerIndex);

        // Unlocked companions
        packet.Write(companionPlayer.UnlockedCompanions.Count);
        foreach (var id in companionPlayer.UnlockedCompanions)
            packet.Write(id);

        // Active companion
        packet.Write(companionPlayer.ActiveCompanionId ?? "");

        if (toWho == -1)
            packet.Send();
        else
            packet.Send(toWho);
    }

    private static void ReceiveFullSync(BinaryReader reader, int whoAmI)
    {
        int playerIndex = reader.ReadInt32();

        var player = Main.player[playerIndex];
        var companionPlayer = player.GetModPlayer<CompanionPlayer>();

        int unlockedCount = reader.ReadInt32();
        companionPlayer.UnlockedCompanions.Clear();
        for (int i = 0; i < unlockedCount; i++)
            companionPlayer.UnlockedCompanions.Add(reader.ReadString());

        string activeId = reader.ReadString();
        companionPlayer.ActiveCompanionId = activeId == "" ? null : activeId;
    }
}
