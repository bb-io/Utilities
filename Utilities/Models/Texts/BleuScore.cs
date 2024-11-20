using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities.Models.Texts
{
    public class BleuScore
    {
        [Display("BLEU score", Description = "The score of the calculation")]
        public double Score { get; set; }
    }
}
