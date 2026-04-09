using CompanionsMod.Core.Networking;
using CompanionsMod.Core.PlayerSlot;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace CompanionsMod;

public class CompanionsMod : Mod
{
    public static CompanionsMod Instance { get; private set; }

    public override void Load()
    {
        Instance = this;
        IL_Player.ItemCheck_Shoot += (ILContext il) =>
        {
            var cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(
                i => i.MatchLdfld<Entity>(nameof(Entity.whoAmI)),
                i => i.MatchLdsfld<Main>(nameof(Main.myPlayer)),
                i => i.OpCode == Mono.Cecil.Cil.OpCodes.Beq_S))
            {
                Logger.Warn("ItemCheck_Shoot: gate not found");
                return;
            }

            // After TryGotoNext, cursor is before the first matched instruction.
            // The beq.s is 2 instructions ahead.
            var skipLabel = (ILLabel)cursor.Instrs[cursor.Index + 2].Operand;

            cursor.Index += 3; // move past all 3 matched instructions, into the non-local path

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(
                player => CompanionSlotManager.IsCompanionSlot(player.whoAmI));
            cursor.Emit(OpCodes.Brtrue, skipLabel);
        };
        IL_Player.ItemCheck_OwnerOnlyCode += (ILContext il) =>
        {
            var cursor = new ILCursor(il);

            // Skip past the first gate
            cursor.TryGotoNext(
                i => i.MatchLdfld<Entity>(nameof(Entity.whoAmI)),
                i => i.MatchLdsfld<Main>(nameof(Main.myPlayer)),
                i => i.OpCode == Mono.Cecil.Cil.OpCodes.Beq_S,
                i => i.MatchRet());

            if (!cursor.TryGotoNext(
                i => i.MatchLdsfld<Main>(nameof(Main.myPlayer)),
                i => i.MatchLdarg0(),
                i => i.MatchLdfld<Entity>(nameof(Entity.whoAmI)),
                i => i.OpCode == Mono.Cecil.Cil.OpCodes.Bne_Un_S))
            {
                Logger.Warn("second gate not found");
                return;
            }

            // cursor is sitting before ldsfld myPlayer — the stack is clean here.
            // Grab where bne.un.s would branch to (the skip target)
            var skipLabel = (ILLabel)cursor.Instrs[cursor.Index + 3].Operand;

            // Insert BEFORE the gate: if companion, skip the gate entirely
            // and fall through to the melee code below
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, bool>>(
                player => CompanionSlotManager.IsCompanionSlot(player.whoAmI));

            // If NOT a companion, jump over our inserted instructions to the original gate
            // If IS a companion, fall through past the gate to the melee code
            var gateLabel = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brfalse, gateLabel);

            // companion path: jump past the entire gate to the melee code
            // We need a label on the instruction after bne.un.s
            var meleeLabel = cursor.DefineLabel();
            cursor.Emit(OpCodes.Br, meleeLabel);

            // non-companion path: original gate runs normally
            cursor.MarkLabel(gateLabel);

            // advance past the 4 gate instructions to mark the melee entry point
            cursor.Index += 4; // now just after bne.un.s
            cursor.MarkLabel(meleeLabel);
        };
    }

    public override void Unload()
    {
        Instance = null;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        // Peek at the packet type byte to determine which handler to use.
        // Legacy NPC packets use values 0-19, new player packets use 20+.
        byte peekType = reader.ReadByte();

        if (peekType >= 20)
        {
            // New player-based companion packets — put the byte back by creating
            // a new reader that includes it. Instead, we restructure: the handlers
            // expect to read the packet type themselves, so we pass the already-read byte.
            CompanionPlayerNetHandler.HandlePacketWithType((CompanionPlayerPacketType)peekType, reader, whoAmI);
        }
        else
        {
            CompanionNetHandler.HandlePacketWithType((CompanionPacketType)peekType, reader, whoAmI);
        }
    }

    public override object Call(params object[] args)
    {
        return Core.API.CompanionCallHandler.HandleCall(args);
    }
}
