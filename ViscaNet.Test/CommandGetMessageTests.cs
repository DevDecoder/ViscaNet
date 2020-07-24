// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms.VisualStyles;
using ViscaNet.Commands;
using ViscaNet.Test.TestData;
using Xunit;
using Xunit.Abstractions;

namespace ViscaNet.Test
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
            for (byte i = 0x0; i < 0x8; i++)
            {
                var message = command.GetMessage(i);
                var enumerator = message.GetEnumerator();
                Assert.True(enumerator.MoveNext());
                builder.Append("0x").Append(enumerator.Current.ToString("x2"));
                Assert.Equal((byte)(0x80 + i), enumerator.Current);
                Assert.True(enumerator.MoveNext());
                builder.Append(" 0x").Append(enumerator.Current.ToString("x2"));
                Assert.Equal(typeByte, enumerator.Current);
                var last = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    last = enumerator.Current;
                    builder.Append(" 0x").Append(enumerator.Current.ToString("x2"));
                }
                Assert.Equal(0xff, last);

                Context.Write($"{i} => ");
                Context.WriteLine(builder.ToString());
                builder.Clear();
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Device_id_invalid(Command command)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => command.GetMessage(8).First());
        }
    }
}
