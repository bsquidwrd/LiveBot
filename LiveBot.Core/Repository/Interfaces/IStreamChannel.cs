using System.Text.RegularExpressions;

namespace LiveBot.Core.Repository.Interfaces
{
    public interface IStreamChannel
    {
        string Site { get; }
        string StreamURL { get; }
        string URLPattern { get; }
        Regex URLRegex { get; }

        bool IsValid();

        string GetUsername();
    }
}