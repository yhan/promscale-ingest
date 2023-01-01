using MathNet.Numerics.Statistics;

namespace Common;

public static class App
{
    public static bool IsRunningInUnitTest { get; set; }
}

/// <summary>
/// Helper functions to output into Console window
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Display histogram from the array
    /// </summary>
    /// <param name="data">Source array</param>
    public static void DisplayHistogram(IEnumerable<double> data)
    {
        var blockSymbol = Convert.ToChar(9608);

        var rowMaxLength = Console.WindowWidth - 1;
        rowMaxLength = (rowMaxLength / 10) * 10;
        var rowCount = rowMaxLength / 3;

        var histogram = new Histogram(data, rowMaxLength);

        // Find the absolute peak
        var maxBucketCount = 0.0;
        for (var i = 0; i < histogram.BucketCount; i++)
        {
            if (histogram[i].Count > maxBucketCount)
            {
                maxBucketCount = histogram[i].Count;
            }
        }

        // Number of bucket counts between rows
        var rowStep = maxBucketCount / rowCount;

        // Draw histogram line-by-line
        Console.WriteLine();

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < histogram.BucketCount; col++)
            {
                if (histogram[col].Count >= maxBucketCount)
                {
                    Console.Write(blockSymbol);
                }
                else
                {
                    Console.Write(@" ");
                }
            }

            Console.SetCursorPosition(0, Console.CursorTop + 1);
            maxBucketCount -= rowStep;
        }

        // Calculate distanse between label in X axis
        var axisStep = histogram.BucketCount / 2;

        var leftLabel = histogram.LowerBound.ToString("N");
        var middleLabel = ((histogram.UpperBound + histogram.LowerBound) / 2.0).ToString("N");
        var rightLabel = histogram.UpperBound.ToString("N");

        Console.Write(leftLabel);
        for (var j = 0; j < axisStep - leftLabel.Length; j++)
        {
            Console.Write(@" ");
        }

        Console.Write(middleLabel);
        for (var j = 0; j < axisStep - middleLabel.Length; j++)
        {
            Console.Write(@" ");
        }

        Console.Write(rightLabel);

        Console.WriteLine();
    }

    /// <summary>
    /// Display histogram from the array
    /// </summary>
    /// <param name="data">Source array</param>
    public static void DisplayHistogram(IEnumerable<int> data)
    {
        DisplayHistogram(data.Select(x => (double)x));
    }
}
