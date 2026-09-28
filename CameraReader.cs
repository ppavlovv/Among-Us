using System;
using System.Drawing;

namespace AmongUsExternal
{
    public class CameraSnapshot
    {
        public float X;
        public float Y;
        public float OrthographicSize;

        public PointF WorldToScreen(float x, float y, Size viewport)
        {
            float scale = viewport.Height / (2 * OrthographicSize);
            return new PointF(viewport.Width / 2f + (x - X) * scale,
                viewport.Height / 2f - (y - Y) * scale);
        }
    }

    public static class CameraReader
    {
        public const string EngineHash = "889D912DA7A6897C469253A810EAA7682D12D0E3F2432F61F879F4152714AF95";

        public static CameraSnapshot Read(MemoryManager memory)
        {
            if (!memory.IsConnected || !memory.IsCompatibleBuild || !memory.IsCompatibleEngine) return null;
            // MethodInfo.klass identifies the closed DestroyableSingleton<HudManager> type.
            IntPtr method = memory.ReadPointer(IntPtr.Add(memory.GameAssemblyBase, 54567896));
            IntPtr singleton = memory.ReadPointer(IntPtr.Add(method, 0x20));
            if (GameLayout.ClassName(memory, singleton) != "DestroyableSingleton`1") return null;
            IntPtr fields = memory.ReadPointer(IntPtr.Add(singleton, 0xB8));
            IntPtr hud = memory.ReadPointer(fields);
            if (GameLayout.ClassName(memory, memory.ReadPointer(hud)) != "HudManager") return null;
            IntPtr follower = memory.ReadPointer(IntPtr.Add(hud, 0x28));
            if (GameLayout.ClassName(memory, memory.ReadPointer(follower)) != "FollowerCamera") return null;
            IntPtr nativeFollower = memory.ReadPointer(IntPtr.Add(follower, 0x10));
            IntPtr gameObject = memory.ReadPointer(IntPtr.Add(nativeFollower, 0x30));
            IntPtr components = memory.ReadPointer(IntPtr.Add(gameObject, 0x30));
            int count = memory.ReadInt(IntPtr.Add(gameObject, 0x40));
            if (components == IntPtr.Zero || count < 1 || count > 64) return null;
            int cameraType = memory.ReadInt(IntPtr.Add(memory.UnityPlayerBase, 0x1BD7370));
            int typeCount = memory.ReadInt(IntPtr.Add(memory.UnityPlayerBase, 0x1BD7374));
            if (typeCount < 1 || typeCount > 16) return null;
            for (int i = 0; i < count; i++)
            {
                IntPtr entry = IntPtr.Add(components, i * 16);
                int type = memory.ReadInt(entry);
                if (type < cameraType || type >= cameraType + typeCount) continue;
                IntPtr camera = memory.ReadPointer(IntPtr.Add(entry, 8));
                float size = memory.ReadFloat(IntPtr.Add(camera, 0x460));
                byte[] center;
                if (camera == IntPtr.Zero || size < 0.1f || size > 100 || float.IsNaN(size)
                    || !memory.TryReadBytes(IntPtr.Add(follower, 0x40), 8, out center)) return null;
                float x = BitConverter.ToSingle(center, 0), y = BitConverter.ToSingle(center, 4);
                if (float.IsNaN(x) || float.IsNaN(y) || Math.Abs(x) > 10000 || Math.Abs(y) > 10000) return null;
                return new CameraSnapshot { X = x, Y = y, OrthographicSize = size };
            }
            return null;
        }

        public static bool ReadPlayerPosition(MemoryManager memory, IntPtr netTransform, out float x, out float y)
        {
            x = y = 0;
            if (!memory.IsCompatibleEngine) return false;
            IntPtr rigidbody = memory.ReadPointer(IntPtr.Add(netTransform, 0x40));
            if (GameLayout.ClassName(memory, memory.ReadPointer(rigidbody)) != "Rigidbody2D") return false;
            IntPtr native = memory.ReadPointer(IntPtr.Add(rigidbody, 0x10));
            IntPtr physicsBody = memory.ReadPointer(IntPtr.Add(native, 0x80));
            byte[] coordinates;
            // UnityPlayer's Rigidbody2D position getter reads the physics body's x/y at 0xC.
            if (physicsBody == IntPtr.Zero || !memory.TryReadBytes(IntPtr.Add(physicsBody, 0x0C), 8, out coordinates)) return false;
            x = BitConverter.ToSingle(coordinates, 0); y = BitConverter.ToSingle(coordinates, 4);
            return !float.IsNaN(x) && !float.IsNaN(y) && Math.Abs(x) < 10000 && Math.Abs(y) < 10000;
        }
    }
}
