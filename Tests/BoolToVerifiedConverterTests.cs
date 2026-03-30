using Dashboard.Views;
using System.Globalization;
using Xunit;

public class BoolToVerifiedConverterTests
{
    [Fact]
    public void Convert_ReturnsCorrectString()
    {
        var conv = new BoolToVerifiedConverter();

        Assert.Equal("Verified", conv.Convert(true, null, null, CultureInfo.InvariantCulture));
        Assert.Equal("Unverified", conv.Convert(false, null, null, CultureInfo.InvariantCulture));
        Assert.Equal("Unverified", conv.Convert(null, null, null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_ReturnsCorrectBool()
    {
        var conv = new BoolToVerifiedConverter();

        Assert.True((bool)conv.ConvertBack("Verified", null, null, CultureInfo.InvariantCulture));
        Assert.False((bool)conv.ConvertBack("Unverified", null, null, CultureInfo.InvariantCulture));
        Assert.False((bool)conv.ConvertBack("anything else", null, null, CultureInfo.InvariantCulture));
    }
}