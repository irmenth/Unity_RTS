using Unity.Collections;

public class BaseCommand
{
    public CommandType commandType = CommandType.None;

    public virtual void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)commandType);
    }

    public virtual void Deserialize(ref DataStreamReader reader) { }
}
