using BankLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BankLite.Application.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Tags("Chat")]
    public class ChatController : ControllerBase
    {
        private readonly IGroqService _groqService;

        public ChatController(IGroqService groqService)
        {
            _groqService = groqService;
        }

        [HttpPost("message")]
        [SwaggerOperation(Summary = "Send chat message", Description = "Sends a message to the AI assistant and returns a response. Max 200 characters.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
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