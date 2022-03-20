namespace OpenStatusPage.Shared.Requests
{
    public class SuccessResponse
    {
        public bool WasSuccessful { get; set; }

        public static SuccessResponse FromSuccess => new() { WasSuccessful = true };

        public static SuccessResponse FromFailure => new() { WasSuccessful = false };
    }
}
