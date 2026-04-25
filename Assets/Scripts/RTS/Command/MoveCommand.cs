using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class MoveCommand : BaseCommand
{
    public float2 pos;

    public MoveCommand()
    {
        commandType = CommandType.Move;
    }
    public MoveCommand(float2 pos)
    {
        commandType = CommandType.Move;
        this.pos = pos;
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)commandType);
        writer.WriteFloat(pos.x);
        writer.WriteFloat(pos.y);
    }

    public override void Deserialize(ref DataStreamReader reader)
    {
        pos.x = reader.ReadFloat();
        pos.y = reader.ReadFloat();

        Debug.Log($"[MoveCommand Deserialize] pos: {pos}");
    }
}
