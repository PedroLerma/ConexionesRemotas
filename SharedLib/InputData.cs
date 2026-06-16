namespace SharedLib;

public class InputData
{
    public string Type { get; set; } = "MouseMove";
    public int X { get; set; }
    public int Y { get; set; }
    public int Button { get; set; }
    public int Delta { get; set; }
    public int KeyCode { get; set; }
}
