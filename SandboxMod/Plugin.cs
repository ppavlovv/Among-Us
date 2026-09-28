using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using KV3Bridge;
using System.Security.Cryptography;
using UnityEngine;

namespace KV3.Sandbox;

[BepInPlugin(Id, "KV3 Among Us Tools by ytpavlov", "0.3.0")]
public sealed class Plugin : BasePlugin
{
    public const string Id = "com.ytpavlov.kv3";
    internal static ManualLogSource Logger;
    internal static bool Compatible;
    private Harmony harmony;
    private SandboxPanel panel;
    private BridgeServer bridge;

    public override void Load()
    {
        Logger = Log;
        using var stream = File.OpenRead(Path.Combine(Paths.GameRootPath, "GameAssembly.dll"));
        using var hash = SHA256.Create();
        Compatible = Convert.ToHexString(hash.ComputeHash(stream)) ==
            "3A86BF7D6E69594809D9BA9965F9B5BC4EB88934FDE1C05DBEC191899D1763DF";
        if (!Compatible) { Log.LogError("Unsupported game build; no hooks installed."); return; }
        harmony = new Harmony(Id);
        try
        {
            harmony.PatchAll(typeof(Plugin).Assembly);
            panel = AddComponent<SandboxPanel>();
            bridge = new BridgeServer();
            panel.Bridge = bridge;
            bridge.Start();
            Log.LogInfo("KV3 loaded. F6 and INSERT share tools. Online only permits local NoClip.");
        }
        catch
        {
            bridge?.Dispose();
            harmony.UnpatchSelf();
            Compatible = false;
            throw;
        }
    }

    public override bool Unload()
    {
        bridge?.Dispose();
        if (panel) { panel.ResetSession(); UnityEngine.Object.Destroy(panel); }
        harmony?.UnpatchSelf();
        Compatible = false;
        return true;
    }
}

public sealed class SandboxPanel : MonoBehaviour
{
    public SandboxPanel(IntPtr pointer) : base(pointer) { }
    internal static SandboxPanel Active;
    internal BridgeServer Bridge;
    private bool open;
    private bool noClip;
    private bool immortal;
    private bool orbit;
    private float orbitStarted;
    private float nextOrbit;
    private readonly Dictionary<byte, Vector2> orbitOrigins = new();
    private Collider2D savedCollider;
    private PlayerControl colliderOwner;
    private bool colliderEnabled;
    private bool colliderCheckReported;
    private IntPtr sessionPlayer;
    private bool sessionWasFreeplay;
    private byte? selected;
    private readonly Queue<byte> killQueue = new();
    private readonly HashSet<byte> pendingKills = new();
    private float nextKill;
    private float confirmUntil;
    private string status = "Freeplay bekleniyor";
    private List<PlayerControl> targets = new();
    private float nextRefresh;
    private int page;

    internal static bool SessionAllowed
    {
        get
        {
            var client = AmongUsClient.Instance;
            var local = PlayerControl.LocalPlayer;
            return SandboxPolicy.IsSessionAllowed(Plugin.Compatible,
                client ? (int)client.NetworkMode : -1, local && local.Data != null);
        }
    }

    private static bool ActionsAllowed
    {
        get
        {
            var local = PlayerControl.LocalPlayer;
            return SandboxPolicy.CanAct(SessionAllowed, MeetingHud.Instance != null,
                local && (local.inVent || local.onLadder || local.inMovingPlat || local.isKilling));
        }
    }

    private static bool OnlineNoClipAllowed
    {
        get
        {
            var client = AmongUsClient.Instance;
            var local = PlayerControl.LocalPlayer;
            return SandboxPolicy.CanUseOnlineNoClip(Plugin.Compatible,
                client ? (int)client.NetworkMode : -1, local && local.Data != null,
                MeetingHud.Instance != null,
                local && (local.inVent || local.onLadder || local.inMovingPlat || local.isKilling));
        }
    }

    private static bool NoClipAllowed => ActionsAllowed || OnlineNoClipAllowed;

    internal static bool Protect(PlayerControl player)
    {
        return Active && Active.immortal && SessionAllowed && player &&
            PlayerControl.LocalPlayer && player.PlayerId == PlayerControl.LocalPlayer.PlayerId;
    }

    public void Awake() { Active = this; }

