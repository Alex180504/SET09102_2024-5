using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Mocks
{
    /// <summary>
    /// Class for mocking static methods in unit tests.
    /// </summary>
    public class MockStaticMethod
    {
        private readonly Delegate _mockDelegate;

        public MockStaticMethod(Delegate mockDelegate)
        {
            _mockDelegate = mockDelegate;
        }

        public Delegate GetMockDelegate()
        {
            return _mockDelegate;
        }
    }
}
