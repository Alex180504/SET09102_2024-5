using System.Collections.Generic;
using System.Globalization;
using Xunit;
using SET09102_2024_5.Converters;

namespace Tests.Converters
{
    public class DictionaryValueConverterTests
    {
        private readonly DictionaryValueConverter _conv = new();

        [Fact]
        public void Convert_ReturnsCorrectValue_WhenKeyExists()
        {
            var dict = new Dictionary<string, double> { ["X"] = 3.14 };
            var result = _conv.Convert(dict, null, "X", CultureInfo.InvariantCulture);
            Assert.Equal(3.14, result);
        }

        [Fact]
        public void Convert_ReturnsZero_WhenKeyNotFound()
        {
            var dict = new Dictionary<string, double>();
            var result = _conv.Convert(dict, null, "Missing", CultureInfo.InvariantCulture);
            Assert.Equal(0, result);
        }
    }
}
