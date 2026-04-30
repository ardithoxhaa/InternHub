using System.ComponentModel.DataAnnotations;

namespace InternHub.Api.Contracts;

public record AiChatMessageDto([Required] string Role, [Required] string Content);
public record AiChatRequestDto([Required] string Message, IEnumerable<AiChatMessageDto>? History);
public record AiChatResponseDto(string Reply, bool UsedOpenAi);
