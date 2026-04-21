using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Server : MonoBehaviour
{
    [SerializeField] private int maxConnections = 4;
    [SerializeField] private ushort port = 7777;
    [SerializeField] private int tickRate = 30;

    private void CleanConnections()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if (!driver.IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                i--;
            }
        }
    }

    private void AcceptConnections()
    {
        NetworkConnection c;
        while ((c = driver.Accept()) != default)
        {
            if (connections.Length >= maxConnections)
            {
                Debug.Log("[Server] max connections reached");
                return;
            }

            connections.AddNoResize(c);
            Debug.Log($"[Server] accepted connection from {c}");
        }
    }

    private void HandleConnectionsMeesage()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            NetworkConnection c = connections[i];
            NetworkEvent.Type cmd;

            while ((cmd = driver.PopEventForConnection(c, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
            {
                switch (cmd)
                {
                    case NetworkEvent.Type.Data:
                        ulong recTick = stream.ReadULong();
                        int recInput = stream.ReadInt();
                        Debug.Log($"[Server] Receive: tick {recTick}, input {recInput}");
                        if (recTick == curTick || recTick == curTick + 1)
                        {
                            pendingInput[i] = recInput;
                            inputReady[i] = true;
                        }
                        break;
                    case NetworkEvent.Type.Disconnect:
                        Debug.Log($"[Server] disconnected from {c}");
                        connections[i] = default;
                        break;
                }
            }
        }
    }

    private void AdvanceTick()
    {
        curTick++;

        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated || connections[i] == default) continue;

            driver.BeginSend(reliablePipeline, connections[i], out DataStreamWriter writer);
            writer.WriteULong(curTick);
            writer.WriteInt(maxConnections);

            for (int j = 0; j < maxConnections; j++)
            {
                writer.WriteInt(j < connections.Length && inputReady[j] ? pendingInput[j] : 0);
            }
            driver.EndSend(writer);
        }

        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated || connections[i] == default) continue;

            inputReady[i] = false;
            pendingInput[i] = 0;
        }
    }

    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;
    private NetworkPipeline reliablePipeline;
    private float tickInterval;
    private float tickTimer;
    private ulong curTick;
    private NativeArray<int> pendingInput;
    private NativeArray<bool> inputReady;

    private void Start()
    {
        driver = NetworkDriver.Create();
        connections = new(maxConnections, Allocator.Persistent);
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

        tickInterval = 1f / tickRate;
        pendingInput = new(maxConnections, Allocator.Persistent);
        inputReady = new(maxConnections, Allocator.Persistent);

        NetworkEndpoint endpoint = NetworkEndpoint.Parse("127.0.0.1", port);
        if (driver.Bind(endpoint) != 0)
        {
            Debug.LogError($"[Server] failed to bind to endpoint {endpoint}");
            return;
        }
        driver.Listen();
        Debug.Log($"[Server] listening on {endpoint}");
    }

    private void Update()
    {
        driver.ScheduleUpdate().Complete();

        CleanConnections();
        AcceptConnections();
        HandleConnectionsMeesage();

        tickTimer += Time.deltaTime;
        while (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            AdvanceTick();
        }
    }

    private void OnDestroy()
    {
        driver.Dispose();
        connections.Dispose();
        pendingInput.Dispose();
        inputReady.Dispose();
    }
}