﻿using Swipe_Core.Readers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Swipe_Core
{
public class CurveCollector
{
    public enum Status
    {
        Calibrating,
        Idle,
        Anomaly
    }

    public event Action<string, float, int>? OnUpdated;
    public event Action<Dictionary<string, List<float>>>? OnDetect;
    public event Action<Status>? OnStatus;

    private Dictionary<string, ConcurrentQueue<float>> _values = new Dictionary<string, ConcurrentQueue<float>>();
    private Dictionary<string, int> _dataIndex = new Dictionary<string, int>();
    private Dictionary<string, (float, float)> _deadzones = new Dictionary<string, (float, float)>();
    private Dictionary<string, float> _deadzoneMultipliers = new Dictionary<string, float>();

    private bool _calibrating = true;
    private int _dataCount = 0;
    private int _dataWindow = 500;

    private bool _anomalyDetected = false;
    private int _anomalySamples = 0;
    private int _anomalySamplesRequired = 10;

    Dictionary<string, List<float>> _detectedCurve = new Dictionary<string, List<float>>();

    private bool _serializeCurves = false;

    public CurveCollector(IDataReader reader)
    {
        reader.OnUpdated += ProcessData;
    }

    public void AddKeyValueTracker(string key, float deadzoneMultiplier = 1.0f)
    {
        _values.TryAdd(key, new ConcurrentQueue<float>());
        _dataIndex.TryAdd(key, 0);
        _deadzones.TryAdd(key, (0.0f, 0.0f));
        _deadzoneMultipliers.TryAdd(key, deadzoneMultiplier);
        _detectedCurve.TryAdd(key, new List<float>());

        for (int i = 0; i < _dataWindow; i++)
        {
            _values[key].Enqueue(0.0f);
        }
    }

    private void ProcessData(string data)
    {
        StoreData(data);

        if (_calibrating)
        {
            OnStatus?.Invoke(Status.Calibrating);
            _dataCount++;
            if (_dataCount >= _dataWindow)
            {
                _calibrating = false;
                SetDeadZone();
                OnStatus?.Invoke(Status.Idle);
            }
        }
        else
        {
            CollectAnomalyCurve();
        }
    }

    private void StoreData(string data)
    {
        string[] linesNewline = data.Split(new[] { '\n' }, StringSplitOptions.None);
        string[] linesComma = data.Split(new[] { ',' }, StringSplitOptions.None);

        string[] lines = linesNewline.Length > linesComma.Length ? linesNewline : linesComma;

        foreach (var line in lines)
        {
            string[] keyValue = line.Split(new[] { ':' }, StringSplitOptions.None);

            foreach (var pair in _values)
            {
                if (pair.Key == keyValue[0])
                {
                    pair.Value.TryDequeue(out _);
                    if (float.TryParse(keyValue[1], out float value))
                    {
                        pair.Value.Enqueue(value);
                        _dataIndex[pair.Key]++;
                        OnUpdated?.Invoke(pair.Key, value, _dataIndex[pair.Key]);
                    }
                }
            }
        }
    }

    private void SetDeadZone()
    {
        foreach (var pair in _values)
        {
            var mean = pair.Value.Average();
            var variance = pair.Value.Average(v => (v - mean) * (v - mean));
            var deviation = (float)Math.Sqrt(variance);

            _deadzones[pair.Key] =
                (mean + deviation * _deadzoneMultipliers[pair.Key], mean - deviation * _deadzoneMultipliers[pair.Key]);
            Debug.WriteLine("Deadzone " + _deadzones[pair.Key].Item1.ToString() + " | " +
                            _deadzones[pair.Key].Item2.ToString());
        }
    }

    private void CollectAnomalyCurve()
    {
        if (_anomalyDetected == false)
        {
            foreach (var pair in _values)
            {
                var input = pair.Value.Last();
                if (input > _deadzones[pair.Key].Item1 || input < _deadzones[pair.Key].Item2)
                {
                    _anomalySamples++;

                    if (_anomalySamples == _anomalySamplesRequired)
                    {
                        _anomalyDetected = true;
                        _dataCount = 0;
                        OnStatus?.Invoke(Status.Anomaly);
                        Debug.WriteLine("Anomaly detected");
                    }

                    return;
                }
            }
        }
        else
        {
            _dataCount++;

            var curvesAtRest = true;
            foreach (var pair in _values)
            {
                var input = pair.Value.Last();
                if (input > _deadzones[pair.Key].Item1 || input < _deadzones[pair.Key].Item2)
                {
                    curvesAtRest = false;
                    _anomalySamples = _anomalySamplesRequired;
                }
            }

            if (curvesAtRest)
            {
                _anomalySamples--;

                if (_anomalySamples == 0)
                {
                    Debug.WriteLine("Rest detected");
                    Debug.WriteLine("Num sample: " + _dataCount);
                    _anomalyDetected = false;
                    OnStatus?.Invoke(Status.Idle);
                    HandleDetectedCurve();
                }
            }
        }
    }

    private void HandleDetectedCurve()
    {
        foreach (var pair in _values)
        {
            _detectedCurve[pair.Key].Clear();
            for (var i = 0; i < _dataCount; i++)
            {
                var index = Math.Max(0, pair.Value.Count - _dataCount + i);
                _detectedCurve[pair.Key].Add(pair.Value.ElementAt(index));
            }
        }

        if (_serializeCurves)
        {
            Serialize();
        }

        OnDetect?.Invoke(_detectedCurve);
    }

    private void Serialize()
    {
        string json = JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true });

        DateTime currentTime = DateTime.UtcNow;
        long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
        var filename = "curves_" + unixTime.ToString() + ".json";
        File.WriteAllText(filename, json);
    }

    public bool IsSeralizing()
    {
        return _serializeCurves;
    }
    public void ToggleSeralizing()
    {
        _serializeCurves = !_serializeCurves;
    }

    public void Recalibrate()
    {
        _calibrating = true;
        _dataCount = 0;
    }
}
}
