using System;

namespace AmongUsExternal.Features
{
    public class ImpostorFeature
    {
        private MemoryManager memory;
        public bool LocalImpostorRole { get; private set; }

        public ImpostorFeature(MemoryManager memory)
        {
            this.memory = memory;
        }

        public bool ToggleLocalImpostor()
        {
            LocalImpostorRole = !LocalImpostorRole;
            Apply();
            return LocalImpostorRole;
        }

        public void Apply()
        {
            if (!memory.IsConnected) return;

            try
            {
                IntPtr playerControlClass = memory.ReadPointer(IntPtr.Add(memory.GameAssemblyBase, (int)Offsets.PlayerControl_Class));
                if (playerControlClass == IntPtr.Zero) return;

                IntPtr staticFields = memory.ReadPointer(IntPtr.Add(playerControlClass, 0x5C));
                if (staticFields == IntPtr.Zero) return;

                IntPtr localPlayer = memory.ReadPointer(IntPtr.Add(staticFields, Offsets.PlayerControl_LocalPlayer));
                if (localPlayer == IntPtr.Zero) return;

                IntPtr myData = memory.ReadPointer(IntPtr.Add(localPlayer, Offsets.PlayerControl_MyData));
                if (myData == IntPtr.Zero) return;

                // Set IsImpostor byte flag on Local Player Data
                memory.WriteBool(IntPtr.Add(myData, Offsets.PlayerData_IsImpostor), LocalImpostorRole);
            }
            catch (Exception ex)
            {
                Logger.LogError("Impostor rol uygulama hatasi", ex);
            }
        }
    }
}
