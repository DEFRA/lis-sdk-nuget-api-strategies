// <copyright file="LoggerExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

// ReSharper disable CheckNamespace
namespace Microsoft.Extensions.Logging;

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Shouldly;

/// <summary>
/// Extension methods for asserting received and not received log messages on <see cref="ILogger"/> instances.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1873:Avoid potentially expensive logging",
    Justification = "Required for testing purposes.")]
[ShouldlyMethods]
public static class LoggerExtensions
{
    /// <param name="logger">The logger instance.</param>
    extension(ILogger logger)
    {
        /// <summary>
        /// Asserts that the logger received a log message with the specified log level and message content.
        /// </summary>
        /// <param name="logLevel">The expected log level.</param>
        /// <param name="message">The expected log message.</param>
        /// <param name="count">The expected number of times the message was logged. Defaults to 1.</param>
        public void ShouldHaveReceived(LogLevel logLevel, string message, int count = 1)
        {
            ArgumentNullException.ThrowIfNull(logger);

            logger.Received(count).Log(
                logLevel,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == message),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        /// <summary>
        /// Asserts that the logger received a log message with the specified message content at any log level.
        /// </summary>
        /// <param name="message">The expected log message.</param>
        /// <param name="count">The expected number of times the message was logged. Defaults to 1.</param>
        public void ShouldHaveReceived(string message, int count = 1)
        {
            ArgumentNullException.ThrowIfNull(logger);

            logger.Received(count).Log(
                Arg.Any<LogLevel>(),
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == message),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        /// <summary>
        /// Asserts that the logger did not receive a log message with the specified log level and message content.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The log message that should not have been logged.</param>
        public void ShouldNotHaveReceived(LogLevel logLevel, string message)
        {
            ArgumentNullException.ThrowIfNull(logger);

            logger.DidNotReceive().Log(
                logLevel,
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == message),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        /// <summary>
        /// Asserts that the logger did not receive a log message with the specified message content at any log level.
        /// </summary>
        /// <param name="message">The log message that should not have been logged.</param>
        public void ShouldNotHaveReceived(string message)
        {
            ArgumentNullException.ThrowIfNull(logger);

            logger.DidNotReceive().Log(
                Arg.Any<LogLevel>(),
                Arg.Any<EventId>(),
                Arg.Is<object>(v => v.ToString() == message),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        /// <summary>
        /// Asserts that the logger did not receive any log messages at any log level.
        /// </summary>
        public void ShouldNotHaveReceivedAny()
        {
            ArgumentNullException.ThrowIfNull(logger);

            logger.DidNotReceive().Log(
                Arg.Any<LogLevel>(),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }
    }
}
