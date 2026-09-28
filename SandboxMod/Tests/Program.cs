using KV3.Sandbox;
using KV3Bridge;

int checks = 0;
void Check(bool actual, bool expected, string name)
{
    if (actual != expected) throw new Exception("FAILED: " + name);
    checks++;
    Console.WriteLine("PASS: " + name);
}

Check(SandboxPolicy.IsSessionAllowed(true, 2, true), true, "Known build and local Freeplay accepted");
foreach (int mode in new[] { -1, 0, 1, 3 })
    Check(SandboxPolicy.IsSessionAllowed(true, mode, true), false, "Non-Freeplay rejected: " + mode);
Check(SandboxPolicy.IsSessionAllowed(false, 2, true), false, "Unknown build rejected");
Check(SandboxPolicy.IsSessionAllowed(true, 2, false), false, "Missing local player rejected");
Check(SandboxPolicy.CanAct(true, false, false), true, "Ready session accepts actions");
Check(SandboxPolicy.CanAct(false, false, false), false, "Session transition cancels actions");
Check(SandboxPolicy.CanAct(true, true, false), false, "Meeting blocks actions");
Check(SandboxPolicy.CanAct(true, false, true), false, "Animation blocks actions");
Check(SandboxPolicy.CanUseOnlineNoClip(true, 1, true, false, false), true,
    "Online local no-clip accepted");
Check(SandboxPolicy.CanUseOnlineNoClip(true, 0, true, false, false), false,
    "Local network no-clip rejected");
Check(SandboxPolicy.CanUseOnlineNoClip(false, 1, true, false, false), false,
    "Unknown build online no-clip rejected");
Check(SandboxPolicy.CanUseOnlineNoClip(true, 1, true, true, false), false,
    "Meeting blocks online no-clip");
Check(SandboxPolicy.CanUseOnlineNoClip(true, 1, true, false, true), false,
    "Animation blocks online no-clip");
Check(SandboxPolicy.CanTarget(true, false, true, false, false, true), true, "Alive dummy accepts kill");
Check(SandboxPolicy.CanTarget(true, false, true, false, true, true), false, "Dead dummy cannot be killed twice");
Check(SandboxPolicy.CanTarget(true, false, true, false, true, false), true, "Dead dummy accepts reposition");
Check(SandboxPolicy.CanTarget(true, true, true, false, false, true), false, "Local player excluded from target list");
Check(SandboxPolicy.CanTarget(true, false, false, false, false, true), false, "Real player excluded from test actions");
Check(SandboxPolicy.CanTarget(true, false, true, true, false, true), false, "Disconnected dummy excluded");
Check(SandboxPolicy.CanTarget(false, false, true, false, false, true), false, "Targets invalidated after leaving Freeplay");

using (var stream = new MemoryStream())
{
    var writer = new BinaryWriter(stream);
    BridgeProtocol.WriteRequest(writer, BridgeCommand.KillSelected, 7);
    stream.Position = 0;
    byte argument;
    var command = BridgeProtocol.ReadRequest(new BinaryReader(stream), out argument);
    Check(command == BridgeCommand.KillSelected && argument == 7, true, "Bridge request round-trip");
}
using (var stream = new MemoryStream())
{
    var writer = new BinaryWriter(stream);
    var snapshot = new BridgeSnapshot { Freeplay = true, NoClipAvailable = true,
        NoClip = true, Status = "hazir" };
    snapshot.Targets.Add(new BridgeTarget { Id = 3, Name = "Dummy 3", Dead = true });
    BridgeProtocol.WriteResponse(writer, snapshot);
    stream.Position = 0;
    var read = BridgeProtocol.ReadResponse(new BinaryReader(stream));
    Check(read.Connected && read.Freeplay && read.NoClipAvailable && read.NoClip &&
        read.Status == "hazir" &&
        read.Targets.Count == 1 && read.Targets[0].Id == 3 && read.Targets[0].Dead,
        true, "Bridge snapshot round-trip");
}
Console.WriteLine(checks + " policy/bridge tests passed. These tests do not verify Unity runtime behavior.");
