using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DiscordBotServer.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DiscordBotServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PresetTextsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PresetTextsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PresetTexts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PresetText>>> GetPresetText()
        {
            return await _context.PresetText.AsQueryable().ToArrayAsync();
        }

        // GET: api/PresetTexts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PresetText>> GetPresetText(string id)
        {
            var presetText = await _context.PresetText.FindAsync(id);

            if (presetText == null)
            {
                return NotFound();
            }

            return presetText;
        }

        // PUT: api/PresetTexts/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPresetText(string id, PresetText presetText)
        {
            if (id != presetText.Id)
            {
                return BadRequest();
            }

            _context.Entry(presetText).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                if (!PresetTextExists(id))
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

        // POST: api/PresetTexts
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<PresetText>> PostPresetText(PresetText presetText)
        {
            _context.PresetText.Add(presetText);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (PresetTextExists(presetText.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetPresetText", new { id = presetText.Id }, presetText);
        }

        // DELETE: api/PresetTexts/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<PresetText>> DeletePresetText(string id)
        {
            var presetText = await _context.PresetText.FindAsync(id);
            if (presetText == null)
            {
                return NotFound();
            }

            _context.PresetText.Remove(presetText);
            await _context.SaveChangesAsync();

            return presetText;
        }

        private bool PresetTextExists(string id)
        {
            return _context.PresetText.Any(e => e.Id == id);
        }
    }
}
