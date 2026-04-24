using Unity.Collections;
using UnityEngine;

public class DeleteCommand : BaseCommand
{
    public DeleteCommand()
    {
        commandType = CommandType.Delete;
    }

    public override void Deserialize(ref DataStreamReader reader)
    {
        Debug.Log($"[GenerateCommand Deserialize]");
    }
}
