using System;
using System.Collections.Generic;

namespace AmongUsExternal
{
    public class PlayerDataModel
    {
        public IntPtr BasePointer { get; set; }
        public byte PlayerId { get; set; }
        public string Name { get; set; }
        public bool IsImpostor { get; set; }
        public bool IsDead { get; set; }
        public byte ColorId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public bool HasPosition { get; set; }
        public bool HasLivePosition { get; set; }
        public bool IsLocal { get; set; }
        public bool IsDummy { get; set; }
        public bool RoleKnown { get; set; }
        public ushort RoleType { get; set; }
        public string RoleName { get; set; }
        public double Distance { get; set; }

        public override string ToString()
        {
            string role = !RoleKnown ? "[BILINMIYOR]" : (IsImpostor ? "[IMPOSTOR]" : "[CREWMATE]");
            string status = IsDead ? "(DEAD)" : "(ALIVE)";
            return string.Format("{0} {1} {2} | Pos: ({3:F1}, {4:F1})", role, Name, status, PositionX, PositionY);
        }
    }

    public class PlayerManager
    {
        private MemoryManager memory;

        public PlayerManager(MemoryManager memory)
        {
            this.memory = memory;
        }

        public List<PlayerDataModel> GetAllPlayers()
        {
            List<PlayerDataModel> players = new List<PlayerDataModel>();
            if (!memory.IsConnected) return players;

            try
            {
                // PlayerControl class pointer -> static fields
                IntPtr staticFields = GameLayout.GetPlayerStatics(memory);
                if (staticFields == IntPtr.Zero) return players;

                IntPtr allPlayersList = memory.ReadPointer(IntPtr.Add(staticFields, 0x08));
                if (allPlayersList == IntPtr.Zero) return players;

                IntPtr itemsArray = memory.ReadPointer(IntPtr.Add(allPlayersList, 0x10));
                int count = memory.ReadInt(IntPtr.Add(allPlayersList, 0x18));
                if (itemsArray == IntPtr.Zero || count < 0 || count > 128) return players;

                for (int i = 0; i < count; i++)
                {
                    IntPtr playerPtr = memory.ReadPointer(IntPtr.Add(itemsArray, 0x20 + (i * IntPtr.Size)));
                    if (playerPtr == IntPtr.Zero) continue;

                    PlayerDataModel player = ParsePlayer(playerPtr);
                    if (player != null)
                    {
                        players.Add(player);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("GetAllPlayers sirasinda hata olustu", ex);
            }

            PlayerDataModel local = players.Find(delegate(PlayerDataModel p) { return p.IsLocal; });
            foreach (PlayerDataModel player in players)
                player.Distance = local != null && local.HasPosition && player.HasPosition
                    ? Math.Sqrt(Math.Pow(player.PositionX - local.PositionX, 2) + Math.Pow(player.PositionY - local.PositionY, 2))
                    : double.NaN;
            return players;
        }

        private IntPtr GetDefaultOutfit(IntPtr data)
        {
            IntPtr outfits = memory.ReadPointer(IntPtr.Add(data, 0x58));
            if (outfits == IntPtr.Zero) return IntPtr.Zero;
            IntPtr entries = memory.ReadPointer(IntPtr.Add(outfits, 0x18));
            int count = memory.ReadInt(IntPtr.Add(outfits, 0x20));
            if (entries == IntPtr.Zero || count < 0 || count > 32) return IntPtr.Zero;
            for (int i = 0; i < count; i++)
            {
                IntPtr entry = IntPtr.Add(entries, 0x20 + i * 24);
                if (memory.ReadInt(entry) >= 0 && memory.ReadInt(IntPtr.Add(entry, 8)) == 0)
                    return memory.ReadPointer(IntPtr.Add(entry, 16));
            }
            return IntPtr.Zero;
        }

        public PlayerDataModel ParsePlayer(IntPtr playerPtr)
        {
            try
            {
                if (GameLayout.ClassName(memory, memory.ReadPointer(playerPtr)) != "PlayerControl") return null;
                byte id = memory.ReadBytes(IntPtr.Add(playerPtr, 0x38), 1)[0];
                IntPtr myDataPtr = memory.ReadPointer(IntPtr.Add(playerPtr, 0x78));

                string name = "Unknown";
                bool isImpostor = false;
                bool isDead = false;
                byte colorId = 0;
                bool roleKnown = false;
                ushort roleType = ushort.MaxValue;

                if (myDataPtr != IntPtr.Zero)
                {
                    IntPtr role = memory.ReadPointer(IntPtr.Add(myDataPtr, 0x68));
                    byte[] roleBytes;
                    byte[] teamBytes;
                    roleKnown = memory.ReadBool(IntPtr.Add(playerPtr, 0x169)) && role != IntPtr.Zero
                        && memory.TryReadBytes(IntPtr.Add(role, 0x20), 2, out roleBytes)
                        && memory.TryReadBytes(IntPtr.Add(role, 0x74), 4, out teamBytes);
                    if (roleKnown)
                    {
                        roleType = BitConverter.ToUInt16(memory.ReadBytes(IntPtr.Add(role, 0x20), 2), 0);
                        int team = memory.ReadInt(IntPtr.Add(role, 0x74));
                        roleKnown = team == 0 || team == 1;
                        isImpostor = team == 1;
                    }
                    isDead = memory.ReadBool(IntPtr.Add(myDataPtr, 0x78));
                    IntPtr outfit = GetDefaultOutfit(myDataPtr);
                    if (outfit != IntPtr.Zero)
                    {
                        name = memory.ReadIl2CppString(memory.ReadPointer(IntPtr.Add(outfit, 0x40)));
                        colorId = (byte)memory.ReadInt(IntPtr.Add(outfit, 0x10));
                    }
                }

                // Get Position from NetTransform
                IntPtr netTransformPtr = memory.ReadPointer(IntPtr.Add(playerPtr, 0xE0));
                float posX = 0f, posY = 0f;
                bool isLocal = playerPtr == GameLayout.GetLocalPlayer(memory);
                bool hasPosition = false;
                bool livePosition = false;
                if (netTransformPtr != IntPtr.Zero)
                {
                    byte[] position;
                    hasPosition = memory.TryReadBytes(IntPtr.Add(netTransformPtr, isLocal ? 0x6C : 0x64), 8, out position);
                    posX = BitConverter.ToSingle(position, 0);
                    posY = BitConverter.ToSingle(position, 4);
                    hasPosition = hasPosition && !float.IsNaN(posX) && !float.IsNaN(posY)
                        && Math.Abs(posX) < 10000 && Math.Abs(posY) < 10000;
                    float liveX, liveY;
                    livePosition = CameraReader.ReadPlayerPosition(memory, netTransformPtr, out liveX, out liveY);
                    if (livePosition) { posX = liveX; posY = liveY; hasPosition = true; }
                }

                PlayerDataModel model = new PlayerDataModel();
                model.BasePointer = playerPtr;
                model.PlayerId = id;
                model.Name = name;
                model.IsImpostor = isImpostor;
                model.IsDead = isDead;
                model.ColorId = colorId;
                model.PositionX = posX;
                model.PositionY = posY;
                model.HasPosition = hasPosition;
                model.HasLivePosition = livePosition;
                model.IsLocal = isLocal;
                model.IsDummy = memory.ReadBool(IntPtr.Add(playerPtr, 0x118));
                model.RoleKnown = roleKnown;
                model.RoleType = roleType;
                model.RoleName = roleKnown ? GetRoleName(roleType) : "Bilinmiyor";
                return model;
            }
            catch (Exception ex)
            {
                Logger.LogError("ParsePlayer hatasi", ex);
                return null;
            }
        }

        public static string GetRoleName(ushort role)
        {
            switch (role)
            {
                case 0: return "Crewmate";
                case 1: return "Impostor";
                case 2: return "Scientist";
                case 3: return "Engineer";
                case 4: return "Guardian Angel";
                case 5: return "Shapeshifter";
                case 6: return "Crewmate Ghost";
                case 7: return "Impostor Ghost";
                case 8: return "Noisemaker";
                case 9: return "Phantom";
                case 10: return "Tracker";
                case 12: return "Detective";
                case 18: return "Viper";
                case 19: return "Judge";
                default: return "Rol " + role;
            }
        }
    }
}
