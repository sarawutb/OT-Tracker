using System;
using Xunit;
using OTTracker_Avalonia.Domain.Entities;
using OTTracker_Avalonia.Domain.Enums;
using OTTracker_Avalonia.AppServices.Services;

namespace OTTracker_Avalonia.Tests;

public class CalculationTests
{
    private readonly OtCalculationService _calculator = new();

    [Fact]
    public void GetHourlyRate_ShouldCalculateCorrectRate()
    {
        // Arrange
        var settings = new AppSettings
        {
            BaseMonthlySalary = 30000m,
            WorkingDaysPerMonth = 30,
            HoursPerDay = 8m
        };

        // Act
        var result = _calculator.GetHourlyRate(settings);

        // Assert
        Assert.Equal(125m, result);
    }

    [Fact]
    public void GetMultiplier_ShouldReturnCorrectMultiplierForDayTypes()
    {
        // Arrange
        var settings = new AppSettings
        {
            RegularMultiplier = 1.5m,
            WeekendMultiplier = 2.0m,
            HolidayMultiplier = 3.0m
        };

        // Act & Assert
        Assert.Equal(1.5m, _calculator.GetMultiplier(settings, DayType.Regular));
        Assert.Equal(2.0m, _calculator.GetMultiplier(settings, DayType.Weekend));
        Assert.Equal(3.0m, _calculator.GetMultiplier(settings, DayType.Holiday));
    }

    [Fact]
    public void GetNetHours_ShouldCalculateCorrectHoursIncludingBreaks()
    {
        // Arrange
        var start = new TimeSpan(17, 0, 0);
        var end = new TimeSpan(21, 0, 0);
        var breakMin = 30;

        // Act
        var result = _calculator.GetNetHours(start, end, breakMin);

        // Assert
        Assert.Equal(3.5m, result);
    }

    [Fact]
    public void GetNetHours_ShouldClampNegativeHoursToZero()
    {
        // Arrange
        var start = new TimeSpan(18, 0, 0);
        var end = new TimeSpan(17, 0, 0);
        var breakMin = 0;

        // Act
        var result = _calculator.GetNetHours(start, end, breakMin);

        // Assert
        Assert.Equal(0m, result);
    }

    [Theory]
    [InlineData(3.5, 125, 1.5, 656.25)]
    [InlineData(5, 125, 2.0, 1250)]
    [InlineData(4, 125, 3.0, 1500)]
    public void GetEstimatedEarnings_ShouldCalculateCorrectEarnings(double netHours, double hourlyRate, double multiplier, double expected)
    {
        // Act
        var result = _calculator.GetEstimatedEarnings((decimal)netHours, (decimal)hourlyRate, (decimal)multiplier);

        // Assert
        Assert.Equal((decimal)expected, result);
    }
}
