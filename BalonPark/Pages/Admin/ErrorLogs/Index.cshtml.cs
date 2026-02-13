using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;

namespace BalonPark.Pages.Admin.ErrorLogs;

public class IndexModel : BaseAdminPage
{
    private readonly ErrorLogRepository _errorLogRepository;

    public IndexModel(ErrorLogRepository errorLogRepository)
    {
        _errorLogRepository = errorLogRepository;
    }

    public IEnumerable<ErrorLog> Logs { get; set; } = new List<ErrorLog>();
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 20;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    [BindProperty(SupportsGet = true)]
    public string? Level { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public Dictionary<string, int> CountByLevel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var (items, totalCount) = await _errorLogRepository.GetPagedAsync(CurrentPage, PageSize, Level, From, To);
        Logs = items;
        TotalCount = totalCount;

        CountByLevel = await _errorLogRepository.GetCountByLevelAsync();

        return Page();
    }

    /// <summary>
    /// Tek bir log kaydının detayını JSON döner (modal için).
    /// </summary>
    public async Task<IActionResult> OnGetDetailAsync(int id)
    {
        var log = await _errorLogRepository.GetByIdAsync(id);
        if (log == null)
            return NotFound();
        return new JsonResult(new
        {
            log.Id,
            log.Message,
            log.MessageTemplate,
            log.Level,
            TimeStamp = log.TimeStamp?.ToString("yyyy-MM-dd HH:mm:ss"),
            log.Exception,
            log.Properties
        });
    }
}
