using System;

namespace AmongUsExternal.Features
{
    public class SpeedFeature
    {
        private readonly MemoryManager memory;
        private IntPtr appliedOptions;
        private float originalSpeed;
        public bool SpeedHackEnabled { get; private set; }
        public float CustomSpeed { get; set; }

        public SpeedFeature(MemoryManager memory)
        {
            this.memory = memory;
            CustomSpeed = 3.5f;
        }

        public bool ToggleSpeedHack()
        {
            if (SpeedHackEnabled) Disable();
            else ApplySpeed(CustomSpeed);
            return SpeedHackEnabled;
        }

        public void ApplySpeed(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0 || multiplier > 10) return;
            IntPtr options = GameLayout.GetOptions(memory);
            if (options == IntPtr.Zero || GameLayout.GetLocalPlayer(memory) == IntPtr.Zero) return;
            float original = memory.ReadFloat(IntPtr.Add(options, 0x20));
            if (!memory.WriteFloat(IntPtr.Add(options, 0x20), multiplier)) return;
            if (!SpeedHackEnabled || appliedOptions != options) originalSpeed = original;
            appliedOptions = options;
            SpeedHackEnabled = true;
        }

        public void Disable()
        {
            if (!SpeedHackEnabled) return;
            if (appliedOptions == GameLayout.GetOptions(memory)
                && !memory.WriteFloat(IntPtr.Add(appliedOptions, 0x20), originalSpeed)) return;
            SpeedHackEnabled = false;
            appliedOptions = IntPtr.Zero;
        }
    }
}
