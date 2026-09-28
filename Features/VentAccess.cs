using System;

namespace AmongUsExternal.Features
{
    public class VentAccessFeature
    {
        private MemoryManager memory;
        public bool VentAccessEnabled { get; private set; }

        public VentAccessFeature(MemoryManager memory)
        {
            this.memory = memory;
        }

        public bool ToggleVentAccess()
        {
            VentAccessEnabled = !VentAccessEnabled;
            Apply();
            return VentAccessEnabled;
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

                // Setting IsImpostor to true enables vent movement and vent interaction buttons on client
                if (VentAccessEnabled)
                {
                    memory.WriteBool(IntPtr.Add(myData, Offsets.PlayerData_IsImpostor), true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("VentAccess uygulama hatasi", ex);
            }
        }
    }
}
