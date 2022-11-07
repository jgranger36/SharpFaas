using System.Text;

namespace Core;

public class FunctionConsole : StringWriter
{
    private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
    private StringWriter _writer;

    public FunctionConsole()
    {
        _writer = new StringWriter();
    }

    public override Encoding Encoding
    {
        get { return Encoding.UTF8; }
    }

    override public void Write(string output)
    {
        try
        {
            _semaphoreSlim.Wait();
            _writer.Write(output);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    override public void WriteLine(string output)
    {
        try
        {
            _semaphoreSlim.Wait();
            _writer.WriteLine(output);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public bool HasText()
    {
        return _writer.GetStringBuilder().Length > 0;
    }

    public string ReadAll()
    {
        var output = string.Empty;
        try
        {
            if (HasText())
            {
                _semaphoreSlim.Wait();

                output = _writer.ToString();

                _writer.GetStringBuilder().Clear();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        return output;
    }
}