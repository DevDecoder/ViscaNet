// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using ViscaNet.Commands;
using Xunit;

namespace ViscaNet.Test.TestData
{
    public class NoResponsesTestData : TheoryData<Command>
    {
        public NoResponsesTestData()
        {
            foreach (Command command in CommandTestData.Instance.Select(t => t[0]).Distinct())
            {
                Add(command);
            }
        }
    }
}
