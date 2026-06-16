namespace BrokerServer.Services;

public class CodeGenerator
{
    private const string Chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private static readonly Random Random = new();

    public string Generate()
    {
        var code = new char[6];
        for (int i = 0; i < 6; i++)
            code[i] = Chars[Random.Next(Chars.Length)];
        return $"{new string(code, 0, 3)}-{new string(code, 3, 3)}";
    }
}
