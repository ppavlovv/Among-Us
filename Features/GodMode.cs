using System;

namespace AmongUsExternal.Features
{
    public class GodModeFeature
    {
        private MemoryManager memory;
        public bool IsGodModeEnabled { get; private set; }

        public GodModeFeature(MemoryManager memory)
        {
            this.memory = memory;
        }

        public bool ToggleGodMode()
        {
            IsGodModeEnabled = !IsGodModeEnabled;
            Apply();
            return IsGodModeEnabled;
        }

        public void Apply()
        {
            if (!memory.IsConnected || !IsGodModeEnabled) return;

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

                // Force IsDead byte to 0 (False) to prevent client death state
                memory.WriteBool(IntPtr.Add(myData, Offsets.PlayerData_IsDead), false);
            }
            catch (Exception ex)
            {
                Logger.LogError("GodMode uygulama hatasi", ex);
            }
        }
    }
}