    public void Update()
    {
        try
        {
            Bridge?.Pump(this);
            if (Input.GetKeyDown(KeyCode.F6)) open = !open;
            if (!SessionAllowed)
            {
                var onlineLocal = PlayerControl.LocalPlayer;
                if (!OnlineNoClipAllowed)
                {
                    if (sessionPlayer != IntPtr.Zero) ResetSession();
                    status = "Freeplay islemleri kapali";
                    return;
                }
                if (sessionWasFreeplay || sessionPlayer != onlineLocal.Pointer)
                {
                    ResetSession();
                    sessionPlayer = onlineLocal.Pointer;
                    status = "Yalnizca yerel No-clip kullanilabilir";
                }
                ApplyNoClipCollider(onlineLocal);
                return;
            }
            var local = PlayerControl.LocalPlayer;
            if (!sessionWasFreeplay || sessionPlayer != local.Pointer)
            {
                ResetSession();
                sessionPlayer = local.Pointer;
                sessionWasFreeplay = true;
                status = "Freeplay hazir";
            }
            ApplyNoClipCollider(local);
            if (Time.unscaledTime >= nextRefresh)
            {
                targets = ReadTargets(false);
                if (selected.HasValue && !targets.Exists(p => p.PlayerId == selected.Value)) selected = null;
                page = Math.Clamp(page, 0, Math.Max(0, (targets.Count - 1) / 6));
                nextRefresh = Time.unscaledTime + 0.25f;
            }
            if (orbit && ActionsAllowed && Time.unscaledTime >= nextOrbit)
            {
                var dummies = ReadTargets(false);
                var center = local.GetTruePosition();
                float phase = (Time.unscaledTime - orbitStarted) * 1.2f;
                for (int i = 0; i < dummies.Count; i++)
                {
                    if (!dummies[i].NetTransform) continue;
                    float angle = phase + 2 * MathF.PI * i / Math.Max(1, dummies.Count);
                    dummies[i].NetTransform.SnapTo(center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 2.3f);
                }
                nextOrbit = Time.unscaledTime + 0.2f;
            }
            if (killQueue.Count > 0 && Time.unscaledTime >= nextKill)
            {
                if (!ActionsAllowed)
                {
                    if (MeetingHud.Instance) { ClearQueue(); status = "Islem iptal: toplanti"; }
                    return;
                }
                var id = killQueue.Dequeue();
                pendingKills.Remove(id);
                var target = ReadTargets(true).Find(p => p.PlayerId == id);
                if (target) Kill(target);
                nextKill = Time.unscaledTime + 0.8f;
            }
        }
        catch (Exception error)
        {
            ResetSession();
            status = "Islem durduruldu: " + error.GetType().Name;
            Plugin.Logger.LogError(error);
        }
    }

