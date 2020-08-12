using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Core;
using NodaTime;
using System.Diagnostics;

namespace balenaLocatingDashboard.Model
{
    public class Beacon
    {
        public string BeaconId { get; set; }
        public string BeaconName { get; set; }
        public DateTime LastSeen { get; set; }
        public string DeviceName { get; set; }
        public double Strength { get; set; }
        public string DeviceUuid {get;set;}
    }
    public class BeaconsViewModel
    {
        private InfluxDBClient _influxDBClient;
        private readonly string _influxOrg;

        private readonly TagViewModel _tagViewModel;

        public BeaconsViewModel()
        {
            _tagViewModel = new TagViewModel();
            var influxHost = Environment.GetEnvironmentVariable("INFLUX_HOST");
            var influxKey = Environment.GetEnvironmentVariable("INFLUX_KEY");
            _influxOrg = Environment.GetEnvironmentVariable("INFLUX_ORG");

            if(null == influxHost || null == influxKey || null == _influxOrg )
            {
                Console.WriteLine("The necessary InfluxDB details have not been set in environment variables.");
                Console.WriteLine("Please see the project documentation for details: https://github.com/balenalabs-incubator/balenaLocating");
                return;
            }

            _influxDBClient = InfluxDBClientFactory.Create(influxHost,
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
                Debug.WriteLine("Influx exception: " + ex);
                return null;
            }

            tables.ForEach(table =>
             {
                 table.Records.ForEach(fluxRecord =>
                 {
                    if((double)fluxRecord.GetValue() <= (double)-1)
                    {
                        #region deviceId
                        var deviceIdRecord = fluxRecord.GetValueByKey("deviceId");
                        if(null == deviceIdRecord)
                        {
                            Debug.WriteLine("No deviceId found for a row in the InfluxDB, so skipping.");
                            Debug.WriteLine("Row timestamp = " + fluxRecord.GetTime());
                            return; //returns from the .foreach not the method
                        }
                        var deviceId = deviceIdRecord.ToString();
                        #endregion

                        #region tagId
                        var tagRecord = fluxRecord.GetValueByKey("tagId");
                        if(null == deviceIdRecord)
                        {
                            Debug.WriteLine("No tagId found for a row in the InfluxDB, so skipping.");
                            Debug.WriteLine("Row timestamp = " + fluxRecord.GetTime());
                            return; //returns from the .foreach not the method
                        }
                        var tagId = tagRecord.ToString();
                        #endregion

                        #region deviceName
                        var deviceNameRecord = fluxRecord.GetValueByKey("deviceName");
                        var deviceName = "unknown";
                        if(null != deviceNameRecord)
                        {
                            deviceName = deviceNameRecord.ToString();
                        }
                        #endregion

                        output.Add(new Beacon
                        {
                            Strength = (double)fluxRecord.GetValue(),
                            LastSeen = ((Instant)fluxRecord.GetTime()).ToDateTimeUtc(), //Is this the record time, rather than the beacon?!?
                            DeviceUuid = deviceId,
                            BeaconId = tagId,
                            BeaconName = _tagViewModel.GetTagName(tagId),
                            DeviceName = deviceName
                        });
                    }
                 });
             });

            return output;
        }
    }
}