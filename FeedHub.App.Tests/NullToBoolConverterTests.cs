using FeedHub_App.Converters;

namespace FeedHub.App.Tests;

public class NullToBoolConverterTests
{
    [Fact]
    public void Convert_ReturnsFalse_WhenValueIsNull()
    {
        // ARRANGE
        var converter = new NullToBoolConverter();

        // ACT
        var result = converter.Convert(null, typeof(bool), null, null!);

        // ASSERT
        Assert.Equal(false, result);
        // Verifies that a null value is converted to false.
    }
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("texto", true)]
    public void Convert_ReturnsExpectedResult_ForStringValues(
    string? value,
    bool expected)
    {
        // ARRANGE
        var converter = new NullToBoolConverter();

        // ACT
        var result = converter.Convert(value, typeof(bool), null, null!);

        // ASSERT
        Assert.Equal(expected, result);
        // Verifies that null, empty, whitespace, and valid strings return the expected boolean.
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Convert_ReturnsExpectedResult_ForDateTimeValues(bool validDate)
    {
        // ARRANGE
        var converter = new NullToBoolConverter();
        var value = validDate ? new DateTime(2026, 1, 1) : default;

        // ACT
        var result = converter.Convert(value, typeof(bool), null, null!);

        // ASSERT
        Assert.Equal(validDate, result);
        // Verifies that the default DateTime returns false and a valid date returns true.
    }
    [Fact]
    public void Convert_ReturnsTrue_WhenValueIsOtherType()
    {
        // ARRANGE
        var converter = new NullToBoolConverter();
        var value = new object();

        // ACT
        var result = converter.Convert(value, typeof(bool), null, null!);

        // ASSERT
        Assert.Equal(true, result);
        // Verifies that values of unsupported types return true.
    }
    [Theory]
    [InlineData("texto", false)]
    [InlineData(null, true)]
    public void Convert_InvertsResult_WhenParameterIsInvert(
    object? value,
    bool expected)
    {
        // ARRANGE
        var converter = new NullToBoolConverter();

        // ACT
        var result = converter.Convert(value, typeof(bool), "invert", null!);

        // ASSERT
        Assert.Equal(expected, result);
        // Verifies that the invert parameter reverses the converted boolean result.
    }
    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        // ARRANGE
        var converter = new NullToBoolConverter();

        // ACT & ASSERT
        Assert.Throws<NotImplementedException>(() =>
            converter.ConvertBack(null, typeof(bool), null, null!));
        // Verifies that ConvertBack is intentionally not implemented.
    }
}
