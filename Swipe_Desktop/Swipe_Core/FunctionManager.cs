namespace Swipe_Core
{
public class FunctionManager
{
    public event Action<List<Function>>? OnFunctionChange;
    private List<Function> _functions = new List<Function>();
    public bool IsExecutionEnabled = true;

    private CurveCollector? _collector = null;
    public FunctionManager(CurveCollector collector)
    {
        LoadFunctions();
        _collector = collector;
        _collector.OnDetect += EvalutateDetectedCurves;
    }

    public void Unload()
    {
        if (_collector != null)
        {
            _collector.OnDetect -= EvalutateDetectedCurves;
        }
    }

    public void LoadFunctions()
    {
        string exePath = AppDomain.CurrentDomain.BaseDirectory;
        DirectoryInfo dir = new DirectoryInfo(Path.Combine(exePath, "Functions"));
        if (dir.Exists)
        {
            foreach (var file in dir.GetFiles("*.json"))
            {
                var func = Function.Load(file);
                if (func != null)
                {
                    _functions.Add(func);
                }
            }

            OnFunctionChange?.Invoke(_functions);
        }
    }

    public List<Function> GetFunctions()
    {
        return _functions;
    }

    public Function GetFunction(int index)
    {
        return _functions[index];
    }

    public Function CreateFunction()
    {
        var func = new Function();
        _functions.Add(func);
        OnFunctionChange?.Invoke(_functions);

        return func;
    }

    public void RemoveFunction(int index)
    {
        _functions.RemoveAt(index);
        OnFunctionChange?.Invoke(_functions);
    }

    private void EvalutateDetectedCurves(Dictionary<string, List<float>> detectedCurves)
    {
    }
}
}
