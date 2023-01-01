namespace Common;

public static class MapExtensions
{
    public static List<NominalMetric> MapToMetrics(this List<MarketOrderVm> mkt)
    {
        return mkt.Select(MapToMetric).ToList();
    }

    public static NominalMetric MapToMetric(this MarketOrderVm x)
    {
        var nm = new NominalMetric
        {
            labels = new lables
            {
                __name__ = "Nominal",
                Id = x.Id,
                OrderId = x.OrderId,
                InstrumentType = x.InstrumentType.ToString(),
                Counterparty = x.Counterparty,
                VenueCategory = x.VenueCategory.ToString(),
                Way = x.Way,
                StrategyName = x.StrategyName,
                VenueType = x.VenueType.ToString(),
                Venue = x.VenueId,
                TopLevelStrategyName = x.TopLevelStrategyName
            },
            samples = new[]
            {
                new[]
                {
                    (long)x.Timestamp.ToUnixTimeMilliseconds(), x.ExecNom
                }
            }
        };

        return nm;
    }
}
