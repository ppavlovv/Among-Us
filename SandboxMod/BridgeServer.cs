using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using KV3Bridge;

namespace KV3.Sandbox;

internal sealed class BridgeServer : IDisposable
{
    private sealed class Request
    {
        public BridgeCommand Command;
        public byte Argument;
        public TaskCompletionSource<BridgeSnapshot> Completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly ConcurrentQueue<Request> pending = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly string pipeName = BridgeProtocol.PipeName(Process.GetCurrentProcess().Id);
    private Task worker;

    public void Start()
    {
        worker = Task.Run(ListenAsync);
        Plugin.Logger.LogInfo("Local INSERT bridge ready: " + pipeName);
    }

    public void Pump(SandboxPanel panel)
    {
        for (int i = 0; i < 8 && pending.TryDequeue(out var request); i++)
        {
            try { request.Completion.TrySetResult(panel.HandleBridgeCommand(request.Command, request.Argument)); }
            catch (Exception error)
            {
                Plugin.Logger.LogError(error);
                request.Completion.TrySetResult(new BridgeSnapshot { Status = "Islem hatasi: " + error.GetType().Name });
            }
        }
    }

    private async Task ListenAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(cancellation.Token);
                using var reader = new BinaryReader(pipe, Encoding.UTF8, true);
                using var writer = new BinaryWriter(pipe, Encoding.UTF8, true);
                byte argument;
                var command = BridgeProtocol.ReadRequest(reader, out argument);
                var request = new Request { Command = command, Argument = argument };
                pending.Enqueue(request);
                var completed = await Task.WhenAny(request.Completion.Task,
                    Task.Delay(1500, cancellation.Token));
                var snapshot = completed == request.Completion.Task
                    ? await request.Completion.Task
                    : new BridgeSnapshot { Status = "Oyun yanit vermedi" };
                BridgeProtocol.WriteResponse(writer, snapshot);
            }
            catch (OperationCanceledException) { break; }
            catch (IOException) { }
            catch (Exception error)
            {
                if (!cancellation.IsCancellationRequested)
                    Plugin.Logger.LogWarning("INSERT bridge: " + error.Message);
            }
        }
    }

    public void Dispose()
    {
        cancellation.Cancel();
        while (pending.TryDequeue(out var request)) request.Completion.TrySetCanceled();
        try { worker?.Wait(500); }
        catch (AggregateException) { }
        if (worker == null || worker.IsCompleted) cancellation.Dispose();
    }
}
