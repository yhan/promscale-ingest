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

    public PromsScaleController(PromsScaleStat stat, IConfiguration config)
    {
        this.stat = stat;
        this.maxNominal = int.Parse(config["MaxNominal"]);
        this.nbDeals = int.Parse(config["NbDeals"]);
        
        this.startTime = DateTimeOffset.Parse(config["StartTime"]);
        this.endTime = DateTimeOffset.Parse(config["EndTime"]);
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
        for (int i = 0; i < generator.NbDeals; i++)
        {
            var metric = generator.OneMarketOrderVm().MapToMetric();
            var json = JsonConvert.SerializeObject(metric);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            stat.Resume();
            _ = await httpClient.PostAsync(url, data);
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

