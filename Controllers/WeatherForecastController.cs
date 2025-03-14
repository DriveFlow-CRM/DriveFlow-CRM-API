using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace DriveFlow_CRM_API.Controllers;

[ApiController]
[Route("api/weatherforecast")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly AppDbContext _context;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpGet("all", Name = "GetWeatherForecasts")]
    public IActionResult Get()
    {
        var weatherForecasts = _context.WeatherForecasts.ToList();
        return Ok(weatherForecasts);
    }

    [HttpGet("{id}", Name = "GetWeatherForecastById")]
    public ActionResult<WeatherForecast> Get(int id)
    {
        var forecast = _context.WeatherForecasts.Find(id);
        if (forecast == null)
        {
            return NotFound();
        }
        return forecast;
    }

    [HttpPost("create", Name = "CreateWeatherForecast")]
    public ActionResult<WeatherForecast> Post([FromBody] WeatherForecast forecast)
    {
        _context.WeatherForecasts.Add(forecast);
        _context.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = forecast.Id }, forecast);
    }

    [HttpPut("update/{id}", Name = "UpdateWeatherForecast")]
    public IActionResult Put(int id, [FromBody] WeatherForecast forecast)
    {
        if (id != forecast.Id)
        {
            return BadRequest();
        }

        _context.Entry(forecast).State = EntityState.Modified;
        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.WeatherForecasts.Any(e => e.Id == id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("delete/{id}", Name = "DeleteWeatherForecast")]
    public IActionResult Delete(int id)
    {
        var forecast = _context.WeatherForecasts.Find(id);
        if (forecast == null)
        {
            return NotFound();
        }

        _context.WeatherForecasts.Remove(forecast);
        _context.SaveChanges();

        return NoContent();
    }
}
