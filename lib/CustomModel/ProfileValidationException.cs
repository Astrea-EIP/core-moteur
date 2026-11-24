// <copyright file="ProfileValidationException.cs" company="Astrea">
// Copyright (c) Astrea. All rights reserved.
// </copyright>

using System;

namespace AstreaEngine
{
    /// <summary>
    /// Exception thrown when a user accessibility profile fails validation (e.g. unknown property, out-of-bounds value).
    /// Inherits from <see cref="ArgumentException"/> for backward compatibility while providing a typed exception
    /// that can be caught and translated to HTTP 400 Bad Request by API layers.
    /// </summary>
    public class ProfileValidationException : ArgumentException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileValidationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public ProfileValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileValidationException"/> class with a specified error message
        /// and the name of the parameter that caused this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public ProfileValidationException(string message, string paramName)
            : base(message, paramName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileValidationException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ProfileValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
