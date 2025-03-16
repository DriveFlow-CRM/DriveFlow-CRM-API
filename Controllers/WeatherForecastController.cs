using DriveFlow_CRM_API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;


//[Authorize(Roles="SuperAdmin,Admin,Instructor,Student")]

namespace DriveFlow_CRM_API.Controllers
{
    [ApiController]
    [Route("api/weatherforecast")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;


        public WeatherForecastController(ApplicationDbContext _db, UserManager<ApplicationUser> _userManager, RoleManager<IdentityRole> _roleManager)
        {
            db = _db;
            userManager = _userManager;
            roleManager = _roleManager;
        }

        // GET: api/weatherforecast/all
        [HttpGet("all", Name = "GetWeatherForecasts")]
        public async Task<IActionResult> GetAllForecastsAsync()
        {
            var forecasts = await db.WeatherForecasts.ToListAsync();
            return Ok(forecasts);
        }

        // GET: api/weatherforecast/{id}
        [HttpGet("{id}", Name = "GetWeatherForecastById")]
        public async Task<ActionResult<WeatherForecast>> GetForecastByIdAsync(int id)
        {
            var forecast = await db.WeatherForecasts.FindAsync(id);
            if (forecast == null)
            {
                return NotFound();
            }
            return Ok(forecast);
        }

        //create nu imi trimite id-ul 
        //trebuie sa-l calculez automat pe baza datelor din tabel
        //trebuie sa verific ca obiectul are toate campurile completate si ca sunt valide
        //foreing key-uri trebuie sa fie valide

        //validez tipurile campurilor si validez foreign key-urile
        //validarea minim necesara pentru a putea face insert in baza de date
        //nu vrem sa ajunga gresit in baza de date

        // POST: api/weatherforecast/create
        [HttpPost("create", Name = "CreateWeatherForecast")]
        public async Task<ActionResult<WeatherForecast>> CreateForecastAsync([FromBody] WeatherForecast forecast)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await db.WeatherForecasts.AddAsync(forecast);
            await db.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetForecastByIdAsync),
                new { id = forecast.Id },
                forecast
            );
        }

        // PUT: api/weatherforecast/update/{id}
        //trebuie sa verific ca id-ul exista 
        //daca nu eroare
        //trebuie sa verific daca obiectul are fk care exista in tabel
        //trebuie sa verific ca are foreign key care exista in tabel
        //trebuie sa verific ca obiectul are toate campurile completate 


        [HttpPut("update/{id}", Name = "UpdateWeatherForecast")]
        public async Task<IActionResult> UpdateForecastAsync(int id, [FromBody] WeatherForecast forecast)
        {
            if (id != forecast.Id)
            {
                return BadRequest("The ID in the URL does not match the ID of the provided object.");
            }

            db.Entry(forecast).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WeatherForecastExists(id))
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

        // DELETE: api/weatherforecast/delete/{id}
        [HttpDelete("delete/{id}", Name = "DeleteWeatherForecast")]
        public async Task<IActionResult> DeleteForecastAsync(int id)
        {
            var forecast = await db.WeatherForecasts.FindAsync(id);
            if (forecast == null)
            {
                return NotFound();
            }

            db.WeatherForecasts.Remove(forecast);
            await db.SaveChangesAsync();

            return NoContent();
        }

        private bool WeatherForecastExists(int id)
        {
            return db.WeatherForecasts.Any(e => e.Id == id);
        }
    }
}
