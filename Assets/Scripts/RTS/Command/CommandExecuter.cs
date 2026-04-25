using UnityEngine;

public class CommandExecuter : MonoBehaviour
{
    public static CommandExecuter instance;

    private void Generate(GenerateCommand cmd)
    {
        if (!UnitBus.instance) return;

        UnitBus.instance.InstantiateUnit(cmd);
    }

    private void Move(MoveCommand cmd)
    {
        if (!UnitBus.instance) return;

        UnitBus.instance.SetDestination(cmd);
    }

    private void Delete()
    {
        if (!UnitBus.instance) return;

        UnitBus.instance.Delete();
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
                    Move(cmd as MoveCommand);
                    break;
                case CommandType.Delete:
                    Delete();
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
