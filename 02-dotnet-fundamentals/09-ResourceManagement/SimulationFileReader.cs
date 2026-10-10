public sealed class SimulationFileReader : IDisposable
{
    private readonly StreamReader _reader;

    public SimulationFileReader(string path)
    {
        _reader = new StreamReader(path);
    }

    public string? ReadLine()
    {
        return _reader.ReadLine();
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}