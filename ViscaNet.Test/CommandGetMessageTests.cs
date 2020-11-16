// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using DevDecoder.ViscaNet.Commands;
using DevDecoder.ViscaNet.Test.TestData;
using Xunit;
using Xunit.Abstractions;

namespace DevDecoder.ViscaNet.Test
{
    public sealed class CommandGetMessageTests : TestsBase
    {
        public CommandGetMessageTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Device_id_valid(Command command)
        {
            Context.WriteLine(command.Name);
            Context.WriteLine();
            var typeByte = (byte)command.Type;
            if (command is CancelCommand cancelCommand)
            {
                // The cancel command specifies the socket in the LSB.
                typeByte += cancelCommand.Socket;
            }

            var builder = new StringBuilder();
            var message = new byte[command.MessageSize];
            for (byte i = 0x0; i < 0x8; i++)
            {
                command.WriteMessage(message.AsSpan(), i);
                var b = message[0];
                builder.Append("0x").Append(b.ToString("x2"));
                Assert.Equal((byte)(0x80 + i), b);

                b = message[1];
                builder.Append(" 0x").Append(b.ToString("x2"));
                Assert.Equal(typeByte, b);
                Assert.Equal(0xff, message[^1]);

                Context.Write($"{i} => ");
                Context.WriteLine(builder.ToString());
                builder.Clear();
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Device_id_invalid(Command command) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => command.WriteMessage(new byte[command.MessageSize], 8));
    }
}
