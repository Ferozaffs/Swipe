namespace Swipe_Core
{
public static class DTW
{
    public static float DerivateDTW(List<float> a, List<float> b)
    {
        var aD = ComputeDerivatives(a);
        var bD = ComputeDerivatives(b);
        return CalculateDTW(aD, bD);
    }

    private static List<float> ComputeDerivatives(List<float> values)
    {
        List<float> derivatives = new List<float>();
        for (int i = 1; i < values.Count(); i++)
        {
            derivatives.Add(values[i] - values[i - 1]);
        }

        return derivatives;
    }

    public static float TimeShiftInvariantDTW(List<float> a, List<float> b)
    {
        float bestDTW = float.PositiveInfinity;

        for (int shift = -25; shift <= 25; shift++)
        {
            var bS = ShiftSeries(b, shift);
            float dtwDistance = CalculateDTW(a, bS);

            if (dtwDistance < bestDTW)
                bestDTW = dtwDistance;
        }

        return bestDTW;
    }

    private static List<float> ShiftSeries(List<float> values, int shift)
    {
        List<float> shifted = new List<float>();
        for (int i = 0; i < values.Count(); i++)
        {
            int shiftedIndex = i + shift;
            shifted.Add((shiftedIndex >= 0 && shiftedIndex < values.Count) ? values[shiftedIndex]
                                                                           : float.PositiveInfinity);
        }
        return shifted;
    }

    public static float CalculateDTW(List<float> a, List<float> b)
    {
        int aC = a.Count;
        int bC = b.Count;

        float[,] dtw = new float[aC + 1, bC + 1];

        dtw[0, 0] = 0;

        for (int i = 1; i <= aC; i++)
        {
            for (int j = 1; j <= bC; j++)
            {
                dtw[i, j] = float.PositiveInfinity;

                float cost = Math.Abs(a[i - 1] - b[j - 1]);

                dtw[i, j] = cost + Math.Min(Math.Min(dtw[i - 1, j], dtw[i, j - 1]), dtw[i - 1, j - 1]);
            }
        }

        return dtw[aC, bC] /= (aC + bC);
    }
}
}
