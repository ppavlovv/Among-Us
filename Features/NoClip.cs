using System;

namespace AmongUsExternal.Features
{
    public class NoClipFeature
    {
        private MemoryManager memory;
        public bool IsEnabled { get; private set; }

        public NoClipFeature(MemoryManager memory)
        {
            this.memory = memory;
        }

        public bool Toggle()
        {
            IsEnabled = !IsEnabled;
            Apply();
            return IsEnabled;
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

                IntPtr myPhysics = memory.ReadPointer(IntPtr.Add(localPlayer, Offsets.PlayerControl_MyPhysics));
                if (myPhysics == IntPtr.Zero) return;

                IntPtr collider = memory.ReadPointer(IntPtr.Add(myPhysics, Offsets.PlayerPhysics_Collider));
                if (collider == IntPtr.Zero) return;

                // Disabling Unity Collider2D component allows passing through walls (NoClip)
                memory.WriteBool(IntPtr.Add(collider, Offsets.Collider_Enabled), !IsEnabled);
            }
            catch (Exception ex)
            {
                Logger.LogError("NoClip uygulama hatasi", ex);
            }
        }
    }
}
