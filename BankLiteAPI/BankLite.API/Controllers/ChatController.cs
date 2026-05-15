using BankLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankLite.Application.DTOs;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IGroqService _groqService;

        public ChatController(IGroqService groqService)
        {
            _groqService = groqService;
        }

        [HttpPost("message")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
                return BadRequest(new { message = "Message cannot be empty" });
            if (message.Content.Length > 200)
                return BadRequest(new { message = "Message cannot exceed 200 characters" });

            var response = await _groqService.GetChatResponseAsync(message.Content);
            return Ok(new { response });
        }
    }
}