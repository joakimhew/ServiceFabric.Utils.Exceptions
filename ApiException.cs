using System;

namespace ServiceFabric.Utils.Exceptions
{

    /// <summary>
    ///  Used to construct a simple API exception message
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="ApiException"/>
        /// </summary>
        /// <param name="message">The message to pass into <see cref="Exception"/></param>
        /// <param name="stackTrace">Sets the <see cref="StackTrace"/></param>
        public ApiException(string message, string stackTrace = null)
            : base(message)
        {
            StackTrace = stackTrace;
        }

        /// <summary>
        /// Stacktrace to the current exception
        /// </summary>
        public override string StackTrace { get; }
    }
}