using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace balenaLocatingDashboard.Model
{
    public static class TagViewModel
    {
        public static List<Tag> GetTagNames()
        {
            List<Tag> output = new List<Tag>();
            var tagNames = Environment.GetEnvironmentVariable("TAG_NAMES");
            if(tagNames != null)
            {
                var mappings = tagNames.Split(',');
                foreach(var mapping in mappings)
                {
                    var parts = mapping.Split(':');
                    output.Add(new Tag
                    {
                        Id = parts[0],
                        Name = parts[1]
                    });
                }
            }
            else
            {
                Debug.WriteLine("Environment Variable was null");
            }

            return output;
        }

        public static string GetTagName(string tagId)
        {
            var tagNames = GetTagNames();
            var mapping = (tagNames.Where(d => d.Id == tagId).FirstOrDefault());

            if(null == mapping)
            {
                return null;
            }

            return mapping.Name;
        }
    }

    public class Tag
    {
          public string Id {get; set;}
        public string Name {get; set;}
        public DateTime LastSeen {get;set;}
        public string Location {get;set;}
    }
}