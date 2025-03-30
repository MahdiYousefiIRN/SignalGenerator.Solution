using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalGenerator.Data.Services;
using System.Text;

namespace SignalGenerator.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly IDataExportService _exportService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(IDataExportService exportService, ILogger<ExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        [HttpGet("csv")]
        public async Task<IActionResult> ExportToCsv(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] string protocolType = null)
        {
            try
            {
                var data = await _exportService.ExportToCsvAsync(startTime, endTime, protocolType);
                var fileName = $"signals_{startTime:yyyyMMdd_HHmmss}_{endTime:yyyyMMdd_HHmmss}.csv";
                
                return File(data, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to CSV");
                return StatusCode(500, "Error exporting data to CSV");
            }
        }

        [HttpGet("json")]
        public async Task<IActionResult> ExportToJson(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] string protocolType = null)
        {
            try
            {
                var data = await _exportService.ExportToJsonAsync(startTime, endTime, protocolType);
                var fileName = $"signals_{startTime:yyyyMMdd_HHmmss}_{endTime:yyyyMMdd_HHmmss}.json";
                
                return File(data, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to JSON");
                return StatusCode(500, "Error exporting data to JSON");
            }
        }

        [HttpGet("excel")]
        public async Task<IActionResult> ExportToExcel(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] string protocolType = null)
        {
            try
            {
                var data = await _exportService.ExportToExcelAsync(startTime, endTime, protocolType);
                var fileName = $"signals_{startTime:yyyyMMdd_HHmmss}_{endTime:yyyyMMdd_HHmmss}.xlsx";
                
                return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data to Excel");
                return StatusCode(500, "Error exporting data to Excel");
            }
        }
    }
} 