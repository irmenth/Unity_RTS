using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client instance;

    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7777;

    public readonly List<BaseCommand> COMMANDS = new();

    private void OnTickReceived(ulong tick, ref DataStreamReader stream)
    {
        if (curTick >= tick) return;
        COMMANDS.Clear();
        curTick = tick;
        int commandCount = stream.ReadInt();
        for (int i = 0; i < commandCount; i++)
        {
            CommandType commandType = (CommandType)stream.ReadByte();
            Debug.Log($"[Client] Receive: command {commandType}");
            BaseCommand command;
            switch (commandType)
            {
                case CommandType.Generate:
                    command = new GenerateCommand();
                    command.Deserialize(ref stream);
                    COMMANDS.Add(command);
                    break;
                case CommandType.Move:
                    break;
                case CommandType.Destroy:
                    break;
            }
        }

        EventBus.Publish(new CommandsReadyEvent(COMMANDS));
    }

    private void HandleConnectionMessage()
    {
        NetworkEvent.Type cmd;

        while ((cmd = connection.PopEvent(driver, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
        {
            switch (cmd)
            {
                case NetworkEvent.Type.Connect:
                    connected = true;
                    Debug.Log($"[Client] connected");
                    connected = true;
                    break;
                case NetworkEvent.Type.Data:
                    byte opCode = stream.ReadByte();
                    switch (opCode)
                    {
                        case (byte)OpCode.Pong:
                            double sendTimestamp = stream.ReadDouble();
                            double rtt = (Time.realtimeSinceStartupAsDouble - sendTimestamp) * 1000;
                            smoothRTT = smoothRTT == 0 ? rtt : rtt * 0.1 + smoothRTT * 0.9;
                            break;
                        case (byte)OpCode.Command:
                            ulong serverTick = stream.ReadULong();
                            OnTickReceived(serverTick, ref stream);
                            break;
                    }
                    break;
                case NetworkEvent.Type.Disconnect:
                    Debug.Log($"[Client] disconnected from {serverIP}:{serverPort}");
                    connected = false;
                    break;
            }
        }
    }

    private void SendPing()
    {
        driver.BeginSend(reliablePipeline, connection, out DataStreamWriter writer);
        writer.WriteByte((byte)OpCode.Ping);
        writer.WriteDouble(Time.realtimeSinceStartupAsDouble);
        driver.EndSend(writer);
    }

    public void SendInput(BaseCommand cmd)
    {
        float tickIntervalMS = 1000f / Server.TICK_RATE;
        int ticksAhead = (int)(smoothRTT / tickIntervalMS + 1f) + 1;
        ulong forTick = curTick + (ulong)ticksAhead;

        driver.BeginSend(reliablePipeline, connection, out DataStreamWriter writer);
        writer.WriteByte((byte)OpCode.Command);
        writer.WriteULong(forTick);
        cmd.Serialize(ref writer);
        driver.EndSend(writer);
    }

    private void Awake()
    {
        instance = this;
    }

    private NetworkDriver driver;
    private NetworkConnection connection;
    private NetworkPipeline reliablePipeline;
    private bool connected = false;
    private ulong curTick;

    private void Start()
    {
        driver = NetworkDriver.Create();
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

        NetworkEndpoint endpoint = NetworkEndpoint.Parse(serverIP, serverPort);
        connection = driver.Connect(endpoint);
        Debug.Log($"[Client] connecting to {endpoint}...");
    }

    [HideInInspector] public double smoothRTT;
    private float pingTimer;
    private const float PING_INTERVAL = 1f;

    private void Update()
    {
        driver.ScheduleUpdate().Complete();

        if (!connection.IsCreated) return;
        HandleConnectionMessage();

        if (connected)
        {
            pingTimer += Time.deltaTime;
            bool canSendPing = false;
            while (pingTimer >= PING_INTERVAL)
            {
                pingTimer -= PING_INTERVAL;
                canSendPing = true;
            }
            if (canSendPing) SendPing();
        }
    }

    private void OnDestroy()
    {
        driver.Dispose();

        instance = null;
    }
}