using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace Demo
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5)]
    public class JsonSerializerPerf_LoginViewModel
    {
        private static readonly LoginViewModel _model = CreateLoginViewModel();
        private static readonly string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(_model);

        //[Benchmark(Baseline = true)]
        public string NewtonsoftSerialize()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(_model);
        }

        [Benchmark(Baseline = true)]
        public LoginViewModel NewtonsoftDeserialize()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<LoginViewModel>(jsonString);
        }

        //[Benchmark]
        public string TextJsonSerialize()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToString(_model, typeof(LoginViewModel));
        }

        [Benchmark]
        public LoginViewModel TextJsonDeserialize()
        {
            return System.Text.Json.Serialization.JsonSerializer.Parse<LoginViewModel>(jsonString);
        }

        private static LoginViewModel CreateLoginViewModel()
            => new LoginViewModel
            {
                Email = "name.familyname@not.com",
                Password = "abcdefgh123456!@",
                RememberMe = true
            };
    }

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5)]
    public class JsonSerializerPerf_Location
    {
        private static readonly Location _model = CreateLocation();
        private static readonly string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(_model);

        //[Benchmark(Baseline = true)]
        public string NewtonsoftSerialize()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(_model);
        }

        [Benchmark(Baseline = true)]
        public Location NewtonsoftDeserialize()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Location>(jsonString);
        }

        //[Benchmark]
        public string TextJsonSerialize()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToString(_model, typeof(Location));
        }

        [Benchmark]
        public Location TextJsonDeserialize()
        {
            return System.Text.Json.Serialization.JsonSerializer.Parse<Location>(jsonString);
        }

        private static Location CreateLocation()
            => new Location
            {
                Id = 1234,
                Address1 = "The Street Name",
                Address2 = "20/11",
                City = "The City",
                State = "The State",
                PostalCode = "abc-12",
                Name = "Nonexisting",
                PhoneNumber = "+0 11 222 333 44",
                Country = "The Greatest"
            };
    }

    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 5)]
    public class JsonSerializerPerf_IndexViewModel
    {
        private static readonly IndexViewModel _model = CreateIndexViewModel();
        private static readonly string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(_model);

        //[Benchmark(Baseline = true)]
        public string NewtonsoftSerialize()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(_model);
        }

        [Benchmark(Baseline = true)]
        public IndexViewModel NewtonsoftDeserialize()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<IndexViewModel>(jsonString);
        }

        //[Benchmark]
        public string TextJsonSerialize()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToString(_model, typeof(IndexViewModel));
        }

        [Benchmark]
        public IndexViewModel TextJsonDeserialize()
        {
            return System.Text.Json.Serialization.JsonSerializer.Parse<IndexViewModel>(jsonString);
        }

        private static IndexViewModel CreateIndexViewModel()
        {
            var events = new List<ActiveOrUpcomingEvent>();
            for (int i = 0; i < 20; i++)
            {
                events.Add(new ActiveOrUpcomingEvent
                {
                    Id = 10,
                    CampaignManagedOrganizerName = "Name FamiltyName",
                    CampaignName = "The very new campaing",
                    Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                    //EndDate = DateTime.UtcNow.AddYears(1),
                    Name = "Just a name",
                    ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                    //StartDate = DateTime.UtcNow
                });
            }

            return new IndexViewModel
            {
                IsNewAccount = false,
                FeaturedCampaign = new CampaignSummaryViewModel
                {
                    Description = "Very nice campaing",
                    Headline = "The Headline",
                    Id = 234235,
                    OrganizationName = "The Company XYZ",
                    ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                    Title = "Promoting Open Source"
                },
                ActiveOrUpcomingEvents = events
            };
        }
    }

    [Serializable]
    public class LoginViewModel
    {
        public virtual string Email { get; set; }
        public virtual string Password { get; set; }
        public virtual bool RememberMe { get; set; }
    }

    [Serializable]
    public class Location
    {
        public virtual int Id { get; set; }
        public virtual string Address1 { get; set; }
        public virtual string Address2 { get; set; }
        public virtual string City { get; set; }
        public virtual string State { get; set; }
        public virtual string PostalCode { get; set; }
        public virtual string Name { get; set; }
        public virtual string PhoneNumber { get; set; }
        public virtual string Country { get; set; }
    }

    [Serializable]
    public class ActiveOrUpcomingEvent
    {
        public virtual int Id { get; set; }
        public virtual string ImageUrl { get; set; }
        public virtual string Name { get; set; }
        public virtual string CampaignName { get; set; }
        public virtual string CampaignManagedOrganizerName { get; set; }
        public virtual string Description { get; set; }
        //public virtual DateTime StartDate { get; set; }
        //public virtual DateTime EndDate { get; set; }
    }

    [Serializable]
    public class CampaignSummaryViewModel
    {
        public virtual int Id { get; set; }
        public virtual string Title { get; set; }
        public virtual string Description { get; set; }
        public virtual string ImageUrl { get; set; }
        public virtual string OrganizationName { get; set; }
        public virtual string Headline { get; set; }
    }

    [Serializable]
    public class IndexViewModel
    {
        public virtual List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
        public virtual CampaignSummaryViewModel FeaturedCampaign { get; set; }
        public virtual bool IsNewAccount { get; set; }
        public bool HasFeaturedCampaign => FeaturedCampaign != null;
    }
}
