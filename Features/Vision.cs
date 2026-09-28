using System;

namespace AmongUsExternal.Features
{
    public class VisionFeature
    {
        private readonly MemoryManager memory;
        private IntPtr appliedOptions;
        private float originalCrew;
        private float originalImpostor;
        public bool MaxVisionEnabled { get; private set; }

        public VisionFeature(MemoryManager memory) { this.memory = memory; }

        public bool ToggleMaxVision()
        {
            if (MaxVisionEnabled) Disable();
            else ApplyVisionMultiplier(10.0f);
            return MaxVisionEnabled;
        }

        public void ApplyVisionMultiplier(float multiplier)
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0 || multiplier > 10) return;
            IntPtr options = GameLayout.GetOptions(memory);
            if (options == IntPtr.Zero || GameLayout.GetLocalPlayer(memory) == IntPtr.Zero) return;
            float crew = memory.ReadFloat(IntPtr.Add(options, 0x24));
            float impostor = memory.ReadFloat(IntPtr.Add(options, 0x28));
            if (!memory.WriteFloat(IntPtr.Add(options, 0x24), multiplier)) return;
            if (!memory.WriteFloat(IntPtr.Add(options, 0x28), multiplier))
            {
                memory.WriteFloat(IntPtr.Add(options, 0x24), crew);
                return;
            }
            if (!MaxVisionEnabled || appliedOptions != options)
            {
                originalCrew = crew;
                originalImpostor = impostor;
            }
            appliedOptions = options;
            MaxVisionEnabled = true;
        }

        public void Disable()
        {
            if (!MaxVisionEnabled) return;
            if (appliedOptions == GameLayout.GetOptions(memory))
            {
                bool crew = memory.WriteFloat(IntPtr.Add(appliedOptions, 0x24), originalCrew);
                bool impostor = memory.WriteFloat(IntPtr.Add(appliedOptions, 0x28), originalImpostor);
                if (!crew || !impostor) return;
            }
            MaxVisionEnabled = false;
            appliedOptions = IntPtr.Zero;
        }
    }
}
