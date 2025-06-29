using FastDtw.CSharp;
using System;
using System.Diagnostics;

namespace Swipe_Core
{
public class FunctionManager
{
    public event Action<Dictionary<Guid, Function>>? OnFunctionChange;
    public bool IsExecutionEnabled = true;

    private Dictionary<Guid, Function> _functions = new Dictionary<Guid, Function>();
    private CurveCollector? _collector = null;
    private float DTWThreshold = 2.25f;

    public FunctionManager(CurveCollector collector)
    {
        LoadFunctions();
        _collector = collector;
        _collector.OnDetect += EvalutateDetectedCurves;
        KeyboardManager.OnKeyPressed += EvaluateHeldKeys;
        KeyboardManager.OnKeyReleased += ResetKeyboardFunctions;
    }

    public void Unload()
    {
        if (_collector != null)
        {
            _collector.OnDetect -= EvalutateDetectedCurves;
        }

        KeyboardManager.OnKeyPressed -= EvaluateHeldKeys;
        KeyboardManager.OnKeyReleased -= ResetKeyboardFunctions;
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
                    _functions.Add(func.Guid, func);
                    func.OnFunctionChange += FunctionManager_OnFunctionChange;
                }
            }

            OnFunctionChange?.Invoke(_functions);
        }
    }

    private void FunctionManager_OnFunctionChange(Function obj)
    {
        OnFunctionChange?.Invoke(_functions);
    }

    public Dictionary<Guid, Function> GetFunctions()
    {
        return _functions;
    }

    public Function GetFunction(Guid guid)
    {
        return _functions[guid];
    }

    public Function CreateFunction()
    {
        var func = new KeyboardFunction();
        _functions.Add(func.Guid, func);
        func.OnFunctionChange += FunctionManager_OnFunctionChange;
        OnFunctionChange?.Invoke(_functions);

        return func;
    }

    public void RemoveFunction(Guid guid)
    {
        _functions[guid].Remove();
        _functions[guid].OnFunctionChange -= FunctionManager_OnFunctionChange;
        _functions.Remove(guid);
        OnFunctionChange?.Invoke(_functions);
    }

    public void ReplaceFunction(Function func)
    {
        _functions[func.Guid].Remove();
        _functions[func.Guid].OnFunctionChange -= FunctionManager_OnFunctionChange;
        _functions.Remove(func.Guid);
        _functions.Add(func.Guid, func);
        func.OnFunctionChange += FunctionManager_OnFunctionChange;
        OnFunctionChange?.Invoke(_functions);
    }
    private void EvaluateHeldKeys(SortedSet<int> keys)
    {
        if (!IsExecutionEnabled || _functions.Count == 0)
        {
            return;
        }

        foreach (var function in _functions)
        {
            var keyboardFunction = function.Value as KeyboardFunction;
            if (keyboardFunction != null && keyboardFunction.IsEnabled)
            {
                keyboardFunction.EvaluateAndRun(keys);
            }
        }
    }

    private void ResetKeyboardFunctions(SortedSet<int> _)
    {
        foreach (var function in _functions)
        {
            var keyboardFunction = function.Value as KeyboardFunction;
            if (keyboardFunction != null && keyboardFunction.IsEnabled)
            {
                keyboardFunction.Reset();
            }
        }
    }

    private void EvalutateDetectedCurves(Dictionary<string, List<float>> detectedCurves)
    {
        if (!IsExecutionEnabled || _functions.Count == 0)
        {
            return;
        }

        List<(SwipeBandFunction, float, List<(Guid, float)>)> functionDtws =
            new List<(SwipeBandFunction, float, List<(Guid, float)>)>();
        foreach (var function in _functions)
        {
            var swipeBandFunction = function.Value as SwipeBandFunction;
            if (swipeBandFunction == null || swipeBandFunction.IsEnabled == false ||
                swipeBandFunction.Recordings.Count == 0)
            {
                continue;
            }

            List<(Guid, float)> dtws = new List<(Guid, float)>();
            var rC = 0;
            foreach (var recording in swipeBandFunction.Recordings)
            {
                var dtw = 0.0f;
                var cC = 0;
                foreach (KeyValuePair<string, List<float>> curve in detectedCurves)
                {
                    if (!swipeBandFunction.AxisEnabled.ContainsKey(curve.Key) ||
                        !swipeBandFunction.AxisEnabled[curve.Key])
                    {
                        continue;
                    }

                    List<float> ? values;
                    if (recording.Value.TryGetValue(curve.Key, out values))
                    {
                        double[] a = curve.Value.Select(f => (double)f).ToArray();
                        double[] b = values.Select(f => (double)f).ToArray();

                        var d = (float)FastDtw.CSharp.Dtw.GetScore(a, b, NormalizationType.PathLength);

                        dtw += d;
                    }

                    cC++;
                }
                dtws.Add((recording.Key, dtw));
                rC++;
            }
            var sortedAsc = dtws.OrderBy(x => x.Item2).ToList();
            var bestDtws = sortedAsc.Take(3);
            var averageDtw = bestDtws.Average(x => x.Item2);

            functionDtws.Add((swipeBandFunction, averageDtw, sortedAsc));
        }
        var sortedDict = functionDtws.OrderBy(x => x.Item2).ToList();

        if (sortedDict.Count > 0 && sortedDict[0].Item2 < DTWThreshold)
        {
            if (sortedDict[0].Item1.RunFunction() == "Success")
            {
                sortedDict[0].Item1.NumActivations++;
            }

            foreach (var recording in sortedDict[0].Item3)
            {
                sortedDict[0].Item1.RecordingActivations[recording.Item1]++;
            }
        }
    }

    public SwipeBandFunction ConvertKeyboardToBand(KeyboardFunction keyboardFunction)
    {
        var swipeBandFunction = new SwipeBandFunction();
        foreach (var prop in typeof(Function).GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(keyboardFunction);
                prop.SetValue(swipeBandFunction, value);
            }
        }

        swipeBandFunction.SetInterfaceType(Function.InterfaceType.SwipeBand);
        ReplaceFunction(swipeBandFunction);
        return swipeBandFunction;
    }

    public KeyboardFunction ConvertBandToKeyboard(SwipeBandFunction swipeBandFunction)
    {
        var keyboardFunction = new KeyboardFunction();
        foreach (var prop in typeof(Function).GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(swipeBandFunction);
                prop.SetValue(keyboardFunction, value);
            }
        }

        keyboardFunction.SetInterfaceType(Function.InterfaceType.Keyboard);
        ReplaceFunction(keyboardFunction);
        return keyboardFunction;
    }
}
}
