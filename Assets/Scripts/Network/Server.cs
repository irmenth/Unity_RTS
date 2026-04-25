using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Server : MonoBehaviour
{
    public static Server instance;
    public const int TICK_RATE = 30;

    [SerializeField] private int maxConnections = 4;
    [SerializeField] private ushort port = 7777;

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

    private void OnCommandReceived(ref DataStreamReader stream)
    {
        ulong recTick = stream.ReadULong();
        if (recTick < curTick) return;

        CommandType commandType = (CommandType)stream.ReadByte();
        Debug.Log($"[Server] Receive: command {commandType}");
        BaseCommand command;
        switch (commandType)
        {
            case CommandType.Generate:
                command = new GenerateCommand();
                command.Deserialize(ref stream);
                commandBuffer.Add(command);
                break;
            case CommandType.Move:
                command = new MoveCommand();
                command.Deserialize(ref stream);
                commandBuffer.Add(command);
                break;
            case CommandType.Delete:
                command = new DeleteCommand();
                command.Deserialize(ref stream);
                commandBuffer.Add(command);
                break;
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
                        OpCode opCode = (OpCode)stream.ReadByte();
                        switch (opCode)
                        {
                            case OpCode.Ping:
                                double timestamp = stream.ReadDouble();
                                driver.BeginSend(reliablePipeline, c, out DataStreamWriter writer);
                                writer.WriteByte((byte)OpCode.Pong);
                                writer.WriteDouble(timestamp);
                                driver.EndSend(writer);
                                break;
                            case OpCode.Command:
                                OnCommandReceived(ref stream);
                                break;
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
            writer.WriteByte((byte)OpCode.Command);
            writer.WriteULong(curTick);
            writer.WriteInt(commandBuffer.Count);

            for (int j = 0; j < commandBuffer.Count; j++)
            {
                commandBuffer[j].Serialize(ref writer);
            }
            driver.EndSend(writer);
        }

        commandBuffer.Clear();
    }

    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;
    private NetworkPipeline reliablePipeline;
    private float tickInterval;
    private float tickTimer;
    private ulong curTick;
    private readonly List<BaseCommand> commandBuffer = new();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        driver = NetworkDriver.Create();
        connections = new(maxConnections, Allocator.Persistent);
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

        tickInterval = 1f / TICK_RATE;

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

        instance = null;
    }
}