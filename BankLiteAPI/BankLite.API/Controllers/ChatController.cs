using BankLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
                return BadRequest(new { message = "Message cannot be empty" });

            var response = await _groqService.GetChatResponseAsync(message.Content);
            return Ok(new { response });
        }
    }

    public record ChatMessageDto(string Content);
}