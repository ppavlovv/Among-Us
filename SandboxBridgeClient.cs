using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using KV3Bridge;

namespace AmongUsExternal
{
    internal sealed class SandboxBridgeClient
    {
        private readonly MemoryManager memory;
        private bool loggedSnapshotFailure;

        public SandboxBridgeClient(MemoryManager memory)
        {
            this.memory = memory;
        }

        public BridgeSnapshot Send(BridgeCommand command, byte argument)
        {
            if (!memory.IsConnected || memory.GetProcessId() <= 0)
                return new BridgeSnapshot { Status = "Oyun baglantisi bekleniyor" };
            try
            {
                using (var pipe = new NamedPipeClientStream(".",
                    BridgeProtocol.PipeName(memory.GetProcessId()), PipeDirection.InOut))
                {
                    pipe.Connect(250);
                    var exchange = Task.Run(delegate
                    {
                        using (var writer = new BinaryWriter(pipe, Encoding.UTF8, true))
                        using (var reader = new BinaryReader(pipe, Encoding.UTF8, true))
                        {
                            BridgeProtocol.WriteRequest(writer, command, argument);
                            return BridgeProtocol.ReadResponse(reader);
                        }
                    });
                    if (!exchange.Wait(2500)) throw new TimeoutException("KV3 bridge timeout");
                    loggedSnapshotFailure = false;
                    return exchange.Result;
                }
            }
            catch (Exception error)
            {
                if (command != BridgeCommand.Snapshot || !loggedSnapshotFailure)
                    Logger.LogError("KV3 bridge baglantisi", error);
                if (command == BridgeCommand.Snapshot) loggedSnapshotFailure = true;
                return new BridgeSnapshot { Status = "KV3 mod baglantisi yok" };
            }
        }
    }
}
