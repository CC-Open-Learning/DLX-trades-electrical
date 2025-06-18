using System;

namespace VARLab.TradesElectrical
{
    public struct RoughInFeedbackInformation : IEquatable<RoughInFeedbackInformation>
    {
        public Task Task  { get; set; }
        public string CodeViolation  { get; set; }
        public string FeedbackDescription  { get; set; }

        public bool Equals(RoughInFeedbackInformation other)
        {
            return Task == other.Task &&
                   CodeViolation == other.CodeViolation &&
                   FeedbackDescription == other.FeedbackDescription;
        }
    }
}