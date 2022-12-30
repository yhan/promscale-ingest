using System.Diagnostics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace Common;

public class Generator
{
    private static string[] topLevelStrategyOptions = { "VWAP", "TWAP", "WVOL", "ECLIPSE", "WOULD" };
    private static string[] strategyOptions = { "Hit", "Quote", "DualQuote", "Fixing" };
    private static Random rand = new();
    private static string[] wayOptions = { "Buy", "Sell" };
    private static string[] instanceOptions = { "NOVENT01", "NOVENT02", "NOVENT03", "NOVENT50" };
    private static string[] venueOptions = { "ChiX", "ENX", "ENA-main", "GER-main", "Turquoise", "SGSI"};
    private static string[] counterpartyOptions = { "cli-a", "cli-b", "cli-c" };
    
    public readonly int NbDeals;
    private readonly DateTimeOffset startTime;
    private readonly DateTimeOffset endTime;
    private readonly int maxNominal;
    private int ingestedCount;
    private readonly double[] data;

    public Generator(int nbDeals): this(nbDeals, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30), DateTimeOffset.UtcNow)
    {
    }
    
    public Generator(int nbDeals, 
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        int maxNominal = 1000)
    {
        // this.backTo = backTo ?? TimeSpan.FromDays(15);
        this.NbDeals = nbDeals;
        this.startTime = startTime;
        this.endTime = endTime;
        
        this.maxNominal = maxNominal;

        this.data = new double[nbDeals];
        Beta.Samples(data, 0.5, 0.5);
        ConsoleHelper.DisplayHistogram(data);
    }

    public List<NominalMetric> Metrics()
    {
        var mkt = Execute();
        return mkt.MapToMetrics();
    }

    public List<MarketOrderVm> Execute()
    {
        var collection = new List<MarketOrderVm>();
        for (int i = 0; i < NbDeals; i++)
        {
            var marketOrderVm = OneMarketOrderVm();
            collection.Add(marketOrderVm);
        }

        return collection;
    }
    
    public MarketOrderVm OneMarketOrderVm()
    {
        double execNom = this.data[ingestedCount] * maxNominal;
        ingestedCount++;
        return new MarketOrderVm(
                        Guid.NewGuid().ToString(),
                        Select(topLevelStrategyOptions, 0),
                        Select(strategyOptions, 1, 2),
                        Select(wayOptions, 0),
                        execNom: execNom,
                        Select(instanceOptions),
                        Select(counterpartyOptions),
                        Select(Enum.GetValues<InstrumentType>(), 0),
                        Select(Enum.GetValues<VenueCategory>(), 0, 3),
                        Select(venueOptions, 0, 2),
                        Select(Enum.GetValues<VenueType>(), 2),
                        RandomDateTimeOffset());
    }

    public T Select<T>(T[] array, params int[] unfairIndexes)
    {
        if(unfairIndexes.Length == 0 || ingestedCount % 5 == 0) //only 20% using random
            return array[rand.Next(0, array.Length)];
        
        var index = RandomExcluding(array.Length, unfairIndexes); // 80% using random excluding
        return array[index];
    }
    
    public int RandomExcluding(int choicesCount, int[] preferIndex)
    {
        var range = Enumerable.Range(0, choicesCount).Where(i => !preferIndex.Contains(i));
        int index = rand.Next(0, choicesCount - preferIndex.Length);
        return range.ElementAt(index);
    }
    private DateTimeOffset RandomDateTimeOffset()
    {
        TimeSpan gap = endTime - startTime;
        var randomGapInSec = rand.Next(0, (int)gap.TotalSeconds);
        return startTime.AddSeconds(randomGapInSec);
    }
}

public enum InstrumentType
{
    Equity,
    Future,
    FutureSpread,
    ETF
}

public enum VenueCategory
{
    LIT,
    DARK,
    LIT_AUCTION,
    DAR_AUCTION
}

public enum VenueType
{
    Main,
    Secondary,
    InternalMarket,
    DarkPool
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
