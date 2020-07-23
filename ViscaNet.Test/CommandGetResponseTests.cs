// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Extensions.Logging;
using ViscaNet.Commands;
using ViscaNet.Test.TestData;
using Xunit;
using Xunit.Abstractions;

namespace ViscaNet.Test
{
    public sealed class CommandGetResponseTests : TestsBase
    {
        public CommandGetResponseTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Theory]
        [ClassData(typeof(CommandTestData))]
        public void InquiryResponse_response(
            ViscaCommand command,
            byte[] expectedData, 
            object? expectedResponse, 
            LogLevel expectedLogLevel, 
            string? expectedLogMessage)
        {
            var response = command.GetResponse(expectedData, logger: Logger);

            Assert.Equal(1, response.DeviceId);
            Assert.Equal(0, response.Socket);

            if (command.Type == ViscaCommandType.Inquiry)
            {
                if (expectedLogLevel != LogLevel.None)
                {
                    if (expectedLogLevel >= LogLevel.Error)
                    {
                        Assert.False(response.IsValid);
                        Assert.Equal(ViscaResponseType.Unknown, response.Type);
                    }
                    else
                    {
                        Assert.True(response.IsValid);
                        Assert.Equal(ViscaResponseType.Inquiry, response.Type);
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
                    Assert.Equal(ViscaResponseType.Inquiry, response.Type);
                    Assert.Equal(0, LogEntryCount);
                }

                // Check result
                Assert.Equal(expectedResponse, response.ResultObject);
                return;
            }

            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var error = LogEntries.Last();
            Assert.Equal(LogLevel.Error, error.LogLevel);
            Assert.Equal(
                $"The '{nameof(ViscaResponseType.Inquiry)}' response was not expected for the '{command.Type}' type.",
                error.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Ack_response(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0, 0x43, 0xff }, logger: Logger);
            // ACK is never a 'valid' response, it precedes a Completion response.
            Assert.False(response.IsValid);
            if (command.Type == ViscaCommandType.Command)
            {
                Assert.Equal(ViscaResponseType.ACK, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else
            {
                Assert.Equal(ViscaResponseType.Unknown, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal($"The '{nameof(ViscaResponseType.ACK)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Ack_response_with_offset(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0x00, 0xA0, 0x43, 0xff }, 1, logger: Logger);
            // ACK is never a 'valid' response, it precedes a Completion response.
            Assert.False(response.IsValid);
            if (command.Type == ViscaCommandType.Command)
            {
                Assert.Equal(ViscaResponseType.ACK, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else
            {
                Assert.Equal(ViscaResponseType.Unknown, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal($"The '{nameof(ViscaResponseType.ACK)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Ack_response_with_count(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0, 0x43, 0xff, 0x00 }, count: 3, logger: Logger);
            // ACK is never a 'valid' response, it precedes a Completion response.
            Assert.False(response.IsValid);
            if (command.Type == ViscaCommandType.Command)
            {
                Assert.Equal(ViscaResponseType.ACK, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else
            {
                Assert.Equal(ViscaResponseType.Unknown, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal($"The '{nameof(ViscaResponseType.ACK)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Ack_response_with_offset_and_count(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0x00, 0xA0, 0x43, 0xff, 0x00 }, 1, 3, logger: Logger);
            // ACK is never a 'valid' response, it precedes a Completion response.
            Assert.False(response.IsValid);
            if (command.Type == ViscaCommandType.Command)
            {
                Assert.Equal(ViscaResponseType.ACK, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else
            {
                Assert.Equal(ViscaResponseType.Unknown, response.Type);
                Assert.Equal(2, response.DeviceId);
                Assert.Equal(3, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal($"The '{nameof(ViscaResponseType.ACK)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Completion_response(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xB0, 0x54, 0xff }, logger: Logger);
            if (command.Type == ViscaCommandType.Command)
            {
                Assert.True(response.IsValid);
                Assert.Equal(ViscaResponseType.Completion, response.Type);
                Assert.Equal(3, response.DeviceId);
                Assert.Equal(4, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else
            {
                Assert.False(response.IsValid);
                Assert.Equal(ViscaResponseType.Unknown, response.Type);
                Assert.Equal(3, response.DeviceId);
                Assert.Equal(4, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal($"The '{nameof(ViscaResponseType.Completion)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandCanceled_response(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xE0, 0x67, 0x04, 0xff }, logger: Logger);
            if (command.Type == ViscaCommandType.Cancel)
            {
                Assert.True(response.IsValid);
                Assert.Equal(ViscaResponseType.Canceled, response.Type);
                Assert.Equal(6, response.DeviceId);
                Assert.Equal(7, response.Socket);
                Assert.Equal(0, LogEntryCount);
            }
            else

            {
                Assert.False(response.IsValid);
                Assert.Equal(ViscaResponseType.Unknown, response.Type);
                Assert.Equal(6, response.DeviceId);
                Assert.Equal(7, response.Socket);
                // Check for error
                Assert.Equal(1, LogEntryCount);
                var lastLog = LogEntries.Last();
                Assert.Equal(LogLevel.Error, lastLog.LogLevel);
                Assert.Equal($"The '{nameof(ViscaResponseType.Canceled)}' response was not expected for the '{command.Type}' type.",
                    lastLog.Message);
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void SyntaxError_response(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xC0, 0x60, 0x02, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.SyntaxError, response.Type);
            Assert.Equal(4, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void SyntaxError_response_with_socket_warning(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xC0, 0x65, 0x02, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.SyntaxError, response.Type);
            Assert.Equal(4, response.DeviceId);
            // Check for warning
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Warning, lastLog.LogLevel);
            Assert.Equal($"The '{nameof(ViscaResponseType.SyntaxError)}' response should not specify a socket, but specified '5'.",
                lastLog.Message);
            // Socket should be zeroed
            Assert.Equal(0, response.Socket);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandBufferFull_response(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xD0, 0x60, 0x03, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.BufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandBufferFull_response_with_socket_warning(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xD0, 0x66, 0x03, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.BufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            // Check for warning
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Warning, lastLog.LogLevel);
            Assert.Equal($"The '{nameof(ViscaResponseType.BufferFull)}' response should not specify a socket, but specified '6'.", lastLog.Message);
            // Socket should be zeroed
            Assert.Equal(0, response.Socket);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void NoSocket_response(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xF0, 0x68, 0x05, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.NoSocket, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(8, response.Socket);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void CommandNotExecutable_response(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xF0, 0x69, 0x41, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.NotExecutable, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(9, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Device_ID_out_of_range_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0x70, 0x43, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Offset_too_big_error(ViscaCommand command)
        {
            // Note offset of 3 won't give same error as it's effectively a 0 count.
            var response = command.GetResponse(new byte[] { 0xA0, 0x43, 0xff }, 4, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(0, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The offset '4' exceeds the response length '3'.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Offset_EOL_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0, 0x43, 0xff }, 3, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(0, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The response was empty.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Count_empty_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0, 0x43, 0xff }, count: 0, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(0, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The response was empty.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Count_too_big_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0, 0x43, 0xff }, count: 4, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(0, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The count '4' exceeds the response length '3'.",
                lastLog.Message);
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Response_too_short_1_byte_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0 }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Response_too_short_2_bytes_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0, 0x43 }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Device_ID_LSB_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA1, 0x43 }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Type_unknown_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0xA0, 0x13, 0xff }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Termination_missing_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0x90, 0x60, 0x02 }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Termination_invalid_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0x90, 0x60, 0x02, 0xFE }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Response_length_invalid_4_bytes_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0x90, 0x41, 0xFF, 0xFF }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
        public void Response_length_invalid_5_bytes_error(ViscaCommand command)
        {
            var response = command.GetResponse(new byte[] { 0x90, 0x41, 0xFF, 0xFF, 0xFF }, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
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
