using System.Diagnostics;
using MathNet.Numerics.Distributions;

namespace Common;

public class Generator
{
    private static string[] topLevelStrategyOptions =
    {
        "VWAP", "TWAP", "WVOL", "ECLIPSE", "WOULD"
    };
    private static string[] strategyOptions =
    {
        "Hit", "Quote", "DualQuote", "Fixing"
    };
    private static Random rand = new();
    private static string[] wayOptions =
    {
        "Buy", "Sell"
    };
    private static string[] instanceOptions =
    {
        "NOVENT01", "NOVENT02", "NOVENT03", "NOVENT50"
    };
    private static string[] venueOptions =
    {
        "ChiX", "ENX", "ENA-main", "GER-main", "Turquoise", "SGSI"
    };
    private static string[] counterpartyOptions =
    {
        "cli-a", "cli-b", "cli-c"
    };
    private static string[] instrOptions =
    {
        "AMZN", "NFLX", "AAPL", "AMD", "TSLA", "DIS", "BABA", "JPM", "TSLA", "MO", "GOOG"
    };

    public readonly int NbDeals;
    private readonly DateTimeOffset startTime;
    private readonly DateTimeOffset endTime;
    private readonly int maxNominal;
    private int ingestedCount;
    private readonly double[] data;
    private readonly string orderIdFormat;
    public readonly string[] orderIds;

    public Generator(int nbDeals) : this(nbDeals, DateTimeOffset.UtcNow - TimeSpan.FromMinutes(30), DateTimeOffset.UtcNow) {}

    public Generator(int nbDeals,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        int maxNominal = 1000)
    {
        // this.backTo = backTo ?? TimeSpan.FromDays(15);
        this.NbDeals = nbDeals;
        orderIdFormat = string.Join("", Enumerable.Repeat("0", (int)Math.Floor(Math.Log10(nbDeals) + 1)));
        orderIds = BuildOrderIds(nbDeals);

        this.startTime = startTime;
        this.endTime = endTime;

        this.maxNominal = maxNominal;

        this.data = new double[nbDeals];
        Beta.Samples(data, 0.5, 0.5);
        if(!App.IsRunningInUnitTest)
            ConsoleHelper.DisplayHistogram(data);
    }
    private string[] BuildOrderIds(int nbDeals)
    {
        // 1000 deals, only 0.1% of distinct orders = 10 orders
        var nbOrders = (int)(nbDeals * 0.001);
        if (nbOrders < 1)
        {
            throw new ArgumentException("At least 1000 deals required. Hint: config::NbDeals");
        }
        return Enumerable.Range(1, nbOrders).Select(x => $"ORL{PadLeadingWithZero(x)}").ToArray();
    }

    private string PadLeadingWithZero(int val)
    {
        return val.ToString(orderIdFormat);
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
        var orderId = this.orderIds[rand.Next(0, orderIds.Length)];
        ingestedCount++;
        return new MarketOrderVm(
        Guid.NewGuid().ToString(),
        orderId,
        instrId: Select(instrOptions),
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
        if (unfairIndexes.Length == 0 || ingestedCount % 5 == 0)//only 20% using random
            return array[rand.Next(0, array.Length)];

        var index = RandomExcluding(array.Length, unfairIndexes);// 80% using random excluding
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