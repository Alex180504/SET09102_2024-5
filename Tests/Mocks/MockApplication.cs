using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Mocks
{
    /// <summary>
    /// A mock implementation of the MAUI Application class for unit testing.
    /// </summary>
    public class MockApplication : Application
    {
        public MockApplication(IDispatcher dispatcher)
        {
            // Use reflection to set the Dispatcher field since it's read-only
            var dispatcherField = typeof(BindableObject).GetField("_dispatcher",
                BindingFlags.NonPublic | BindingFlags.Instance);
            dispatcherField?.SetValue(this, dispatcher);
        }
    }
}
