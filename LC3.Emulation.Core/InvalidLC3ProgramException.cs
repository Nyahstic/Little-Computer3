using System;
using System.Runtime.Serialization;

namespace LC3.Emulation.Core
{
    /// <summary>
    /// Exception thrown when an LC3 program is considered invalid within the emulation context.
    /// This version includes the processor context for debugging purposes.
    /// </summary>
    [Serializable]
    public class InvalidLC3ProgramException : Exception
    {
        /// <summary>
        /// Gets the processor instance that was running when the exception occurred.
        /// </summary>
        public ProcessorUnprotected? ProcessorContext { get; }

        public InvalidLC3ProgramException() { }

        public InvalidLC3ProgramException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance with a message and the processor context.
        /// </summary>
        public InvalidLC3ProgramException(string message, ProcessorUnprotected context) : base(message)
        {
            ProcessorContext = context;
        }

        public InvalidLC3ProgramException(string message, Exception inner) : base(message, inner) { }

        public InvalidLC3ProgramException(string message, ProcessorUnprotected context, Exception inner) : base(message, inner)
        {
            ProcessorContext = context;
        }

        protected InvalidLC3ProgramException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}