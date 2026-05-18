using BankLite.Application.DTOs;
using BankLite.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace BankLite.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Tags("Chat")]
    public class ChatController : BaseController
    {
        private readonly IGroqService _groqService;
        private readonly IValidator<ChatMessageDto> _validator;

        public ChatController(IGroqService groqService, IValidator<ChatMessageDto> validator)
        {
            _groqService = groqService;
            _validator = validator;
        }

        [HttpPost("message")]
        [SwaggerOperation(Summary = "Send chat message", Description = "Sends a message to the AI assistant and returns a response. Max 200 characters.")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageDto message)
        {
            var validation = await _validator.ValidateAsync(message);
            if (!validation.IsValid)
                return BadRequest(validation.Errors);

            var response = await _groqService.GetChatResponseAsync(message.Content);
            return Ok(new { response });
        }
    }
}