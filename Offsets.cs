using System;

namespace AmongUsExternal
{
    /// <summary>
    /// Game Assembly Offsets for Among Us (Il2Cpp / GameAssembly.dll)
    /// Centralized offsets for memory modification.
    /// </summary>
    public static class Offsets
    {
        // Base Module Name
        public const string GameModuleName = "GameAssembly.dll";

        // Main Class Pointer Offsets (Relative to GameAssembly.dll)
        public static IntPtr PlayerControl_Class = new IntPtr(0x02C7C3B4); 
        public static IntPtr GameOptionsData_Class = new IntPtr(0x02C7C410); 
        public static IntPtr AmongUsClient_Class = new IntPtr(0x02C7C298); 

        // PlayerControl Fields
        public static int PlayerControl_LocalPlayer = 0x5C;  // Static instance of local player
        public static int PlayerControl_AllPlayers = 0x60;   // System.Collections.Generic.List<PlayerControl>
        public static int PlayerControl_PlayerId = 0x08;     // byte
        public static int PlayerControl_MyData = 0x34;       // PlayerData struct pointer
        public static int PlayerControl_MyPhysics = 0x38;    // PlayerPhysics component pointer
        public static int PlayerControl_NetTransform = 0x3C; // CustomNetworkTransform component pointer
        public static int PlayerControl_KillTimer = 0x44;    // float (Kill cooldown timer)
        public static int PlayerControl_InVent = 0x48;       // bool (IsInVent flag)

        // PlayerData Fields (MyData)
        public static int PlayerData_PlayerName = 0x0C;      // String pointer
        public static int PlayerData_ColorId = 0x10;         // byte
        public static int PlayerData_HatId = 0x14;           // uint
        public static int PlayerData_PetId = 0x18;           // uint
        public static int PlayerData_SkinId = 0x1C;          // uint
        public static int PlayerData_IsDead = 0x29;          // bool (byte)
        public static int PlayerData_IsImpostor = 0x28;      // bool (byte)

        // PlayerPhysics Fields (MyPhysics)
        public static int PlayerPhysics_Speed = 0x14;        // float (Base movement speed)
        public static int PlayerPhysics_Collider = 0x18;     // UnityEngine.Collider2D pointer
        public static int Collider_Enabled = 0x18;           // bool (byte) disable for noclip

        // NetworkTransform Fields (Position)
        public static int NetTransform_PrevPos = 0x2C;       // Vector2 (float x, float y)
        public static int NetTransform_TargetPos = 0x34;     // Vector2 (float x, float y)

        // GameOptions Data / Vision & Speed Offsets
        public static int GameOptions_PlayerSpeedMod = 0x10; // float (Movement speed multiplier)
        public static int GameOptions_CrewLightMod = 0x14;   // float (default 1.0f)
        public static int GameOptions_ImpostorLightMod = 0x18;// float (default 1.5f)
        public static int GameOptions_KillCooldown = 0x20;   // float (Kill cooldown setting)
        public static int GameOptions_KillDistance = 0x24;   // int (0: Short, 1: Medium, 2: Long)
        public static int GameOptions_NumImpostors = 0x08;   // int
    }
}
