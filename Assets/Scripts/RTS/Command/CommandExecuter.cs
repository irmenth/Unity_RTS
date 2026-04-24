using UnityEngine;

public class CommandExecuter : MonoBehaviour
{
    public static CommandExecuter instance;

    private void Generate(GenerateCommand cmd)
    {
        if (!UnitBus.instance) return;

        UnitBus.instance.InstantiateUnit(cmd);
    }

    private void Delete(DeleteCommand cmd)
    {
        if (!UnitBus.instance) return;

        UnitBus.instance.Delete(cmd);
    }

    private void OnCommandsReady(CommandsReadyEvent e)
    {
        foreach (var cmd in e.commands)
        {
            switch (cmd.commandType)
            {
                case CommandType.Generate:
                    Generate(cmd as GenerateCommand);
                    break;
                case CommandType.Move:
                    break;
                case CommandType.Delete:
                    Delete(cmd as DeleteCommand);
                    break;
            }
        }
    }

    private void Start()
    {
        instance = this;
        EventBus.Subscribe<CommandsReadyEvent>(OnCommandsReady);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<CommandsReadyEvent>(OnCommandsReady);
        instance = null;
    }
}
