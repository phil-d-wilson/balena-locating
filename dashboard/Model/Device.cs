using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Core;
using NodaTime;
using System.Diagnostics;

namespace balenaLocatingDashboard.Model
{
    public class DeviceViewModel
    {
        private InfluxDBClient _influxDBClient;
        private readonly string _influxOrg;
        private readonly string _influxBucket;

        private readonly TagViewModel _tagViewModel;

        public DeviceViewModel()
        {
            _tagViewModel = new TagViewModel();
            var influxHost = Environment.GetEnvironmentVariable("INFLUX_HOST");
            var influxKey = Environment.GetEnvironmentVariable("INFLUX_KEY");
            _influxOrg = Environment.GetEnvironmentVariable("INFLUX_ORG");
            _influxBucket = Environment.GetEnvironmentVariable("INFLUX_BUCKET");

            if (null == influxHost || null == influxKey || null == _influxOrg || null == _influxBucket)
            {
                Console.WriteLine("The necessary InfluxDB details have not been set in environment variables.");
                Console.WriteLine("Please see the project documentation for details: https://github.com/balenalabs-incubator/balenaLocating");
                return;
            }

            try
            {
                _influxDBClient = InfluxDBClientFactory.Create(influxHost,
                influxKey.ToCharArray());
                _influxDBClient.SetLogLevel(LogLevel.Body);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Influx exception: " + ex);
                return;
            }
        }

        public async Task<Dictionary<string, List<Beacon>>> GetData()
        {
            var output = new Dictionary<string, List<Beacon>>();

            var flux = "from(bucket:\"" + _influxBucket + "\")"
            + " |> range(start: -48h)  "
            + " |> group(columns: [\"deviceId\", \"tagId\"])"
            + "|> sort(columns: [\"_time\"], desc: true)"
            + "|> first()  "
            + "|> yield(name: \"first\")";

            List<InfluxDB.Client.Core.Flux.Domain.FluxTable> tables;
            try
            {
                var queryApi = _influxDBClient.GetQueryApi();
                tables = await queryApi.QueryAsync(flux, _influxOrg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Influx exception: " + ex);
                return null;
            }

            tables.ForEach(table =>
             {
                 table.Records.ForEach(fluxRecord =>
                 {
                     var deviceIdRecord = fluxRecord.GetValueByKey("deviceId");
                     var deviceNameRecord = fluxRecord.GetValueByKey("deviceName");
                     var tagIdRecord = fluxRecord.GetValueByKey("tagId");
                     var strengthRecord = (double)fluxRecord.GetValue();
                     var lastSeenRecord = ((Instant)fluxRecord.GetTime()).ToDateTimeUtc();

                     #region device
                     if (null == deviceIdRecord)
                     {
                         Debug.WriteLine("No deviceId found for a row in the InfluxDB, so skipping.");
                         Debug.WriteLine("Row timestamp = " + fluxRecord.GetTime());
                         return; //returns from the .foreach not the method
                     }
                     var deviceId = deviceIdRecord.ToString();

                     var deviceName = "unknown";
                     if (null != deviceNameRecord)
                     {
                         deviceName = deviceNameRecord.ToString();
                     }
                     #endregion

                     #region tagId
                     var tagRecord = fluxRecord.GetValueByKey("tagId");
                     if (null == deviceIdRecord)
                     {
                         Debug.WriteLine("No tagId found for a row in the InfluxDB, so skipping.");
                         Debug.WriteLine("Row timestamp = " + fluxRecord.GetTime());
                         return; //returns from the .foreach not the method
                     }
                     var tagId = tagRecord.ToString();
                     #endregion

                     if (!output.ContainsKey(deviceId))
                     {
                         output.Add(deviceId, new List<Beacon>());
                     }

                     var beaconName = _tagViewModel.GetTagName(tagId);
                     if (string.IsNullOrEmpty(beaconName))
                     {
                         beaconName = tagId;
                     }

                     output[deviceId].Add(new Beacon
                     {
                         BeaconId = tagId,
                         BeaconName = beaconName,
                         DeviceName = deviceName,
                         DeviceUuid = deviceId,
                         LastSeen = lastSeenRecord,
                         Strength = strengthRecord
                     });
                 });
             });

            return output;
        }
    }
}