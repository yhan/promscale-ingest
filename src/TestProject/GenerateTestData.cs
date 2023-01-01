using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using Common;
using CsvHelper;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using NFluent;

namespace TestProject;

public class GenerateTestData
{
    [Test]
    public void GenerateCSV()
    {
        var generator = new Generator(1_000_000);
        List<MarketOrderVm> marketOrderVms = generator.Execute();
        using (var writer = new StreamWriter(@"mkt-time.csv"))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.Context.RegisterClassMap<MarketOrderVmMap>();
            csv.WriteRecords(marketOrderVms);
        }
    }

    [Test]
    public void GeneratePromScaleMetric()
    {
        Generator generator = new Generator(1);
        var metrics = generator.Metrics();
        TestContext.WriteLine(JsonConvert.SerializeObject(metrics, Formatting.Indented));
    }

    [Test]
    public async Task PostToPromScale()
    {
        Generator generator = new Generator(1 * 1000 * 10);
        
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9201")
        };
        var url = new Uri("http://localhost:9201/write");
        long sumMs = 0;
        var sw = new Stopwatch();
        for (int i = 0; i < generator.NbDeals; i++)
        {
            var metric = generator.OneMarketOrderVm().MapToMetric();
            var json = JsonConvert.SerializeObject(metric);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            
            sw.Start();
            _ = await httpClient.PostAsync(url, data);
            sumMs += sw.ElapsedMilliseconds;
            sw.Reset();
        }
    }

    [Test]
    public void ShowESTimestamp()
    {
        TestContext.WriteLine(DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'hhmmssZ"));
    }

    [Test]
    public async Task InsertAll()
    {
        var generator = new Generator(1_000_000);
        List<MarketOrderVm> marketOrderVms = generator.Execute();

        await using var dbContext = new MarketOrdersContext();
        var sw = Stopwatch.StartNew();
        await dbContext.BulkInsertAsync(marketOrderVms);
        TestContext.WriteLine($"Insert All took {sw.Elapsed}");
    }
    
    [Test]
    public async Task DebugTempTableNotDropped()
    {
        var bulkConfig = new BulkConfig
        {
            //PreserveInsertOrder = true, // true is default
            SetOutputIdentity = true,
            //BatchSize = 4000,
            UseTempDB = true,
            //CalculateStats = true
        };
        
        var generator = new Generator(100);
        while(true)
        {
            List<MarketOrderVm> marketOrderVms = generator.Execute();
            await using var dbContext = new MarketOrdersContext();
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
           
            await dbContext.BulkInsertAsync(marketOrderVms, bulkConfig);
            await transaction.CommitAsync();

            await Task.Delay(2000);
        }
    }
    

    [Test]
    public async Task ArchiveWithRawSql()
    {
        await using var dbContext = new MarketOrdersContext();
        var sw = Stopwatch.StartNew();
        var lines =  await dbContext.Database.ExecuteSqlRawAsync(
        @"truncate table histo.""MarketOrderVms"";
          insert into histo.""MarketOrderVms"" select * from daily.""MarketOrderVms"";"); //19s

        TestContext.WriteLine($"Archive {lines} lines took {sw.Elapsed}");
    }

    [Test]
    public async Task ArchiveWithFile()
    {
        await using var dbContext = new MarketOrdersContext();
        var sw = Stopwatch.StartNew();
        dbContext.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        var lines = await dbContext.Database.ExecuteSqlRawAsync(
        
        @"truncate table histo.""MarketOrderVms"";
          
          -- copy table data to file
          copy (select * from daily.""MarketOrderVms"") to 'c:\yi\data\MarketOrderVms.copy';
          
          -- copy data from file to table
          copy histo.""MarketOrderVms"" from 'c:\yi\data\MarketOrderVms.copy';");

        TestContext.WriteLine($"Archive {lines} lines took {sw.Elapsed}"); // 00:00:59.0979567
    }

    [Test]
    public void TestRandomExcluding()
    {
        var generator = new Generator(1);
        int i = 0;
        while (i< 100)
        {
            TestContext.WriteLine(generator.RandomExcluding(4,
            new[]
            {
                0, 2
            }));
            i++;
        }
    }

    [Test]
    public void TestSelectNoPreference()
    {
        var generator = new Generator(1);
        _ = generator.Select(Enumerable.Range(0, 100).ToArray());
    }

    [TestCase(500000)]
    [TestCase(1000)]
    public void OrderIds(int size)
    {
        var gen = new Generator(size);
        Check.That(gen.OrderIds).HasSize((int)(size * 0.001));
        foreach (var orderId in gen.OrderIds)
        {
            TestContext.WriteLine(orderId);
        }
    }

    [Test]
    public void NotEnoughDeals()
    {
        int size = 999;
        Check.ThatCode(() => _ = new Generator(size)).Throws<ArgumentException>();
    }
}

[SetUpFixture]
public class AssemblyInitializer
{
    [OneTimeSetUp] 
    public void Initialize()
    {
        App.IsRunningInUnitTest = true;
    }
}