const BeaconScanner = require('node-beacon-scanner');
const scanner = new BeaconScanner();
const date = require('date-and-time');

//Ready the Balena SDK
var balena = require('balena-sdk')({
	apiUrl: "https://api.balena-cloud.com/",
	dataDirectory: "/app/balena"
})
var key = process.env.BALENACLOUD_KEY;
balena.auth.loginWithToken(key);

//TODO - fail gracefully if something isn't configured or returns null
var deviceId = process.env.BALENA_DEVICE_UUID;
var deviceName = GetDeviceName(deviceId);
var rssiThreshold = process.env.RSSI_THRESHOLD;
var separationPeriod = process.env.SEP_PERIOD;
var influxHost = process.env.INFLUX_HOST;
var influxKey = process.env.INFLUX_KEY;
var influxBucket = process.env.INFLUX_BUCKET;
var influxOrg = process.env.INFLUX_ORG;

//Set the separation period
var separationPeriod = separationPeriod * 1000

//create a dictionary to track last sent datetime per tag
var lastSentDictionary = {}

//Ready the InfluxDB client
const Influxdb = require('influxdb-v2');
const influxdb = new Influxdb({
  host: influxHost,
  token: influxKey
});

// Set an Event handler for becons
scanner.onadvertisement = (ad) => {
  var tagId = ad.iBeacon.major + "-" + ad.iBeacon.minor;

    if(null != rssiThreshold && ad.rssi < rssiThreshold)
    {
      console.log("iBeacon for tag: " + tagId + " ignored because the RSSI was below the set threshold: " + rssiThreshold);
      return;
    }

    if(ad.rssi > -10)
    {
      console.log("Invalid beacon received and ignored");
      return;
    }

    if (tagId in lastSentDictionary)
    {
      //if this device has sent an iBeacon entry for this tag less than
      // 30 seconds ago, don't send another yet.
      var gap = (new Date) - lastSentDictionary[tagId];
      if (gap < separationPeriod)
      {
        console.log("iBeacon for tag: " + tagId + " ignored because it was reported only " + gap/1000 + "seconds ago.");
        return;
      }
    }

    //create the Influx data row from the beacon
    data = 'ibeacon,device=' + deviceId + ',deviceName=' + deviceName + ',tag=' + tagId + ' rssi=' + ad.rssi + ' ' + date
    console.log("Beacon: " + data);

    (async () => {
      await influxdb.write(
        {
          org: influxOrg,
          bucket: influxBucket
        },
        [{
          measurement: 'iBeacon',
          tags: {
            deviceId: deviceId,
            deviceName: deviceName,
            tagId: tagId
          },
          fields: {
            rssi: ad.rssi
          }
        }]
      );
    })().catch(error => {
      console.error('\n🐞 An error occurred!', error);
      process.exit(1);
    });

    lastSentDictionary[tagId] = new Date;
};

function GetDeviceName(deviceId)
{
  balena.models.device.tags.getAllByDevice(deviceId).then(function(tags) {
    if(null != tags)
    {
      tags.forEach(function(tag) {
        if(tag.tag_key == "Name")
        {
            deviceName = tag.value;
            console.log("Using the 'Name' tag for the device name.")
            console.log("Device name is " + tag.value);
            return;
        }
      });
    }
    else
    {
      balena.models.device.getName(deviceId).then(function(name) {
        
        console.log("No 'Name' tag set for this device so using "+ name + " instead");
        deviceName = name;
        return;
      }); 
        
    }
  })
}

// Start scanning for iBeacons
scanner.startScan().then(() => {
  console.log('Started to scan.')  ;
}).catch((error) => {
  console.error(error);
});