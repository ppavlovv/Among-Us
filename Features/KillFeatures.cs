using System;

namespace AmongUsExternal.Features
{
    public class KillFeatures
    {
        private readonly MemoryManager memory;
        public bool ZeroKillCooldownEnabled { get; private set; }

        public KillFeatures(MemoryManager memory) { this.memory = memory; }

        public bool ToggleZeroKillCooldown()
        {
            ZeroKillCooldownEnabled = !ZeroKillCooldownEnabled;
            Apply();
            return ZeroKillCooldownEnabled;
        }

        public void Apply()
        {
            if (!ZeroKillCooldownEnabled) return;
            IntPtr player = GameLayout.GetLocalPlayer(memory);
            if (player == IntPtr.Zero || !memory.WriteFloat(IntPtr.Add(player, 0xB8), 0.0f))
                ZeroKillCooldownEnabled = false;
        }
    }
}
