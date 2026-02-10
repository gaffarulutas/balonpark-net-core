using Microsoft.AspNetCore.Mvc;
using BalonPark.Services;
using BalonPark.Models;

namespace BalonPark.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MailsController(IMailService mailService, ILogger<MailsController> logger) : ControllerBase
{

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardData([FromQuery] string folder = "INBOX", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Tek IMAP bağlantısı ile tüm verileri al
            var messages = await mailService.GetFolderMessagesAsync(folder, page, pageSize);
            var stats = await mailService.GetEmailStatsAsync();
            var folders = await mailService.GetFoldersAsync();
            
            return Ok(new { 
                success = true, 
                data = new {
                    messages = messages,
                    stats = stats,
                    folders = folders
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dashboard data");
            return BadRequest(new { success = false, message = "Dashboard verileri alınırken hata oluştu" });
        }
    }

    [HttpGet("folders")]
    public async Task<IActionResult> GetFolders()
    {
        try
        {
            var folders = await mailService.GetFoldersAsync();
            return Ok(new { success = true, data = folders });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting folders");
            return BadRequest(new { success = false, message = "Klasörler alınırken hata oluştu" });
        }
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] string folder = "INBOX", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var messages = await mailService.GetFolderMessagesAsync(folder, page, pageSize);
            return Ok(new { success = true, data = messages });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting messages");
            return BadRequest(new { success = false, message = "Mesajlar alınırken hata oluştu" });
        }
    }

    [HttpGet("message/{folder}/{uid}")]
    public async Task<IActionResult> GetMessage(string folder, uint uid)
    {
        try
        {
            var message = await mailService.GetMessageByUidAsync(folder, uid);
            if (message == null)
                return NotFound(new { success = false, message = "Mesaj bulunamadı" });

            return Ok(new { success = true, data = message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting message");
            return BadRequest(new { success = false, message = "Mesaj alınırken hata oluştu" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await mailService.GetEmailStatsAsync();
            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting stats");
            return BadRequest(new { success = false, message = "İstatistikler alınırken hata oluştu" });
        }
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailModel model)
    {
        try
        {
            var result = await mailService.SendEmailAsync(model);
            if (result)
                return Ok(new { success = true, message = "E-posta başarıyla gönderildi" });
            
            return BadRequest(new { success = false, message = "E-posta gönderilemedi" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email");
            return BadRequest(new { success = false, message = "E-posta gönderilirken hata oluştu" });
        }
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkAsRead([FromBody] MailActionRequest request)
    {
        try
        {
            var result = await mailService.MarkAsReadAsync(request.Folder, request.Uid);
            return Ok(new { success = result, message = result ? "Okundu olarak işaretlendi" : "İşlem başarısız" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking as read");
            return BadRequest(new { success = false, message = "İşlem başarısız oldu" });
        }
    }

    [HttpPost("mark-unread")]
    public async Task<IActionResult> MarkAsUnread([FromBody] MailActionRequest request)
    {
        try
        {
            var result = await mailService.MarkAsUnreadAsync(request.Folder, request.Uid);
            return Ok(new { success = result, message = result ? "Okunmadı olarak işaretlendi" : "İşlem başarısız" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking as unread");
            return BadRequest(new { success = false, message = "İşlem başarısız oldu" });
        }
    }

    [HttpPost("toggle-flag")]
    public async Task<IActionResult> ToggleFlag([FromBody] MailActionRequest request)
    {
        try
        {
            var result = await mailService.ToggleFlagAsync(request.Folder, request.Uid);
            return Ok(new { success = result, message = result ? "İşaretleme güncellendi" : "İşlem başarısız" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling flag");
            return BadRequest(new { success = false, message = "İşlem başarısız oldu" });
        }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteMessage([FromBody] MailActionRequest request)
    {
        try
        {
            var result = await mailService.DeleteMessageAsync(request.Folder, request.Uid);
            return Ok(new { success = result, message = result ? "Mesaj silindi" : "İşlem başarısız" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting message");
            return BadRequest(new { success = false, message = "İşlem başarısız oldu" });
        }
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveMessage([FromBody] MoveMailRequest request)
    {
        try
        {
            var result = await mailService.MoveToFolderAsync(request.SourceFolder, request.Uid, request.TargetFolder);
            return Ok(new { success = result, message = result ? "Mesaj taşındı" : "İşlem başarısız" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error moving message");
            return BadRequest(new { success = false, message = "İşlem başarısız oldu" });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchMessages([FromQuery] string query, [FromQuery] string folder = "INBOX")
    {
        try
        {
            var messages = await mailService.SearchMessagesAsync(query, folder);
            return Ok(new { success = true, data = messages });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching messages");
            return BadRequest(new { success = false, message = "Arama sırasında hata oluştu" });
        }
    }

}

public class MailActionRequest
{
    public string Folder { get; set; } = string.Empty;
    public uint Uid { get; set; }
}

public class MoveMailRequest : MailActionRequest
{
    public string SourceFolder { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;
}

