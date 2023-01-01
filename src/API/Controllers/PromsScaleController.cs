using System.Diagnostics;
using System.Text;
using API.Functions;
using Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class PromsScaleController : ControllerBase
{
    private readonly PromsScaleStat stat;
    private readonly int maxNominal;
    private readonly int nbDeals;
    private readonly DateTimeOffset startTime;
    private readonly DateTimeOffset endTime;
    private readonly int ingestBatchSize;

    public PromsScaleController(PromsScaleStat stat, IConfiguration config)
    {
        this.stat = stat;
        this.maxNominal = int.Parse(config["MaxNominal"]);
        this.nbDeals = int.Parse(config["NbDeals"]);
        
        this.startTime = DateTimeOffset.Parse(config["StartTime"]);
        this.endTime = DateTimeOffset.Parse(config["EndTime"]);

        this.ingestBatchSize = int.Parse(config["IngestBatchSize"]);
    }

    [HttpPost("AppendDeals")]
    public async Task AppendDeals()
    {
        Generator generator = new Generator(nbDeals: nbDeals, startTime, endTime, maxNominal: maxNominal);

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9201")
        };
        var url = new Uri("http://localhost:9201/write");
        
        var sw = new Stopwatch();
        var jsb = new StringBuilder();
        for (int i = 0; i < generator.NbDeals; i++)
        {
            var metric = generator.OneMarketOrderVm().MapToMetric();
            jsb.AppendLine(JsonConvert.SerializeObject(metric));
            stat.Resume();
            if((i != 0 && i % ingestBatchSize == 0) || i+1 == nbDeals)
            {
                var data = new StringContent(jsb.ToString(), Encoding.UTF8, "application/json");
                _ = await httpClient.PostAsync(url, data);
                jsb.Clear();
            }
            stat.RecordThenSuspend();
           
            sw.Reset();
        }
    }

    [HttpGet("Stat")]
    public dynamic Stat()
    {
        return stat.Value();
    }
}

