using Swipe_Core;
using System.Text.Json;

internal class Program
{
    private static void Main(string[] args)
    {
        if (args.Count() != 2)
        {
            Console.WriteLine("Wrong amount of input arguments, arg1: baseline, arg2: folder with tests");
            return;
        }

        string filePath = args[0];

        string jsonContent = File.ReadAllText(filePath);

        var baselineCurves = JsonSerializer.Deserialize<Dictionary<string, List<float>>>(jsonContent);
        if (baselineCurves == null)
        {
            Console.WriteLine("Invalid input file");
            return;
        }

        Dictionary<string, List<float>> results = new Dictionary<string, List<float>>();
        float total = 0.0f;
        int count = 0;

        foreach (string file in Directory.EnumerateFiles(args[1], "*.json"))
        {
            jsonContent = File.ReadAllText(file);
            var curves = JsonSerializer.Deserialize<Dictionary<string, List<float>>>(jsonContent);

            if (curves != null)
            {
                float fileTotal = 0.0f;
                int fileCount = 0;

                foreach (KeyValuePair<string, List<float>> baselineCurve in baselineCurves)
                {
                    List<float> ? values;
                    if (curves.TryGetValue(baselineCurve.Key, out values))
                    {
                        var dtw = DTW.CalculateDTW(baselineCurve.Value, values);
                        results.TryAdd(baselineCurve.Key, new List<float>());
                        results[baselineCurve.Key].Add(dtw);
                        total += dtw;
                        count++;
                        fileTotal += dtw;
                        fileCount++;
                        Console.WriteLine(dtw);
                    }
                }

                Console.WriteLine("### File Avg: " + (fileTotal / fileCount).ToString());
            }
        }

        Console.WriteLine("");
        Console.WriteLine("-------------------------------");
        Console.WriteLine("Avg: " + (total / count).ToString());
        Console.WriteLine("-------------------------------");

        foreach (KeyValuePair<string, List<float>> result in results)
        {
            var sum = 0.0f;
            foreach (var value in result.Value)
            {
                sum += value;
            }
            Console.WriteLine("Avg " + result.Key + ": " + (sum / result.Value.Count).ToString());
        }
    }
}
