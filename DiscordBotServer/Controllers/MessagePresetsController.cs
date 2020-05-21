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
    [ApiController]
    public class MessagePresetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessagePresetsController(AppDbContext context)
        {
            _context = context;
        }

        [Route("api/MessagePresetsBatch")]
        [HttpPost]
        public async Task<ActionResult> BatchAdd([FromBody] MessagePreset[] inputs)
        {
            _context.MessagePreset.AddRange(inputs);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
