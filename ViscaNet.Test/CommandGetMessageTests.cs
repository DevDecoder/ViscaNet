// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms.VisualStyles;
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
        public void Device_id_valid(ViscaCommand command)
        {
            Context.WriteLine(command.Name);
            Context.WriteLine();
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
                Assert.Equal((byte)(command.Type), enumerator.Current);
                byte last;
                do
                {
                    last = enumerator.Current;
                    builder.Append(" 0x").Append(enumerator.Current.ToString("x2"));
                } while (enumerator.MoveNext());
                Assert.Equal(0xff, last);

                Context.Write($"{i} => ");
                Context.WriteLine(builder.ToString());
                builder.Clear();
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Device_id_invalid(ViscaCommand command)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => command.GetMessage(8).First());
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Socket_valid(ViscaCommand command)
        {
            if (command.Type == ViscaCommandType.Cancel)
            {
                for (byte i = 0x0; i < 0x10; i++)
                {
                    var message = command.GetMessage(socket: i);
                    Assert.Equal((byte)(ViscaCommandType.Cancel + i), message.Skip(1).First());
                }
            } else {
                var message = command.GetMessage();
                Assert.Equal((byte)(command.Type), message.Skip(1).First());
            }
        }

        [Theory]
        [ClassData(typeof(NoResponsesTestData))]
        public void Socket_invalid(ViscaCommand command)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => command.GetMessage(socket: 0x10).Skip(1).First());
        }
    }
}
