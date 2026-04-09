using dot_net_core_rest_api.Controllers;

namespace dot_net_core_rest_api.Tests;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _controller = new();

    [Fact]
    public void Get_ReturnsFiveForecasts()
    {
        var result = _controller.Get();

        var forecasts = result.ToList();
        Assert.Equal(5, forecasts.Count);
    }

    [Fact]
    public void Get_ForecastsHaveFutureDates()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        var forecasts = _controller.Get().ToList();

        Assert.All(forecasts, f => Assert.True(f.Date > today));
    }

    [Fact]
    public void Get_ForecastsHaveValidTemperatures()
    {
        var forecasts = _controller.Get().ToList();

        Assert.All(forecasts, f =>
        {
            Assert.InRange(f.TemperatureC, -20, 54);
        });
    }

    [Fact]
    public void Get_ForecastsHaveSummaries()
    {
        var forecasts = _controller.Get().ToList();

        Assert.All(forecasts, f => Assert.False(string.IsNullOrEmpty(f.Summary)));
    }
}
