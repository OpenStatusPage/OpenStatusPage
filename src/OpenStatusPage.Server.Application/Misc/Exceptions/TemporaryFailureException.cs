namespace OpenStatusPage.Server.Application.Misc.Exceptions
{
    [Serializable]
    public class TemporaryFailureException : Exception
    {
        public TemporaryFailureException() { }
        public TemporaryFailureException(string message) : base(message) { }
        public TemporaryFailureException(string message, Exception inner) : base(message, inner) { }
        protected TemporaryFailureException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
