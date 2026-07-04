using System;
using System.Text.Json.Serialization;

namespace AppleJobGather
{
    public class HydrationEnvelope
    {
        [JsonPropertyName("loaderData")]
        public LoaderData? LoaderData { get; set; }
    }

    public class LoaderData
    {
        [JsonPropertyName("search")]
        public SearchData? Search { get; set; }

        [JsonPropertyName("jobDetails")]
        public JobDetailsData? JobDetails { get; set; }
    }

    public class SearchData
    {
        [JsonPropertyName("searchResults")]
        public SearchResult[]? SearchResults { get; set; }

        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }
    }

    public class SearchResult
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("positionId")]
        public string? PositionId { get; set; }

        [JsonPropertyName("postingTitle")]
        public string? PostingTitle { get; set; }

        [JsonPropertyName("jobSummary")]
        public string? JobSummary { get; set; }
    }

    public class JobDetailsData
    {
        [JsonPropertyName("jobsData")]
        public JobData? JobsData { get; set; }
    }

    public class JobData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("positionId")]
        public string? PositionId { get; set; }

        [JsonPropertyName("postingTitle")]
        public string? PostingTitle { get; set; }

        [JsonPropertyName("jobSummary")]
        public string? JobSummary { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("preferredQualifications")]
        public string? PreferredQualifications { get; set; }

        [JsonPropertyName("minimumQualifications")]
        public string? MinimumQualifications { get; set; }

        [JsonPropertyName("postingDate")]
        public string? PostingDate { get; set; }

        [JsonPropertyName("teamNames")]
        public string[]? TeamNames { get; set; }

        [JsonPropertyName("locations")]
        public LocationInfo[]? Locations { get; set; }
    }

    public class LocationInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("stateProvince")]
        public string? StateProvince { get; set; }

        [JsonPropertyName("countryName")]
        public string? CountryName { get; set; }
    }

    public class JobOutput
    {
        public string? JobId { get; set; }
        public string? JobName { get; set; }
        public string? JobSummary { get; set; }
        public string? Description { get; set; }
        public string? PreferredQualifications { get; set; }
        public string? MinimumQualifications { get; set; }
        public string? Team { get; set; }
        public string? Location { get; set; }
        public string? PostingDate { get; set; }
        public string? CrawlTime { get; set; }
        public string? SourceUrl { get; set; }
    }
}
