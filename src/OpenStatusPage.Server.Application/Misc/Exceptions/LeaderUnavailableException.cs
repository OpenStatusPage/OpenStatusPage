namespace OpenStatusPage.Server.Application.Misc.Exceptions
{
    [Serializable]
    public class LeaderUnavailableException : Exception
    {
        public LeaderUnavailableException() { }
        public LeaderUnavailableException(string message) : base(message) { }
        public LeaderUnavailableException(string message, Exception inner) : base(message, inner) { }
        protected LeaderUnavailableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
