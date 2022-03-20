namespace OpenStatusPage.Server.Application.Misc.Exceptions
{
    [Serializable]
    public class FinalFailureException : Exception
    {
        public FinalFailureException() { }
        public FinalFailureException(string message) : base(message) { }
        public FinalFailureException(string message, Exception inner) : base(message, inner) { }
        protected FinalFailureException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
