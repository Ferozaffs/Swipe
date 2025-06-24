using FastDtw.CSharp;
using System.Diagnostics;

namespace Swipe_Core
{
public class FunctionManager
{
    public event Action<List<Function>>? OnFunctionChange;
    public bool IsExecutionEnabled = true;

    private List<Function> _functions = new List<Function>();
    private CurveCollector? _collector = null;
    private float DTWThreshold = 2.0f;

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
        DirectoryInfo dir = new DirectoryInfo("Functions");
        if (dir.Exists)
        {
            foreach (var file in dir.GetFiles("*.json"))
            {
                var func = Function.Load(file);
                if (func != null)
                {
                    _functions.Add(func);
                    _functions.Last().OnFunctionChange += FunctionManager_OnFunctionChange;
                }
            }

            OnFunctionChange?.Invoke(_functions);
        }
    }

    private void FunctionManager_OnFunctionChange(Function obj)
    {
        OnFunctionChange?.Invoke(_functions);
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
        _functions.Last().OnFunctionChange += FunctionManager_OnFunctionChange;
        OnFunctionChange?.Invoke(_functions);

        return func;
    }

    public void RemoveFunction(int index)
    {
        _functions.ElementAt(index).Remove();
        _functions.ElementAt(index).OnFunctionChange -= FunctionManager_OnFunctionChange;
        _functions.RemoveAt(index);
        OnFunctionChange?.Invoke(_functions);
    }

    private void EvalutateDetectedCurves(Dictionary<string, List<float>> detectedCurves)
    {
        if (!IsExecutionEnabled || _functions.Count == 0)
        {
            return;
        }

        List<(Function, float)> functionDtws = new List<(Function, float)>();
        foreach (var function in _functions)
        {
            if (function.Recordings.Count == 0)
            {
                continue;
            }

            List<float> dtws = new List<float>();
            var rC = 0;
            foreach (var recording in function.Recordings)
            {
                var dtw = 0.0f;
                var cC = 0;
                foreach (KeyValuePair<string, List<float>> curve in detectedCurves)
                {
                    if (curve.Key.Contains("Proximity"))
                    {
                        continue;
                    }

                    List<float> ? values;
                    if (recording.TryGetValue(curve.Key, out values))
                    {
                        double[] a = curve.Value.Select(f => (double)f).ToArray();
                        double[] b = values.Select(f => (double)f).ToArray();

                        var d = (float)FastDtw.CSharp.Dtw.GetScore(a, b, NormalizationType.PathLength);

                        dtw += d;

                        // Debug.WriteLine(rC + ":" + curve.Key + " = " + d);
                    }

                    cC++;
                }
                dtws.Add(dtw);
                rC++;
            }
            dtws.Sort();
            var bestDtws = dtws.Take(3);
            var averageDtw = bestDtws.Average();

            functionDtws.Add((function, averageDtw));
        }
        var sortedDict = from dtw in functionDtws orderby dtw.Item2 ascending select dtw;

        if (functionDtws.Count > 0 && functionDtws[0].Item2 < DTWThreshold)
        {
            functionDtws[0].Item1.RunFunction();
        }
    }
}
}
