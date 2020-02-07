using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Writes;
using NodaTime;

namespace balenaLocatingDashboard.Model
{
    public class Beacon
    {
        public string BeaconId { get; set; }
        public string BeaconName { get; set; }
        public DateTime LastSeen { get; set; }
        public string Location { get; set; }
        public double Strength { get; set; }
        public string DeviceName {get;set;}
    }
    public class BeaconsViewModel
    {
        private InfluxDBClient _influxDBClient;
        private readonly string _influxOrg;

        public BeaconsViewModel()
        {
            var influxHost = Environment.GetEnvironmentVariable("INFLUX_HOST");
            var influxKey = Environment.GetEnvironmentVariable("INFLUX_KEY");
            _influxOrg = Environment.GetEnvironmentVariable("INFLUX_ORG");

            _influxDBClient = InfluxDBClientFactory.Create("https://" + influxHost,
                influxKey.ToCharArray());
                _influxDBClient.SetLogLevel(LogLevel.Body);
        }

        public async Task<IList<Beacon>> GetLatest()
        {
            var output = new List<Beacon>();
            var flux = "from(bucket:\"balenaLocating\")"
            +" |> range(start: -48h)  "
            +" |> group(columns: [\"tagId\"])"
            +"|> sort(columns: [\"_time\"], desc: true)"
            +"|> first()  "
            +"|> yield(name: \"first\")";
            var queryApi = _influxDBClient.GetQueryApi();
            List<InfluxDB.Client.Core.Flux.Domain.FluxTable> tables;
            try
            {
                tables = await queryApi.QueryAsync(flux, _influxOrg);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Influx exception: " + ex);
                return null;
            }

            tables.ForEach(table =>
             {
                 table.Records.ForEach(fluxRecord =>
                 {
                    if((double)fluxRecord.GetValue() <= (double)-1)
                    {
                        var deviceId = fluxRecord.GetValueByKey("deviceId").ToString();
                        var tagId = fluxRecord.GetValueByKey("tagId").ToString();

                        output.Add(new Beacon
                        {
                            Strength = (double)fluxRecord.GetValue(),
                            LastSeen = ((Instant)fluxRecord.GetTime()).ToDateTimeUtc(),
                            DeviceName = deviceId,
                            BeaconId = tagId,
                            BeaconName = TagViewModel.GetTagName(tagId),
                            Location = DeviceViewModel.GetDeviceMapping(deviceId)
                        });
                    }
                 });
             });

            return output;
        }
    }
}