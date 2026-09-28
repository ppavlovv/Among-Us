using System;
using System.Collections.Generic;

namespace AmongUsExternal.Features
{
    /// <summary>
    /// Teleport özelliği - Herhangi bir oyuncuya veya konuma ışınlanma
    /// </summary>
    public class TeleportFeature
    {
        private MemoryManager memory;
        private PlayerManager playerManager;

        public TeleportFeature(MemoryManager memory, PlayerManager playerManager)
        {
            this.memory = memory;
            this.playerManager = playerManager;
        }

        /// <summary>
        /// Local oyuncuyu belirtilen koordinatlara ışınla
        /// </summary>
        public bool TeleportTo(float x, float y)
        {
            if (!memory.IsConnected) return false;

            try
            {
                IntPtr localPlayer = GetLocalPlayer();
                if (localPlayer == IntPtr.Zero) return false;

                IntPtr netTransform = memory.ReadPointer(IntPtr.Add(localPlayer, Offsets.PlayerControl_NetTransform));
                if (netTransform == IntPtr.Zero) return false;

                // Hem PrevPos hem TargetPos'u güncelle (desync önlemek için)
                memory.WriteFloat(IntPtr.Add(netTransform, Offsets.NetTransform_PrevPos), x);
                memory.WriteFloat(IntPtr.Add(netTransform, Offsets.NetTransform_PrevPos + 0x04), y);
                memory.WriteFloat(IntPtr.Add(netTransform, Offsets.NetTransform_TargetPos), x);
                memory.WriteFloat(IntPtr.Add(netTransform, Offsets.NetTransform_TargetPos + 0x04), y);

                Logger.LogInfo(string.Format("Teleport basarili: ({0:F2}, {1:F2})", x, y));
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Teleport hatasi", ex);
                return false;
            }
        }

        /// <summary>
        /// Belirtilen oyuncunun yanına ışınlan
        /// </summary>
        public bool TeleportToPlayer(byte playerId)
        {
            List<PlayerDataModel> players = playerManager.GetAllPlayers();
            foreach (PlayerDataModel p in players)
            {
                if (p.PlayerId == playerId)
                {
                    return TeleportTo(p.PositionX + 0.5f, p.PositionY);
                }
            }
            return false;
        }

        /// <summary>
        /// Impostor'a ışınlan (kill için)
        /// </summary>
        public bool TeleportToNearestImpostor()
        {
            List<PlayerDataModel> players = playerManager.GetAllPlayers();
            foreach (PlayerDataModel p in players)
            {
                if (p.IsImpostor && !p.IsDead)
                {
                    return TeleportTo(p.PositionX + 0.5f, p.PositionY);
                }
            }
            return false;
        }

        /// <summary>
        /// Önceden tanımlanmış konumlara ışınla
        /// </summary>
        public bool TeleportToLocation(string locationName)
        {
            // Skeld haritasının önemli noktaları
            switch (locationName.ToLower())
            {
                case "admin":     return TeleportTo(4.5f, -8.0f);
                case "cafeteria": return TeleportTo(-1.0f, 3.0f);
                case "weapons":   return TeleportTo(8.7f, 1.7f);
                case "o2":        return TeleportTo(6.4f, -3.4f);
                case "navigation": return TeleportTo(16.4f, -3.7f);
                case "shields":   return TeleportTo(9.0f, -8.5f);
                case "comms":     return TeleportTo(3.7f, -15.5f);
                case "storage":   return TeleportTo(-1.6f, -15.0f);
                case "electrical": return TeleportTo(-8.5f, -8.2f);
                case "lower":     return TeleportTo(-10.5f, -3.2f);
                case "security":  return TeleportTo(-13.2f, -3.9f);
                case "reactor":   return TeleportTo(-20.4f, -5.5f);
                case "upper":     return TeleportTo(-9.7f, 3.1f);
                case "medbay":    return TeleportTo(-8.2f, -3.5f);
                default: return false;
            }
        }

        private IntPtr GetLocalPlayer()
        {
            IntPtr playerControlClass = memory.ReadPointer(IntPtr.Add(memory.GameAssemblyBase, (int)Offsets.PlayerControl_Class));
            if (playerControlClass == IntPtr.Zero) return IntPtr.Zero;

            IntPtr staticFields = memory.ReadPointer(IntPtr.Add(playerControlClass, 0x5C));
            if (staticFields == IntPtr.Zero) return IntPtr.Zero;

            return memory.ReadPointer(IntPtr.Add(staticFields, Offsets.PlayerControl_LocalPlayer));
        }
    }
}
