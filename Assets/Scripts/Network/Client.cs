using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.InputSystem;

public class Client : MonoBehaviour
{
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7777;

    private void OnTickReceived(ulong tick, int[] inputs)
    {
        if (curTick < tick)
        {
            curTick = tick;
            for (int i = 0; i < inputs.Length; i++)
            {
                Debug.Log($"[Client] tick: {tick}, received input: {inputs[i]}");
            }
        }
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
                    ulong serverTick = stream.ReadULong();
                    int inputCount = stream.ReadInt();
                    int[] serverInputs = new int[inputCount];
                    for (int i = 0; i < inputCount; i++)
                    {
                        serverInputs[i] = stream.ReadInt();
                    }
                    OnTickReceived(serverTick, serverInputs);
                    break;
                case NetworkEvent.Type.Disconnect:
                    Debug.Log($"[Client] disconnected from {serverIP}:{serverPort}");
                    connected = false;
                    break;
            }
        }
    }

    private void SendInput(ulong targetTick, int value)
    {
        if (!connected) return;

        driver.BeginSend(reliablePipeline, connection, out DataStreamWriter writer);
        writer.WriteULong(targetTick);
        writer.WriteInt(value);
        driver.EndSend(writer);
    }

    private NetworkDriver driver;
    private NetworkConnection connection;
    private NetworkPipeline reliablePipeline;
    private bool connected = false;
    private ulong curTick;
    private int pendingInput;
    private bool inputReady;

    private void Start()
    {
        driver = NetworkDriver.Create();
        reliablePipeline = driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

        NetworkEndpoint endpoint = NetworkEndpoint.Parse(serverIP, serverPort);
        connection = driver.Connect(endpoint);
        Debug.Log($"[Client] connecting to {endpoint}...");
    }

    private void Update()
    {
        driver.ScheduleUpdate().Complete();

        if (!connection.IsCreated) return;
        HandleConnectionMessage();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            pendingInput = 1;
            inputReady = true;
        }

        if (connected && inputReady)
        {
            SendInput(curTick + 1, pendingInput);
            pendingInput = 0;
            inputReady = false;
        }
    }

    private void OnDestroy()
    {
        driver.Dispose();
    }
}