namespace RouteWise.Exceptions
{
    public class FlightSearchException : Exception
    {
        public FlightSearchException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightSearchException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public FlightSearchException(string message): base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlightSearchException"/> class with a specified error message.
        /// Enables setting of the inner exception.
        /// </summary>
        /// <param name="message">TThe message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// </param>
        public FlightSearchException(string message, Exception innerException) : base(message, innerException) { }
    }
}
