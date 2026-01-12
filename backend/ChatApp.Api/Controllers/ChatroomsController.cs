using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatroomsController : ControllerBase
{
    private readonly IChatroomService _chatroomService;
    private readonly ILogger<ChatroomsController> _logger;

    public ChatroomsController(IChatroomService chatroomService, ILogger<ChatroomsController> logger)
    {
        _chatroomService = chatroomService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChatroomDto>>> GetChatrooms()
    {
        var chatrooms = await _chatroomService.GetAllChatroomsAsync();
        var chatroomDtos = chatrooms.Select(c => new ChatroomDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            CreatedAt = c.CreatedAt
        });

        return Ok(chatroomDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChatroomDto>> GetChatroom(int id)
    {
        var chatroom = await _chatroomService.GetChatroomByIdAsync(id);
        if (chatroom == null)
            return NotFound();

        return Ok(new ChatroomDto
        {
            Id = chatroom.Id,
            Name = chatroom.Name,
            Description = chatroom.Description,
            CreatedAt = chatroom.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<ChatroomDto>> CreateChatroom([FromBody] CreateChatroomDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var chatroom = await _chatroomService.CreateChatroomAsync(model.Name, model.Description);

        return CreatedAtAction(nameof(GetChatroom), new { id = chatroom.Id }, new ChatroomDto
        {
            Id = chatroom.Id,
            Name = chatroom.Name,
            Description = chatroom.Description,
            CreatedAt = chatroom.CreatedAt
        });
    }
}
