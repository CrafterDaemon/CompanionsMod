namespace CompanionsMod.Core.Networking;

public enum CompanionPlayerPacketType : byte
{
    SpawnCompanionPlayer = 20,
    DespawnCompanionPlayer = 21,
    SyncCompanionPosition = 22,
    SyncCompanionEquipment = 23,
    SyncCompanionHealth = 24,
    SyncCompanionOrder = 25
}
