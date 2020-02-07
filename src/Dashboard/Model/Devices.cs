using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace balenaLocatingDashboard.Model
{
    public static class DeviceViewModel
    {
        public static List<Device> GetMappedDevices()
        {
            List<Device> output = new List<Device>();
            var deviceLocations = Environment.GetEnvironmentVariable("DEVICE_LOCATIONS");
            if(deviceLocations != null)
            {
                var mappings = deviceLocations.Split(',');
                foreach(var mapping in mappings)
                {
                    var parts = mapping.Split(':');
                    output.Add(new Device
                    {
                        Name = parts[0],
                        Location = parts[1]
                    });
                }
            }
            else
            {
                Console.WriteLine("Environment Variable was null");
            }

            return output;
        }

        public static string GetDeviceMapping(string deviceName)
        {
            var deviceLocations = GetMappedDevices();
            var mapping = (deviceLocations.Where(d => d.Name == deviceName).FirstOrDefault());

            return mapping.Location;
        }
    }

    public class Device
    {
          public string Name {get; set;}
        public string Location {get; set;}
    }
}