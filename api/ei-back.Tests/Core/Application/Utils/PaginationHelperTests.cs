using ei_back.Core.Application.Utils;

namespace ei_back.Tests.Core.Application.Utils
{
    public class PaginationHelperTests
    {
        [Theory]
        [InlineData(null, "desc")]
        [InlineData("", "desc")]
        [InlineData("asc", "asc")]
        [InlineData("desc", "desc")]
        [InlineData("invalid", "asc")]
        public void ValidateSort_ReturnsExpected(string input, string expected)
        {
            var result = PaginationHelper.ValidateSort(input);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(-1, 10)]
        [InlineData(0, 10)]
        [InlineData(1, 1)]
        [InlineData(20, 20)]
        public void ValidateSize_ReturnsExpected(int input, int expected)
        {
            var result = PaginationHelper.ValidateSize(input);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 10, 0)]
        [InlineData(1, 10, 0)]
        [InlineData(2, 10, 10)]
        [InlineData(3, 5, 10)]
        public void ValidateOffset_ReturnsExpected(int page, int size, int expected)
        {
            var result = PaginationHelper.ValidateOffset(page, size);
            result.Should().Be(expected);
        }
    }
}
