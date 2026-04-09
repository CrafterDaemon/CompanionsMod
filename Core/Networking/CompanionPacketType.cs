namespace CompanionsMod.Core.Networking;

public enum CompanionPacketType : byte
{
    SyncSpawn,
    SyncDespawn,
    SyncEquipment,
    SyncHealth,
    SyncQuestState,
    SyncRecruit,
    SyncRespawn,
    FullSync
}
