using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Contexts
{
    public class FlightContext
    {
        [Display("Flight ID")]
        public string? FlightId { get; set; }

        [Display("Flight URL")]
        public string? FlightUrl { get; set; }

        [Display("Bird ID")]
        public string? BirdId { get; set; }

        [Display("Bird name")]
        public string? BirdName { get; set; }

        [Display("Nest ID")]
        public string? NestId { get; set; }

        [Display("Nest name")]
        public string? NestName { get; set; }
    }
}