    private static List<PlayerControl> ReadTargets(bool killing)
    {
        var result = new List<PlayerControl>();
        if (!SessionAllowed || PlayerControl.AllPlayerControls == null) return result;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (!player || player.Data == null) continue;
            if (SandboxPolicy.CanTarget(SessionAllowed, player.PlayerId == PlayerControl.LocalPlayer.PlayerId,
                player.isDummy, player.Data.Disconnected, player.Data.IsDead, killing)) result.Add(player);
        }
        return result;
    }

    private void RestoreCollider()
    {
        try
        {
            if (savedCollider)
                savedCollider.enabled = colliderOwner && colliderOwner.Data != null && colliderOwner.Data.IsDead
                    ? false : colliderEnabled;
        }
        catch (Exception error) { Plugin.Logger.LogWarning("Collider restore: " + error.Message); }
        savedCollider = null;
        colliderOwner = null;
        colliderCheckReported = false;
    }

    private void ApplyNoClipCollider(PlayerControl local)
    {
        if (!noClip || !local.Collider) return;
        if (!savedCollider || savedCollider.Pointer != local.Collider.Pointer)
        {
            RestoreCollider();
            savedCollider = local.Collider;
            colliderOwner = local;
            colliderEnabled = savedCollider.enabled;
            colliderCheckReported = false;
        }
        savedCollider.enabled = false;
        if (!colliderCheckReported)
        {
            colliderCheckReported = true;
            Plugin.Logger.LogInfo("Collider enabled after NoClip=" + savedCollider.enabled);
        }
        if (!savedCollider.enabled &&
            (status == "Freeplay hazir" || status == "Yalnizca yerel No-clip kullanilabilir"))
            status = "No-clip etkin";
    }

    private void ClearQueue() { killQueue.Clear(); pendingKills.Clear(); confirmUntil = 0; }

    [HideFromIl2Cpp]
    internal BridgeSnapshot HandleBridgeCommand(BridgeCommand command, byte argument)
    {
        if (command != BridgeCommand.Snapshot)
        {
            if (command == BridgeCommand.NoClip && NoClipAllowed)
                SetNoClip(argument != 0);
            else if (!ActionsAllowed) status = "Yalnizca Freeplay - islem yapilamadi";
            else switch (command)
            {
                case BridgeCommand.Protection:
                    immortal = argument != 0;
                    status = immortal ? "Koruma acik" : "Koruma kapali";
                    Plugin.Logger.LogInfo("Death/exile protection: " + immortal);
                    break;
                case BridgeCommand.ReviveSelf: ReviveSelf(); break;
                case BridgeCommand.TestDeath: TestDeath(); break;
                case BridgeCommand.KillSelected:
                    QueueKills(ReadTargets(true).Where(p => p.PlayerId == argument)); break;
                case BridgeCommand.PullSelected:
                    PullPlayers(ReadTargets(false).Where(p => p.PlayerId == argument).ToList()); break;
                case BridgeCommand.PullAll: PullPlayers(ReadTargets(false)); break;
                case BridgeCommand.ReviveAll: ReviveDummies(); break;
                case BridgeCommand.Orbit: SetOrbit(argument != 0); break;
                case BridgeCommand.Shuffle: ShufflePositions(); break;
                case BridgeCommand.KillAll: QueueKills(ReadTargets(true)); break;
                case BridgeCommand.Cancel: ClearQueue(); status = "Bekleyen islemler iptal edildi"; break;
                default: status = "Bilinmeyen komut"; break;
            }
        }
        return GetBridgeSnapshot();
    }

    [HideFromIl2Cpp]
    private BridgeSnapshot GetBridgeSnapshot()
    {
        var snapshot = new BridgeSnapshot {
            Freeplay = SessionAllowed, NoClipAvailable = NoClipAllowed,
            NoClip = noClip, Protection = immortal,
            Orbit = orbit, Status = status
        };
        foreach (var target in ReadTargets(false))
            snapshot.Targets.Add(new BridgeTarget {
                Id = target.PlayerId, Name = target.Data.PlayerName,
                Dead = target.Data.IsDead
            });
        return snapshot;
    }

    private void ReviveSelf()
    {
        if (!ActionsAllowed) return;
        PlayerControl.LocalPlayer.Revive();
        status = PlayerControl.LocalPlayer.Data.IsDead ? "Diriltme dogrulanamadi" : "Yerel oyuncu hayatta";
    }

    private void TestDeath()
    {
        if (!ActionsAllowed || PlayerControl.LocalPlayer.Data.IsDead) return;
        PlayerControl.LocalPlayer.Die(DeathReason.Kill, true);
        if (immortal && !PlayerControl.LocalPlayer.Data.IsDead)
            PlayerControl.LocalPlayer.Exiled();
        bool dead = PlayerControl.LocalPlayer.Data.IsDead;
        status = immortal ? (dead ? "Koruma basarisiz" : "Olum ve surgun engellendi")
            : (dead ? "Yerel oyuncu olu" : "Olum dogrulanamadi");
        Plugin.Logger.LogInfo("Freeplay protection test: enabled=" + immortal + " localDead=" + dead);
    }

    private void SetOrbit(bool value)
    {
        if (value)
        {
            if (!ActionsAllowed) return;
            orbitOrigins.Clear();
            foreach (var target in ReadTargets(false)) orbitOrigins[target.PlayerId] = target.GetTruePosition();
            if (orbitOrigins.Count == 0) return;
            orbit = true;
            orbitStarted = nextOrbit = Time.unscaledTime;
            status = "Donen halka aktif";
            Plugin.Logger.LogInfo("Freeplay orbit started with " + orbitOrigins.Count + " dummies");
        }
        else StopOrbit();
    }

    private void StopOrbit()
    {
        if (!orbit) return;
        orbit = false;
        if (SessionAllowed && PlayerControl.LocalPlayer.Pointer == sessionPlayer)
        {
            foreach (var target in ReadTargets(false))
                if (target.NetTransform && orbitOrigins.TryGetValue(target.PlayerId, out var position))
                    target.NetTransform.SnapTo(position);
        }
        orbitOrigins.Clear();
        status = "Donen halka kapali";
        Plugin.Logger.LogInfo("Freeplay orbit stopped");
    }

    private void ShufflePositions()
    {
        if (!ActionsAllowed) return;
        if (orbit) StopOrbit();
        var dummies = ReadTargets(false).Where(p => p.NetTransform).ToList();
        if (dummies.Count < 2) return;
        var positions = dummies.Select(p => p.GetTruePosition()).ToArray();
        for (int i = positions.Length - 1; i > 0; i--)
        {
            int j = System.Random.Shared.Next(i + 1);
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }
        for (int i = 0; i < dummies.Count; i++)
        {
            if (!ActionsAllowed) break;
            dummies[i].NetTransform.SnapTo(positions[i]);
        }
        status = "Konumlar karistirildi: " + dummies.Count;
        Plugin.Logger.LogInfo("Freeplay shuffle calls=" + dummies.Count);
    }

    private void ReviveDummies()
    {
        if (!ActionsAllowed) return;
        ClearQueue();
        int revived = 0;
        foreach (var target in ReadTargets(false))
        {
            if (!ActionsAllowed) break;
            if (!target.Data.IsDead) continue;
            target.Revive();
            if (!target.Data.IsDead) revived++;
        }
        status = "Diriltilen deneme oyuncusu: " + revived;
        Plugin.Logger.LogInfo("Freeplay dummy revives=" + revived);
    }

    public void ResetSession()
    {
        StopOrbit();
        RestoreCollider();
        noClip = immortal = false;
        ClearQueue();
        selected = null;
        targets.Clear();
        sessionPlayer = IntPtr.Zero;
        sessionWasFreeplay = false;
    }

    public void OnDestroy() { ResetSession(); if (Active == this) Active = null; }

    private void SetNoClip(bool value)
    {
        if (!NoClipAllowed) return;
        noClip = value;
        if (!value) RestoreCollider();
        status = value ? "No-clip etkin (yerel)" : "No-clip kapali";
        Plugin.Logger.LogInfo("NoClip requested: " + value);
    }

    private void Kill(PlayerControl target)
    {
        if (!ActionsAllowed || !target || !ReadTargets(true).Exists(p => p.PlayerId == target.PlayerId)) return;
        // Freeplay-local execution, without RPCs or changing the killer's role/death flag.
        PlayerControl.LocalPlayer.MurderPlayer(target, MurderResultFlags.Succeeded);
        status = target.Data.IsDead ? "Olduruldu: " + target.Data.PlayerName
            : "Oldurme sonucu bekleniyor: " + target.Data.PlayerName;
        Plugin.Logger.LogInfo("Freeplay murder target=" + target.PlayerId + " targetDead=" + target.Data.IsDead
            + " localDead=" + PlayerControl.LocalPlayer.Data.IsDead + " localRole=" + PlayerControl.LocalPlayer.Data.Role.Role);
    }

    [HideFromIl2Cpp]
    private void QueueKills(IEnumerable<PlayerControl> players)
    {
        if (!ActionsAllowed) return;
        foreach (var player in players)
            if (pendingKills.Add(player.PlayerId)) killQueue.Enqueue(player.PlayerId);
        status = "Siradaki deneme oyuncusu: " + killQueue.Count;
    }

    [HideFromIl2Cpp]
    private void PullPlayers(List<PlayerControl> players)
    {
        if (!ActionsAllowed) return;
        var origin = PlayerControl.LocalPlayer.GetTruePosition();
        int moved = 0;
        int verified = 0;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (!ActionsAllowed || !player || !player.NetTransform) break;
            float angle = (2 * MathF.PI * i) / Math.Max(1, players.Count);
            var destination = origin + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 0.8f;
            player.NetTransform.SnapTo(destination);
            moved++;
            if (Vector2.Distance(player.GetTruePosition(), destination) < 1.2f) verified++;
        }
        status = "Konum dogrulandi: " + verified + "/" + moved;
        Plugin.Logger.LogInfo("Freeplay snap calls=" + moved + " verified=" + verified);
    }

    [HideFromIl2Cpp]
    private void Run(Action action)
    {
        try { action(); }
        catch (Exception error) { status = "Hata: " + error.GetType().Name; Plugin.Logger.LogError(error); }
    }

    public void OnGUI()
    {
        if (!open) return;
        float width = Math.Min(540, Screen.width - 24);
        float height = Math.Min(640, Screen.height - 24);
        var area = new Rect((Screen.width - width) / 2, Math.Max(12, (Screen.height - height) / 2), width, height);
        GUI.Box(area, "KV3 / Yerel araclar");
        GUILayout.BeginArea(new Rect(area.x + 14, area.y + 30, width - 28, height - 44));
        GUILayout.Label(status, GUILayout.Height(34));
        bool enabled = GUI.enabled;
        GUI.enabled = NoClipAllowed;
        bool newNoClip = GUILayout.Toggle(noClip, "No-clip", GUILayout.Height(26));
        if (newNoClip != noClip) Run(() => SetNoClip(newNoClip));
        GUI.enabled = ActionsAllowed;
        bool newImmortal = GUILayout.Toggle(immortal, "Oldurulme ve surgune karsi koruma", GUILayout.Height(26));
        if (immortal != newImmortal)
        {
            immortal = newImmortal;
            Plugin.Logger.LogInfo("Death/exile protection: " + immortal);
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Kendini dirilt", GUILayout.Height(30))) Run(ReviveSelf);
        if (GUILayout.Button(immortal ? "Korumayi test et" : "Olumu test et", GUILayout.Height(30))) Run(TestDeath);
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
        int start = page * 6;
        for (int i = start; i < Math.Min(start + 6, targets.Count); i++)
        {
            var player = targets[i];
            if (!player || player.Data == null) continue;
            string label = player.Data.PlayerName + (player.Data.IsDead ? " [olu]" : " [hayatta]");
            if (GUILayout.Toggle(selected == player.PlayerId, label, GUILayout.Height(25))) selected = player.PlayerId;
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Height(30))) page = Math.Max(0, page - 1);
        GUILayout.Label((page + 1) + " / " + Math.Max(1, (targets.Count + 5) / 6), GUILayout.Width(48));
        if (GUILayout.Button(">", GUILayout.Height(30))) page = Math.Min(Math.Max(0, (targets.Count - 1) / 6), page + 1);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Secileni oldur", GUILayout.Height(30))) Run(() => QueueKills(ReadTargets(true).Where(p => p.PlayerId == selected)));
        if (GUILayout.Button("Secileni yanina cek", GUILayout.Height(30))) Run(() => PullPlayers(ReadTargets(false).Where(p => p.PlayerId == selected).ToList()));
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Tum deneme oyuncularini yanina cek", GUILayout.Height(30))) Run(() => PullPlayers(ReadTargets(false)));
        if (GUILayout.Button("Tum deneme oyuncularini dirilt", GUILayout.Height(30))) Run(ReviveDummies);
        GUILayout.BeginHorizontal();
        bool newOrbit = GUILayout.Toggle(orbit, "Donen halka", GUILayout.Height(30));
        if (newOrbit != orbit) Run(() => SetOrbit(newOrbit));
        if (GUILayout.Button("Konumlari karistir", GUILayout.Height(30))) Run(ShufflePositions);
        GUILayout.EndHorizontal();
        string killLabel = confirmUntil > Time.unscaledTime ? "Toplu oldurmeyi onayla" : "Tum deneme oyuncularini oldur";
        if (GUILayout.Button(killLabel, GUILayout.Height(30))) Run(() =>
        {
            if (confirmUntil > Time.unscaledTime) { QueueKills(ReadTargets(true)); confirmUntil = 0; }
            else confirmUntil = Time.unscaledTime + 10;
        });
        if (GUILayout.Button("Bekleyen islemleri iptal et", GUILayout.Height(30))) ClearQueue();
        GUI.enabled = enabled;
        if (GUILayout.Button("Kapat", GUILayout.Height(30))) open = false;
        GUILayout.EndArea();
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
internal static class MurderProtection
{
    private static bool Prefix(PlayerControl target) { return !SandboxPanel.Protect(target); }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
internal static class DeathProtection
{
    private static bool Prefix(PlayerControl __instance) { return !SandboxPanel.Protect(__instance); }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
internal static class ExileProtection
{
    private static bool Prefix(PlayerControl __instance) { return !SandboxPanel.Protect(__instance); }
}
