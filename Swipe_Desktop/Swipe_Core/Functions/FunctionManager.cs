using FastDtw.CSharp;
using Swipe_Core.Devices;
using System;
using System.Diagnostics;
using System.Linq;

namespace Swipe_Core.Functions
{
public class FunctionManager
{
    public event Action<Dictionary<Guid, Function>>? OnFunctionChange;
    public event Action<bool>? OnActivation;
    public bool IsExecutionEnabled = true;

    public Dictionary<Guid, Function> Functions { get; private set; } = new Dictionary<Guid, Function>();
    public List<Function> ActivatedFunctions { get; private set; } = new List<Function>();
    private KeyboardDevice? _keyboardDevice = null;
    private BandDevice? _bandDevice = null;
    private PadDevice? _padDevice = null;
    private Logger? _logger = null;
    private float DTWThreshold = 2.25f;

    public FunctionManager(KeyboardDevice? keyboardDevice, BandDevice? bandDevice, PadDevice? padDevice)
    {
        LoadFunctions();
        if (keyboardDevice != null)
        {
            _keyboardDevice = keyboardDevice;
            _keyboardDevice.OnKeyPressed += EvaluateHeldKeys;
            _keyboardDevice.OnKeyReleased += ResetKeyboardFunctions;
        }
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
    }

    public void Unload()
    {
        foreach (var func in Functions)
        {
            func.Value.Save();
        }

        if (_keyboardDevice != null)
        {
            _keyboardDevice.OnKeyPressed -= EvaluateHeldKeys;
            _keyboardDevice.OnKeyReleased -= ResetKeyboardFunctions;
        }

        if (_bandDevice != null && _bandDevice.CurveCollector != null)
        {
            _bandDevice.CurveCollector.OnDetect -= EvalutateDetectedCurves;
        }

        if (_padDevice != null)
        {
            _padDevice.OnKeyPressed -= EvaluatePadClick;
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
                    Functions.Add(func.Guid, func);
                    func.OnFunctionChange += FunctionManager_OnFunctionChange;
                }
            }

            OnFunctionChange?.Invoke(Functions);
        }
    }

    private void FunctionManager_OnFunctionChange(Function obj)
    {
        OnFunctionChange?.Invoke(Functions);
    }

    public Dictionary<Guid, Function> GetFunctions()
    {
        return Functions;
    }

    public Function GetFunction(Guid guid)
    {
        return Functions[guid];
    }

    public Function CreateFunction()
    {
        var func = new KeyboardFunction();
        Functions.Add(func.Guid, func);
        func.OnFunctionChange += FunctionManager_OnFunctionChange;
        OnFunctionChange?.Invoke(Functions);

        _logger?.Log("[USER] Function added");

        return func;
    }

    public void RemoveFunction(Guid guid)
    {
        _logger?.Log($"[USER] Function removed");

        Functions[guid].Remove();
        Functions[guid].OnFunctionChange -= FunctionManager_OnFunctionChange;
        Functions.Remove(guid);
        OnFunctionChange?.Invoke(Functions);
    }

    public void ReplaceFunction(Function func)
    {
        Functions[func.Guid].Remove();
        Functions[func.Guid].OnFunctionChange -= FunctionManager_OnFunctionChange;
        Functions.Remove(func.Guid);
        Functions.Add(func.Guid, func);
        func.OnFunctionChange += FunctionManager_OnFunctionChange;
        OnFunctionChange?.Invoke(Functions);
    }

    public void SetDTWTreshold(float dtw)
    {
        DTWThreshold = dtw;
    }
    public void AddLogger(Logger logger)
    {
        _logger = logger;
    }

    private void EvaluateHeldKeys(SortedSet<int> keys)
    {
        if (!IsExecutionEnabled || Functions.Count == 0)
        {
            return;
        }

        foreach (var function in Functions)
        {
            var keyboardFunction = function.Value as KeyboardFunction;
            if (keyboardFunction != null && keyboardFunction.IsEnabled)
            {
                if (keyboardFunction.EvaluateAndRun(keys))
                {
                    ActivatedFunctions.Add(keyboardFunction);
                    OnActivation?.Invoke(true);
                    _logger?.Log($"[SYSTEM] Keyboard function {keyboardFunction.Name} activated");
                }
            }
        }
    }

    private void ResetKeyboardFunctions(SortedSet<int> _)
    {
        foreach (var function in Functions)
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
        if (!IsExecutionEnabled || Functions.Count == 0)
        {
            return;
        }

        List<(BandFunction, float, List<(Guid, float)>)> functionDtws =
            new List<(BandFunction, float, List<(Guid, float)>)>();
        foreach (var function in Functions)
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
                ActivatedFunctions.Add(sortedDict[0].Item1);
                OnActivation?.Invoke(true);
                _logger?.Log($"[SYSTEM] Band function {sortedDict[0].Item1.Name} activated");
            }

            foreach (var recording in sortedDict[0].Item3)
            {
                sortedDict[0].Item1.RecordingActivations[recording.Item1]++;
            }
        }
    }
    private void EvaluatePadClick(PadDevice.PadKey key)
    {
        if (!IsExecutionEnabled || Functions.Count == 0)
        {
            return;
        }

        foreach (var function in Functions)
        {
            var padFunction = function.Value as PadFunction;
            if (padFunction != null && padFunction.IsEnabled)
            {
                var modifier = PadFunction.KeyModifier.None;
                if (_keyboardDevice != null)
                {
                    bool ctrl = _keyboardDevice.Keys.Contains(0x11);
                    bool shift = _keyboardDevice.Keys.Contains(0x10);
                    bool alt = _keyboardDevice.Keys.Contains(0x12);

                    if (ctrl && shift && alt)
                        modifier = PadFunction.KeyModifier.CtrlAltShift;
                    else if (ctrl && shift)
                        modifier = PadFunction.KeyModifier.CtrlShift;
                    else if (ctrl && alt)
                        modifier = PadFunction.KeyModifier.CtrlAlt;
                    else if (alt && shift)
                        modifier = PadFunction.KeyModifier.AltShift;
                    else if (ctrl)
                        modifier = PadFunction.KeyModifier.Ctrl;
                    else if (shift)
                        modifier = PadFunction.KeyModifier.Shift;
                    else if (alt)
                        modifier = PadFunction.KeyModifier.Alt;
                    else
                        modifier = PadFunction.KeyModifier.None;
                }

                if (padFunction.EvaluateAndRun(key, modifier))
                {
                    ActivatedFunctions.Add(padFunction);
                    OnActivation?.Invoke(true);
                    _logger?.Log($"[SYSTEM] Pad function { padFunction.Name } activated");
                }
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
