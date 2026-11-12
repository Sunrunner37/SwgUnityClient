namespace Swg.Client.Runtime.Commands
{
    public interface ICommandDispatcher
    {
        void Submit(string channel, string text, out string error);
        void Execute(string name, string arguments, out string error);
    }
}
