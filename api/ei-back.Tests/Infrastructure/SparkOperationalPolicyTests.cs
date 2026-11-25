using ei_back.Core.Application.Service.Sparks;

namespace ei_back.Tests.Infrastructure;

public class SparkOperationalPolicyTests
{
    [Fact]
    public void Coverage_WithOutstandingGrants_UsesFullCapacityAndExactThreshold()
    {
        var unitCost = 0.0061m;
        SparkOperationalPolicy.CoverageBlockReason(820 * unitCost, unitCost, 820, 6)
            .Should().Be(SparkOperationalPolicy.InsufficientCredits);
        SparkOperationalPolicy.CoverageBlockReason(826 * unitCost - 0.0001m, unitCost, 820, 6)
            .Should().Be(SparkOperationalPolicy.InsufficientCredits);
        SparkOperationalPolicy.CoverageBlockReason(826 * unitCost, unitCost, 820, 6)
            .Should().BeNull();
        SparkOperationalPolicy.CoverageBlockReason(null, unitCost, 820, 6)
            .Should().Be(SparkOperationalPolicy.ProviderUnavailable);
    }

    [Fact]
    public void Report_WithEqualityOrUnknownCredits_DoesNotTreatThemAsDeficit()
    {
        SparkOperationalPolicy.ShouldReport(0, 0, 82, 82m, 82, false).Should().BeFalse();
        SparkOperationalPolicy.ShouldReport(0, 0, 82, 82m, null, false).Should().BeFalse();
        SparkOperationalPolicy.ShouldReport(0, 0, 81, 82m, 82, false).Should().BeTrue();
        SparkOperationalPolicy.ShouldReport(0, 0, 82, 82m, 81, false).Should().BeTrue();
        SparkOperationalPolicy.ShouldReport(0, 1, 82, 82m, 82, false).Should().BeTrue();
        SparkOperationalPolicy.ShouldReport(1, 0, 82, 82m, 82, false).Should().BeTrue();
        SparkOperationalPolicy.ShouldReport(0, 0, 82, 82m, 82, true).Should().BeTrue();
    }

    [Fact]
    public void Window_WithSaoPauloBoundary_UsesLocalMidnight()
    {
        var start = new DateTimeOffset(2026, 11, 10, 3, 0, 0, TimeSpan.Zero);
        SparkCalendar.LocalDate(start.AddTicks(-1)).Should().Be(new DateOnly(2026, 11, 9));
        SparkCalendar.Window(start.AddTicks(-1), 10).End.Should().Be(start);
        SparkCalendar.Window(start, 10).Start.Should().Be(start);
        SparkCalendar.Window(new DateTimeOffset(2026, 2, 27, 0, 0, 0, TimeSpan.Zero), 31)
            .End.Should().Be(new DateTimeOffset(2026, 2, 28, 3, 0, 0, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(6, 10, 11, false)]
    [InlineData(7, 10, 11, true)]
    [InlineData(10, 10, 11, true)]
    [InlineData(11, 10, 11, true)]
    [InlineData(12, 10, 11, false)]
    public void IsRenewalWindow_WithThreeDayLead_IncludesFirstDayAfterClosure(int day, int closure, int nextMonth, bool expected)
    {
        var stamp = new DateOnly(2026, 10, day);
        var pastClosure = day >= closure;
        var dates = new SparkCalendarWindow(stamp,
            new DateOnly(2026, pastClosure ? 10 : 9, closure),
            new DateOnly(2026, pastClosure ? nextMonth : 10, closure));
        SparkCalendar.IsRenewalWindow(dates, 3).Should().Be(expected);
    }
}
