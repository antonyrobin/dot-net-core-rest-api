namespace dot_net_core_rest_api.Tests;

public class WeatherForecastTests
{
    [Fact]
    public void TemperatureF_ConvertsFromCelsius()
    {
        var forecast = new WeatherForecast { TemperatureC = 0 };
        Assert.Equal(32, forecast.TemperatureF);
    }

    [Fact]
    public void TemperatureF_ConvertsPositiveValue()
    {
        var forecast = new WeatherForecast { TemperatureC = 100 };
        // 32 + (int)(100 / 0.5556) = 32 + 179 = 211
        Assert.Equal(32 + (int)(100 / 0.5556), forecast.TemperatureF);
    }

    [Fact]
    public void TemperatureF_ConvertsNegativeValue()
    {
        var forecast = new WeatherForecast { TemperatureC = -40 };
        Assert.Equal(32 + (int)(-40 / 0.5556), forecast.TemperatureF);
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var date = new DateOnly(2026, 4, 9);
        var forecast = new WeatherForecast
        {
            Date = date,
            TemperatureC = 25,
            Summary = "Warm"
        };

        Assert.Equal(date, forecast.Date);
        Assert.Equal(25, forecast.TemperatureC);
        Assert.Equal("Warm", forecast.Summary);
    }
}
