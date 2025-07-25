using FastDtw.CSharp;
using Swipe_Core.Devices;
using System;
using System.Diagnostics;

namespace Swipe_Core.Functions
{
public class FunctionManager
{
    public event Action<Dictionary<Guid, Function>>? OnFunctionChange;
    public bool IsExecutionEnabled = true;

    private Dictionary<Guid, Function> _functions = new Dictionary<Guid, Function>();
    private BandDevice? _bandDevice = null;
    private PadDevice? _padDevice = null;
    private float DTWThreshold = 2.25f;

    public FunctionManager(BandDevice? bandDevice, PadDevice? padDevice)
    {
        LoadFunctions();
        if (bandDevice != null && bandDevice.CurveCollector != null)
        {
            _bandDevice = bandDevice;
            _bandDevice.CurveCollector.OnDetect += EvalutateDetectedCurves;
        }
        if (padDevice != null)
        {
            _padDevice = padDevice;
            _padDevice.OnKeyPressed += EvaluatePadClick;
        }
        KeyboardManager.OnKeyPressed += EvaluateHeldKeys;
        KeyboardManager.OnKeyReleased += ResetKeyboardFunctions;
    }

    public void Unload()
    {
        if (_bandDevice != null && _bandDevice.CurveCollector != null)
        {
            _bandDevice.CurveCollector.OnDetect -= EvalutateDetectedCurves;
        }

        if (_padDevice != null)
        {
            _padDevice.OnKeyPressed -= EvaluatePadClick;
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

        List<(BandFunction, float, List<(Guid, float)>)> functionDtws =
            new List<(BandFunction, float, List<(Guid, float)>)>();
        foreach (var function in _functions)
        {
            var swipeBandFunction = function.Value as BandFunction;
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

                        var d = (float)Dtw.GetScore(a, b, NormalizationType.PathLength);

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
    private void EvaluatePadClick(PadDevice.PadKey key)
    {
        if (!IsExecutionEnabled || _functions.Count == 0)
        {
            return;
        }

        foreach (var function in _functions)
        {
            var padFunction = function.Value as PadFunction;
            if (padFunction != null && padFunction.IsEnabled)
            {
                padFunction.EvaluateAndRun(key);
            }
        }
    }

    public TTarget ConvertFunction<TSource, TTarget>(TSource source, Function.InterfaceType targetType)
        where TSource : Function
        where TTarget : Function, new()
    {
        var target = new TTarget();

        foreach (var prop in typeof(Function).GetProperties())
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(source);
                prop.SetValue(target, value);
            }
        }

        target.SetInterfaceType(targetType);
        ReplaceFunction(target);
        return target;
    }

    public BandFunction ConvertKeyboardToBand(KeyboardFunction keyboardFunction)
    {
        return ConvertFunction<KeyboardFunction, BandFunction>(keyboardFunction, Function.InterfaceType.Band);
    }
    public PadFunction ConvertKeyboardToPad(KeyboardFunction keyboardFunction)
    {
        return ConvertFunction<KeyboardFunction, PadFunction>(keyboardFunction, Function.InterfaceType.Pad);
    }
    public KeyboardFunction ConvertBandToKeyboard(BandFunction bandFunction)
    {
        return ConvertFunction<BandFunction, KeyboardFunction>(bandFunction, Function.InterfaceType.Keyboard);
    }
    public PadFunction ConvertBandToPad(BandFunction bandFunction)
    {
        return ConvertFunction<BandFunction, PadFunction>(bandFunction, Function.InterfaceType.Pad);
    }
    public KeyboardFunction ConvertPadToKeyboard(PadFunction padFunction)
    {
        return ConvertFunction<PadFunction, KeyboardFunction>(padFunction, Function.InterfaceType.Keyboard);
    }
    public BandFunction ConvertPadToBand(PadFunction padFunction)
    {
        return ConvertFunction<PadFunction, BandFunction>(padFunction, Function.InterfaceType.Band);
    }
}
}
