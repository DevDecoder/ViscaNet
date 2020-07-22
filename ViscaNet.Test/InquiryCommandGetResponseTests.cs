// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace ViscaNet.Test
{
    public sealed class InquiryCommandGetResponseTests : TestsBase
    {
        public InquiryCommandGetResponseTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void TestPowerOn()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x50, 0x02, 0xff}, logger: Logger);
            Assert.True(response.IsValid);
            Assert.Equal(ViscaResponseType.InquiryResponse, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(PowerMode.On, response.Response);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestPowerOff()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x50, 0x03, 0xff}, logger: Logger);
            Assert.True(response.IsValid);
            Assert.Equal(ViscaResponseType.InquiryResponse, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(PowerMode.Standby, response.Response);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestPowerInvalid()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x50, 0x04, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(PowerMode.Unknown, response.Response);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(0, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("Invalid power result '0x04' received.",
                lastLog.Message);
        }

        [Fact]
        public void TestPowerInvalidLength()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x50, 0x03, 0x00, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(PowerMode.Unknown, response.Response);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The inquiry response's payload length '2' is invalid, should be '1'.",
                lastLog.Message);
        }

        [Fact]
        public void TestAck()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x43, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The inquiry command did not expect the 'ACK' response.",
                lastLog.Message);
        }

        [Fact]
        public void TestOffset()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0x00,0xA0, 0x43, 0xff}, 1, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The inquiry command did not expect the 'ACK' response.",
                lastLog.Message);
        }

        [Fact]
        public void TestCount()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] { 0xA0, 0x43, 0xff, 0x00}, count:3, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The inquiry command did not expect the 'ACK' response.",
                lastLog.Message);
        }

        [Fact]
        public void TestOffsetCount()
        {
            var response =
                ViscaCommand.InquirePower.GetResponse(new byte[] {0x00, 0xA0, 0x43, 0xff, 0x00}, 1, 3, Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(2, response.DeviceId);
            Assert.Equal(3, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The inquiry command did not expect the 'ACK' response.",
                lastLog.Message);
        }

        [Fact]
        public void TestCompletion()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xB0, 0x54, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.Unknown, response.Type);
            Assert.Equal(3, response.DeviceId);
            Assert.Equal(4, response.Socket);
            // Check for error
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.Last();
            Assert.Equal(LogLevel.Error, lastLog.LogLevel);
            Assert.Equal("The inquiry command did not expect the 'Completion' response.",
                lastLog.Message);
        }

        [Fact]
        public void TestSyntaxError()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xC0, 0x60, 0x02, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.SyntaxError, response.Type);
            Assert.Equal(4, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestSyntaxErrorWarning()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xC0, 0x65, 0x02, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
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
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xD0, 0x60, 0x03, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.CommandBufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            Assert.Equal(0, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestCommandBufferFullWarning()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xD0, 0x66, 0x03, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.CommandBufferFull, response.Type);
            Assert.Equal(5, response.DeviceId);
            // Check for warning
            Assert.Equal(1, LogEntryCount);
            var lastLog = LogEntries.First();
            Assert.Equal(LogLevel.Warning, lastLog.LogLevel);
            Assert.Equal("The 'CommandBufferFull' response should not specify a socket, but specified '6'.", lastLog.Message);
            // Socket should be zeroed
            Assert.Equal(0, response.Socket);
        }

        [Fact]
        public void TestCommandCanceled()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xE0, 0x67, 0x04, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.CommandCanceled, response.Type);
            Assert.Equal(6, response.DeviceId);
            Assert.Equal(7, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestNoSocket()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xF0, 0x68, 0x05, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.NoSocket, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(8, response.Socket);
        }

        [Fact]
        public void TestNotExecutable()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xF0, 0x69, 0x41, 0xff}, logger: Logger);
            Assert.False(response.IsValid);
            Assert.Equal(ViscaResponseType.CommandNotExecutable, response.Type);
            Assert.Equal(7, response.DeviceId);
            Assert.Equal(9, response.Socket);
            Assert.Equal(0, LogEntryCount);
        }

        [Fact]
        public void TestDeviceIdInvalid()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0x70, 0x43, 0xff}, logger: Logger);
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

        [Fact]
        public void TestOffsetTooBig()
        {
            // Note offset of 3 won't give same error as it's effectively a 0 count.
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x43, 0xff}, 4, logger: Logger);
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

        [Fact]
        public void TestCountIndicatesEmpty()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x43, 0xff}, 3, logger: Logger);
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

        [Fact]
        public void TestZeroCount()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x43, 0xff}, count:0, logger: Logger);
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

        [Fact]
        public void TestCountTooBig()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x43, 0xff}, count: 4, logger: Logger);
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

        [Fact]
        public void TestInvalidResponseSize()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0}, logger: Logger);
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

        [Fact]
        public void TestInvalidResponseSize2()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x43}, logger: Logger);
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

        [Fact]
        public void TestInvalidDeviceLSB()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA1, 0x43}, logger: Logger);
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

        [Fact]
        public void TestUnknownType()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0xA0, 0x13, 0xff}, logger: Logger);
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

        [Fact]
        public void TestInvalidTypeLength()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0x90, 0x60, 0x02}, logger: Logger);
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

        [Fact]
        public void TestBadTermination()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0x90, 0x60, 0x02, 0xFE}, logger: Logger);
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

        [Fact]
        public void TestTypeTooLong()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0x90, 0x41, 0xFF, 0xFF}, logger: Logger);
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

        [Fact]
        public void TestTypeTooLong2()
        {
            var response = ViscaCommand.InquirePower.GetResponse(new byte[] {0x90, 0x41, 0xFF, 0xFF, 0xFF}, logger: Logger);
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
