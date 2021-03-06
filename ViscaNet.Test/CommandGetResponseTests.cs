﻿// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using DevDecoder.ViscaNet.Commands;
using DevDecoder.ViscaNet.Test.TestData;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace DevDecoder.ViscaNet.Test
{
    public sealed class CommandGetResponseTests : TestsBase
    {
        public CommandGetResponseTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Theory]
        [ClassData(typeof(CommandTestData))]
        public void InquiryResponse_response(
            Command command,
            byte[] expectedData,
            object? expectedResponse,
            LogLevel expectedLogLevel,
            string? expectedLogMessage)
        {
            var response = command.GetResponse(expectedData, Logger);

            Assert.Equal(1, response.DeviceId);
            Assert.Equal(0, response.Socket);

            if (command.Type == CommandType.Inquiry)
            {
                if (expectedLogLevel != LogLevel.None)
                {
                    if (expectedLogLevel >= LogLevel.Error)
                    {
                        Assert.False(response.IsValid);
                        Assert.Equal(ResponseType.Unknown, response.Type);
                    }
                    else
                    {
                        Assert.True(response.IsValid);
                        Assert.Equal(ResponseType.Inquiry, response.Type);
                    }

                    // Check for log
                    Assert.Equal(1, LogEntryCount);
                    var lastLog = LogEntries.Last();
                    Assert.Equal(expectedLogLevel, lastLog.LogLevel);
                    Assert.Equal(expectedLogMessage, lastLog.Message);
                }
                else
                {
                    Assert.True(response.IsValid);
                    Assert.Equal(ResponseType.Inquiry, response.Type);
                    Assert.Equal(0, LogEntryCount);
                }

                // Check result
                Assert.Equal(expectedResponse, response.ResultObject);
                return;
            }

            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var error = LogEntries.Last();
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(
                $"The '{nameof(ResponseType.Inquiry)}' response was not expected for the '{command.Type}' type.",
                error.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Ack_response(Command command)
        {
            var response = command.GetResponse(new byte[] {0xA0, 0x43, 0xff}, Logger);
            // ACK is never a 'valid' response, it precedes a Completion response.
            Assert.False(response.IsValid);
            if (command.Type == CommandType.Command)
            {
                Assert.Equal(ResponseType.ACK, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else
            {
                Assert.Equal(ResponseType.Unknown, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal(
                    $"The '{nameof(ResponseType.ACK)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Completion_response(Command command)
        {
            var response = command.GetResponse(new byte[] {0xB0, 0x54, 0xff}, Logger);
            if (command.Type == CommandType.Command)
            {
                Assert.True(response.IsValid);
                Assert.Equal(ResponseType.Completion, response.Type);
                Assert.Equal(3, response.DeviceId);
                Assert.Equal(4, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else
            {
                Assert.False(response.IsValid);
                Assert.Equal(ResponseType.Unknown, response.Type);
                Assert.Equal(3, response.DeviceId);
                Assert.Equal(4, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal(
                    $"The '{nameof(ResponseType.Completion)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandCanceled_response(Command command)
        {
            var response = command.GetResponse(new byte[] {0xE0, 0x67, 0x04, 0xff}, Logger);
            if (command.Type == CommandType.Cancel)
            {
                Assert.True(response.IsValid);
                Assert.Equal(ResponseType.Canceled, response.Type);
                Assert.Equal(6, response.DeviceId);
                Assert.Equal(7, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else

            {
                Assert.False(response.IsValid);
                Assert.Equal(ResponseType.Unknown, response.Type);
                Assert.Equal(6, response.DeviceId);
                Assert.Equal(7, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal(
                    $"The '{nameof(ResponseType.Canceled)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void SyntaxError_response(Command command)
        {
            var response = command.GetResponse(new byte[] {0xC0, 0x60, 0x02, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.SyntaxError, response.Type);
            Assert.Equal(4, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void SyntaxError_response_with_socket_warning(Command command)
        {
            var response = command.GetResponse(new byte[] {0xC0, 0x65, 0x02, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.SyntaxError, response.Type);
            Assert.Equal(4, response.DeviceId);
            // Check for warning
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Warning, lastLog.LogLevel);
            Assert.Equal(
                $"The '{nameof(ResponseType.SyntaxError)}' response should not specify a socket, but specified '5'.",
                lastLog.Message);
            // Socket should be zeroed
            Assert.Equal(0, response.Socket);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandBufferFull_response(Command command)
        {
            var response = command.GetResponse(new byte[] {0xD0, 0x60, 0x03, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.BufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandBufferFull_response_with_socket_warning(Command command)
        {
            var response = command.GetResponse(new byte[] {0xD0, 0x66, 0x03, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.BufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            // Check for warning
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Warning, lastLog.LogLevel);
            Assert.Equal(
                $"The '{nameof(ResponseType.BufferFull)}' response should not specify a socket, but specified '6'.",
                lastLog.Message);
            // Socket should be zeroed
            Assert.Equal(0, response.Socket);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void NoSocket_response(Command command)
        {
            var response = command.GetResponse(new byte[] {0xF0, 0x68, 0x05, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.NoSocket, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(8, response.Socket);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandNotExecutable_response(Command command)
        {
            var response = command.GetResponse(new byte[] {0xF0, 0x69, 0x41, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.NotExecutable, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(9, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Device_ID_out_of_range_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0x70, 0x43, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(0, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The response's device id '-1' was invalid, as it must be greater than 0 (usually 1).",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Response_too_short_1_byte_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0xA0}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The response's length '1' was too short.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Response_too_short_2_bytes_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0xA0, 0x43}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The response's length '2' was too short.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Device_ID_LSB_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0xA1, 0x43}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(0, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The response's device byte 'A1' was invalid.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Type_unknown_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0xA0, 0x13, 0xff}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The response's type '10ff' is unknown.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Termination_missing_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0x90, 0x60, 0x02}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(1, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The response's last byte '02' was not a termination '0xFF'.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Termination_invalid_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0x90, 0x60, 0x02, 0xFE}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(1, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The response's last byte 'FE' was not a termination '0xFF'.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Response_length_invalid_4_bytes_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0x90, 0x41, 0xFF, 0xFF}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(1, response.DeviceId);
            Assert.Equal(1, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The response's length '4' was invalid for it's type 'ACK'.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Response_length_invalid_5_bytes_error(Command command)
        {
            var response = command.GetResponse(new byte[] {0x90, 0x41, 0xFF, 0xFF, 0xFF}, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ResponseType.Unknown, response.Type);
            Assert.Equal(1, response.DeviceId);
            Assert.Equal(1, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The response's length '5' was invalid for it's type 'ACK'.",
                lastLog.Message);
        }
    }
}
