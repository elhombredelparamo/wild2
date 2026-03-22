using Godot;

namespace Wild.UI.Commands
{
    public interface IConsoleCommand
    {
        string Name { get; }
        string Description { get; }
        void Execute(string[] args);
    }
}
