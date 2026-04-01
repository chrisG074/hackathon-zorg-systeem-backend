using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftZorg.Data;
using SoftZorg.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoftZorg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeldingenController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MeldingenController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/meldingen
        // Dit is de endpoint die je React overzicht gaat aanroepen!
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Melding>>> GetMeldingen()
        {
            // Haalt alles op en zet de nieuwste meldingen bovenaan
            var meldingen = await _context.Meldingen
                                          .OrderByDescending(m => m.Datum)
                                          .ToListAsync();

            return Ok(meldingen);
        }

        // POST: api/meldingen
        // Deze endpoint gebruiken we later wanneer we de "Nieuwe Melding" formulieren gaan opslaan
        [HttpPost]
        public async Task<ActionResult<Melding>> PostMelding(Melding melding)
        {
            // Zorgt dat de datum altijd actueel is bij het opslaan als de frontend niks meestuurt
            if (melding.Datum == default)
            {
                melding.Datum = DateTime.Now;
            }

            _context.Meldingen.Add(melding);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeldingen), new { id = melding.Id }, melding);
        }
    }
}