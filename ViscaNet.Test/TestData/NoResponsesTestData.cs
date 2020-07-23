// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Xunit;

namespace ViscaNet.Test.TestData
{
    public class NoResponsesTestData : TheoryData<ViscaCommand>
    {
        public NoResponsesTestData()
        {
            foreach (ViscaCommand command in CommandTestData.Instance.Select(t=>t[0]).Distinct())
            {
                Add(command);
            }
        }
    }
}
