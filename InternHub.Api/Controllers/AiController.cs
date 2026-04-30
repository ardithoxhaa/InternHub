using InternHub.Api.Contracts;
using InternHub.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternHub.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController(AiChatService aiChat) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponseDto>> Chat(AiChatRequestDto request)
    {
        return Ok(await aiChat.ChatAsync(request));
    }
}
