using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using FluentAssertions;

using ContextComputing;

namespace ContextComputingUnitTests
{
    // Context types.
    public class LogTextBox { }

    public class Logger : IContextComputingListener
    {
        [Listener]
        [Publishes(new string[] { "A", "B" })]
        public void LogMe(ContextRouter router, ContextItem item,
            [Context(nameof(LogTextBox))]   object textBox,
            LogInfo info)                   // context here defaults to the type name of the parameter.
        {
        }
    }

    [TestClass]
    public class AutoRegistrationUnitTests
    {
        [TestMethod]
        public void RegisterClassTest()
        {
            ContextRouter cr = new ContextRouter();
            AutoRegistration.AutoRegister<Logger>(cr);

            var listeners = cr.GetAllListeners();
            listeners.Count.Should().Be(1);
            listeners[0].Name.Should().Be(typeof(Logger).Name);

            var contexts = cr.GetTriggerContexts(typeof(Logger), "LogMe");
            contexts.Count.Should().Be(2);
            contexts[0].Should().Be("LogTextBox");
            contexts[1].Should().Be("LogInfo");
        }
    }
}
