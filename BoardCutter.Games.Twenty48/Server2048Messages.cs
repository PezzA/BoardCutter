using System.Runtime.Serialization;

namespace BoardCutter.Games.Twenty48;

public static class Server2048Messages
{
    public const string AckHome = "AckHome";
    public const string InitGame = "InitGame";
    public const string PublicVisible = "PublicVisible";
    public const string SetGame = "SetGame";
    public const string ErrorMessage = "ErrorMessage";
}