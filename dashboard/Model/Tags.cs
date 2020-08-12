using System;
using System.Linq;
using System.Collections.Generic;

namespace balenaLocatingDashboard.Model
{
    public class TagViewModel
    {
        List<Tag> _tagNames;

        public TagViewModel()
        {
            _tagNames = GetTagNames();
        }

        public List<Tag> GetTagNames()
        {
            List<Tag> output = new List<Tag>();
            var enVariables = Environment.GetEnvironmentVariables();
            foreach(System.Collections.DictionaryEntry enVar in enVariables)
            {
                if (enVar.Key.ToString().StartsWith("TAG_"))
                {
                    output.Add(new Tag
                    {
                        Id = enVar.Value.ToString(),
                        Name = enVar.Key.ToString().Replace("TAG_","")
                    });
                }
            }

            return output;
        }

        public string GetTagName(string tagId)
        {
            var mapping = (_tagNames.Where(d => d.Id == tagId).FirstOrDefault());

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