![balenaLocating](https://i.ibb.co/KXZJ8Fy/logo.jpg)

Use Raspberry Pi's and Bluetooth beacons to ensure you never lose your important stuff again. 
It gives you a web dashboard, viewable from anywhere, that shows you where all your stuff is:

![dashboard](https://i.ibb.co/XYbFgS3/dashboard.jpg)


If you want to read the detailed blogpost for this project, follow this [link](www.notreadyyet.com).

# Contents
* [Introduction](#introduction)
* [Hardware required](#hardware-required)
* [Software required](#software-required)
* [Setup the Application](#Setup-the-Application)
* [Configure](#Configure)

# Introduction
This project turns devices into Bluetooth Low Energy (BLE) sensors. By naming those devices to the location you put them in (e.g. house, office, garage) any BLE beacons detected can be related to the location. So attach BLE beacon tags to your stuff, and use this project to locate it!

# Hardware required
* Raspberry Pi (3B+ or 4) or a balenaFin
* A 4GB or greater micro SD Card (we always recommend 16Gb SanDisk Extreme Pro SD cards)
* Power supply
* Some BLE beacon tags or a smartphone app to make a virtual beacon for testing. 


# Software Required
* Software to flash an SD card (we recommend [balenaEtcher](etcher.io))
* A free tier balenaCloud account to setup and manage your fleet of Raspberry Pi sensors
* A free tier [InfluxDB](https://www.influxdata.com/products/influxdb-cloud/) Cloud account 

# Setup the Application
You can deploy this project to a new balenaCloud application in one click using the button below:
<br/><br/>
[![](https://balena.io/deploy.png)](https://dashboard.balena-cloud.com/deploy?repoUrl=https://github.com/balenalabs-incubator/balenaLocating)
<br/><br/>Or, you can create an application in your balenaCloud dashboard and balena push this code to it the traditional way.

# Configure
Now you need to update (or create if you used the `balena push` approach) the following environment variables:

* INFLUX_BUCKET - this is a bucket created in InfluxDB cloud
* INFLUX_HOST - this is the host URL from influxDB cloud,
* INFLUX_KEY - this is a key you need to create in influxDB cloud.
* INFLUX_ORG - this is the org ID from, yep you guessed it, influxDB cloud

You can also name your tags/beacons by setting some more environment variables in balenaCloud. Each one must start with the name ‘TAG_’ followed by the name of the thing:
`TAG_Wallet`
The value is the ID of the tag.

Setting an enviroment variable called `DEBUG`  to `true` if you need more output from the services, mainly to find out why/if a tag is being found and ignored, or just not found at all.

The variable `RSSI_THRESHOLD` can be set to a number between `-100` and `0`. The default is `-75`. It’s the threshold for the strength of signal received from a tag, filtering out weak signals. What this allows you to do is trim each sensor, so that their coverage plots don’t overlap.

Lastly you can set `SEP_PERIOD` to the number of seconds between reporting each tag beacon.


Again, if none of this make sense - check out the blog post with the full tutorial

