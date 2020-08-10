const BeaconScanner = require('node-beacon-scanner');
const scanner = new BeaconScanner();
const date = require('date-and-time');

let debug = false;

let deviceId = process.env.BALENA_DEVICE_UUID;
let deviceName = process.env.BALENA_DEVICE_NAME_AT_INIT;
let rssiThreshold = process.env.RSSI_THRESHOLD || -75;
let separationPeriod = process.env.SEP_PERIOD || 30;
let influxHost = process.env.INFLUX_HOST;
let influxKey = process.env.INFLUX_KEY;
let influxBucket = process.env.INFLUX_BUCKET;
let influxOrg = process.env.INFLUX_ORG;
if (![deviceId, influxHost, influxBucket, influxKey, influxOrg].every(Boolean)) {
  throw new exc("Please check that all environment variables are configured. Exiting.");
}


let debugSetting = process.env.DEBUG || "false";
if (debugSetting.toLowerCase() == "true") {
  debug = true;
}

influxHost = influxHost.replace("https://", "");

//Set the separation period
separationPeriod = separationPeriod * 1000;

//create a dictionary to track last sent datetime per tag
let lastSentDictionary = {};

//Ready the InfluxDB client
const Influxdb = require('influxdb-v2');
const influxdb = new Influxdb({
  host: influxHost,
  token: influxKey
});

// Set an Event handler for becons
scanner.onadvertisement = (ad) => {

  let tagId = ad.address

  if (ad.rssi > -10) {
    if (debug) { console.log("Invalid beacon received: " + ad.address + " and ignored"); }
    return;
  }

  if (ad.beaconType == "iBeacon") {
    if (ad.iBeacon.major == 0 || ad.iBeacon.minor == 0) {
      if (debug) { console.log("Beacon with invalid UUID/major/minor found. Ignoring") }
      return;
    }

    //TODO: Remove this when the ghost beacons issue is resolved
    if (ad.iBeacon.minor != 139 && ad.iBeacon.minor != 1218 && ad.iBeacon.minor != 6) {
      if (debug) {
        console.log("Ad: " + objToString(ad))
        console.log("iBeacon: " + objToString(ad.iBeacon))
      }
    }

    tagId = ad.iBeacon.uuid + "-" + ad.iBeacon.major + "-" + ad.iBeacon.minor;
  }
  else if (ad.beaconType == "eddystoneUid") {
    if (debug) {
      console.log("Ad: " + objToString(ad))
      console.log("EddystoneUid: " + objToString(ad.eddystoneUid))
    }
    tagId = ad.eddystoneUid.namespace + "-" + ad.eddystoneUid.instance;
  }
  else if (ad.beaconType == "eddystoneTlm") {
    if (debug) {
      console.log("Ad: " + objToString(ad))
      console.log("eddystoneTlm: " + objToString(ad.eddystoneTlm))
      console.log("Eddystone TLM beacons are not supported. Ignoring")
    }
    return;
  }
  else if (ad.beaconType == "eddystoneUrl") {
    if (debug) {
      console.log("Ad: " + objToString(ad))
      console.log("eddystoneUrl: " + objToString(ad.eddystoneUrl))
      console.log("Eddystone URL beacons are not supported. Ignoring")
    }
    return;
  }
  else {
    if (debug) {
      console.log("Other type of advertisement packet recieved. Currently not supported. Ignoring:")
      console.log(objToString(ad))
    }
    return;
  }



  if (null != rssiThreshold && ad.rssi < rssiThreshold) {
    if (debug) { console.log("Beacon for tag: " + tagId + " ignored because the RSSI was below the set threshold: " + rssiThreshold) }
    return;
  }

  if (tagId in lastSentDictionary) {
    //if this device has sent an iBeacon entry for this tag less than
    // 30 seconds ago, don't send another yet.
    let gap = (new Date) - lastSentDictionary[tagId];
    if (gap < separationPeriod) {
      if (debug) { console.log("Beacon for tag: " + tagId + " ignored because it was reported only " + gap / 1000 + "seconds ago.") }
      return;
    }
  }

  //create the Influx data row from the beacon
  data = 'beacon,device=' + deviceId + ',deviceName=' + deviceName + ',tag=' + tagId + ' rssi=' + ad.rssi
  console.log("Beacon: " + data);

  (async () => {
    await influxdb.write(
      {
        org: influxOrg,
        bucket: influxBucket
      },
      [{
        measurement: 'Beacon',
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

function objToString(obj) {
  let str = '';
  for (let p in obj) {
    if (obj.hasOwnProperty(p)) {
      str += p + '::' + obj[p] + '\n';
    }
  }
  return str;
}

// Start scanning for iBeacons
scanner.startScan().then(() => {
  console.log('Started to scan.');
}).catch((error) => {
  console.error(error);
});