// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace ViscaNet.Test
{
    public sealed class CommandGetResponseTests : TestsBase
    {
        public CommandGetResponseTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void TestAck()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0, 0x43, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.ACK, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestOffset()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0x00,0xA0, 0x43, 0xff}, 1, logger: Logger);
            Assert.Equal(ViscaResponseType.ACK, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestCount()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] { 0xA0, 0x43, 0xff, 0x00}, count:3, logger: Logger);
            Assert.Equal(ViscaResponseType.ACK, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestOffsetCount()
        {
            var response =
                ViscaCommand.Reset.GetResponse(new byte[] {0x00, 0xA0, 0x43, 0xff, 0x00}, 1, 3, logger: Logger);
            Assert.Equal(ViscaResponseType.ACK, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestCompletion()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xB0, 0x54, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.Completion, response.Type);
            Assert.Equal(3, response.DeviceId);
            Assert.Equal(4, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestSyntaxError()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xC0, 0x60, 0x02, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.SyntaxError, response.Type);
            Assert.Equal(4, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestSyntaxErrorWarning()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xC0, 0x65, 0x02, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.SyntaxError, response.Type);
            Assert.Equal(4, response.DeviceId);
            // Check for warning
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Warning, lastLog.LogLevel);
            Assert.Equal("The 'SyntaxError' response should not specify a socket, but specified '5'.",
                lastLog.Message);
            // Socket should be zeroed
            Assert.Equal(0, response.Socket);
        }

        [Fact]
        public void TestCommandBufferFull()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xD0, 0x60, 0x03, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.CommandBufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestCommandBufferFullWarning()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xD0, 0x66, 0x03, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.CommandBufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            // Check for warning
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Warning, lastLog.LogLevel);
            Assert.Equal("The 'CommandBufferFull' response should not specify a socket, but specified '6'.", lastLog.Message);
            // Socket should be zeroed
            Assert.Equal(0, response.Socket);
        }

        [Fact]
        public void TestCommandCanceled()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xE0, 0x67, 0x04, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.CommandCanceled, response.Type);
            Assert.Equal(6, response.DeviceId);
            Assert.Equal(7, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestNoSocket()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xF0, 0x68, 0x05, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.NoSocket, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(8, response.Socket);
        }

        [Fact]
        public void TestNotExecutable()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xF0, 0x69, 0x41, 0xff}, logger: Logger);
            Assert.Equal(ViscaResponseType.CommandNotExecutable, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(9, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestDeviceIdInvalid()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0x70, 0x43, 0xff}, logger: Logger);
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

        [Fact]
        public void TestOffsetTooBig()
        {
            // Note offset of 3 won't give same error as it's effectively a 0 count.
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0, 0x43, 0xff}, 4, logger: Logger);
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

        [Fact]
        public void TestCountIndicatesEmpty()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0, 0x43, 0xff}, 3, logger: Logger);
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

        [Fact]
        public void TestZeroCount()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0, 0x43, 0xff}, count:0, logger: Logger);
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

        [Fact]
        public void TestCountTooBig()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0, 0x43, 0xff}, count: 4, logger: Logger);
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

        [Fact]
        public void TestInvalidResponseSize()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0}, logger: Logger);
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

        [Fact]
        public void TestInvalidResponseSize2()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0, 0x43}, logger: Logger);
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

        [Fact]
        public void TestInvalidDeviceLSB()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA1, 0x43}, logger: Logger);
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

        [Fact]
        public void TestUnknownType()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0xA0, 0x13, 0xff}, logger: Logger);
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

        [Fact]
        public void TestInvalidTypeLength()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0x90, 0x60, 0x02}, logger: Logger);
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

        [Fact]
        public void TestBadTermination()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0x90, 0x60, 0x02, 0xFE}, logger: Logger);
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

        [Fact]
        public void TestTypeTooLong()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0x90, 0x41, 0xFF, 0xFF}, logger: Logger);
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

        [Fact]
        public void TestTypeTooLong2()
        {
            var response = ViscaCommand.Reset.GetResponse(new byte[] {0x90, 0x41, 0xFF, 0xFF, 0xFF}, logger: Logger);
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

        [Fact]
        public void TestInquiryResponse()
        {
            var response =
                ViscaCommand.Reset.GetResponse(new byte[] {0x90, 0x50, 0x02, 0xFF}, logger: Logger);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(1, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The InquiryResponse is not a valid response type for a command.",
                lastLog.Message);
        }

        [Fact]
        public void TestInquiryResponse2()
        {
            var response =
                ViscaCommand.Reset.GetResponse(new byte[] {0x90, 0x50, 0x00, 0xFF}, logger: Logger);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(1, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The InquiryResponse is not a valid response type for a command.",
                lastLog.Message);
        }

        [Fact]
        public void TestInquiryResponse3()
        {
            var response =
                ViscaCommand.Reset.GetResponse(new byte[] {0x90, 0x50, 0x00, 0x00, 0x00, 0x00, 0xFF},
                    logger: Logger);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(1, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            // Type is calculated from bytes 2 & 3
            Assert.Equal("The InquiryResponse is not a valid response type for a command.",
                lastLog.Message);
        }
    }
}
