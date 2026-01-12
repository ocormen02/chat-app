using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/chatrooms/{chatroomId}/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IChatroomService _chatroomService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMessageService messageService,
        IChatroomService chatroomService,
        UserManager<ApplicationUser> userManager,
        ILogger<MessagesController> logger)
    {
        _messageService = messageService;
        _chatroomService = chatroomService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(int chatroomId, [FromQuery] int limit = 50)
    {
        if (!await _chatroomService.ChatroomExistsAsync(chatroomId))
            return NotFound("Chatroom not found");

        if (limit > 100) limit = 100; // Max limit
        if (limit < 1) limit = 50; // Default limit

        var messages = await _messageService.GetLatestMessagesAsync(chatroomId, limit);
        var userIds = messages.Select(m => m.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync();

        var messageDtos = messages.Select(m =>
        {
            var user = users.FirstOrDefault(u => u.Id == m.UserId);
            return new MessageDto
            {
                Id = m.Id,
                UserId = m.UserId,
                UserName = user?.UserName,
                UserDisplayName = user?.DisplayName,
                ChatroomId = m.ChatroomId,
                Content = m.Content,
                Timestamp = m.Timestamp,
                IsBotMessage = m.IsBotMessage
            };
        });

        return Ok(messageDtos);
    }
}
