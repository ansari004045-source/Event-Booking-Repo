using EventBooking.Helpers;
using FluentAssertions;

namespace EventBooking.Tests;

public class PagingHelperTests
{
    // Listing must not accept page 0, a negative page, or a page size above the cap.
    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-1, 500, 1, 100)]
    [InlineData(2, 25, 2, 25)]
    public void Normalize_ClampsPageAndSize(int page, int pageSize, int expectedPage, int expectedSize)
    {
        var (p, s) = PagingHelper.Normalize(page, pageSize);
        p.Should().Be(expectedPage);
        s.Should().Be(expectedSize);
    }
}
