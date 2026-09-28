using System;
using System.Collections.Generic;
using System.IO;

namespace KV3Bridge
{
    public enum BridgeCommand : byte
    {
        Snapshot = 0,
        NoClip = 1,
        Protection = 2,
        ReviveSelf = 3,
        TestDeath = 4,
        KillSelected = 5,
        PullSelected = 6,
        PullAll = 7,
        ReviveAll = 8,
        Orbit = 9,
        Shuffle = 10,
        KillAll = 11,
        Cancel = 12
    }

    public sealed class BridgeTarget
    {
        public byte Id;
        public string Name;
        public bool Dead;

        public override string ToString()
        {
            return "[" + Id + "] " + Name + (Dead ? " [olu]" : " [hayatta]");
        }
    }

    public sealed class BridgeSnapshot
    {
        public bool Connected;
        public bool Freeplay;
        public bool NoClipAvailable;
        public bool NoClip;
        public bool Protection;
        public bool Orbit;
        public string Status = "";
        public List<BridgeTarget> Targets = new List<BridgeTarget>();
    }

    public static class BridgeProtocol
    {
        public static string PipeName(int processId)
        {
            return "KV3.Freeplay." + processId;
        }

        public static void WriteRequest(BinaryWriter writer, BridgeCommand command, byte argument)
        {
            writer.Write((byte)command);
            writer.Write(argument);
            writer.Flush();
        }

        public static BridgeCommand ReadRequest(BinaryReader reader, out byte argument)
        {
            BridgeCommand command = (BridgeCommand)reader.ReadByte();
            argument = reader.ReadByte();
            return command;
        }

        public static void WriteResponse(BinaryWriter writer, BridgeSnapshot snapshot)
        {
            writer.Write(snapshot.Freeplay);
            writer.Write(snapshot.NoClipAvailable);
            writer.Write(snapshot.NoClip);
            writer.Write(snapshot.Protection);
            writer.Write(snapshot.Orbit);
            writer.Write(snapshot.Status ?? "");
            writer.Write(snapshot.Targets.Count);
            foreach (BridgeTarget target in snapshot.Targets)
            {
                writer.Write(target.Id);
                writer.Write(target.Name ?? "");
                writer.Write(target.Dead);
            }
            writer.Flush();
        }

        public static BridgeSnapshot ReadResponse(BinaryReader reader)
        {
            var snapshot = new BridgeSnapshot();
            snapshot.Freeplay = reader.ReadBoolean();
            snapshot.NoClipAvailable = reader.ReadBoolean();
            snapshot.NoClip = reader.ReadBoolean();
            snapshot.Protection = reader.ReadBoolean();
            snapshot.Orbit = reader.ReadBoolean();
            snapshot.Status = reader.ReadString();
            int count = reader.ReadInt32();
            if (count < 0 || count > 32) throw new InvalidDataException("Invalid target count");
            for (int i = 0; i < count; i++)
            {
                snapshot.Targets.Add(new BridgeTarget {
                    Id = reader.ReadByte(), Name = reader.ReadString(), Dead = reader.ReadBoolean()
                });
            }
            snapshot.Connected = true;
            return snapshot;
        }
    }
}
