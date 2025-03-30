using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SignalGenerator.Data.Services;
using SignalGenerator.Data.Models;
using SignalGenerator.Data.Interfaces;

namespace SignalGenerator.Web.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TestingController : ControllerBase
    {
        private readonly ISignalTestingService _testingService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly ISystemEvaluationService _evaluationService;
        private readonly ILogger<TestingController> _logger;

        public TestingController(
            ISignalTestingService testingService,
            IErrorHandlingService errorHandlingService,
            ISystemEvaluationService evaluationService,
            ILogger<TestingController> logger)
        {
            _testingService = testingService;
            _errorHandlingService = errorHandlingService;
            _evaluationService = evaluationService;
            _logger = logger;
        }

        // ??? ??? ????? ??????? ?? ???????? ? ?????? ??? ?? ???? ??????
        private async Task<IActionResult> ExecuteWithLoggingAsync<T>(string operation, Func<Task<T>> func)
        {
            try
            {
                _logger.LogInformation("Executing {Operation}", operation);
                var result = await func();
                return Ok(new { success = true, result });
            }
            catch (Exception ex)
            {
                _errorHandlingService.LogError(operation, ex);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // ??? ????? ??? ?????? ?? ??????? ?? ExecuteWithLoggingAsync
        [HttpPost("test-transmission")]
        public Task<IActionResult> TestSignalTransmission([FromBody] SignalConfig config)
            => ExecuteWithLoggingAsync("SignalTransmission", () => _testingService.TestSignalTransmissionAsync(config));

        [HttpGet("test-status")]
        public async Task<IActionResult> GetTestStatus()
        {
            try
            {
                // ????? ?? ??? ??? ? ???????? ???????
                var result = await ExecuteWithLoggingAsync("SystemStatus", async () => new
                {
                    status = _errorHandlingService.GetSystemStatus(),
                    recentErrors = _errorHandlingService.GetRecentErrors("component_name", 10) // ?? ????? ????????? ??????? component ?? ???? ????.
                });

                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }


        // ??? ?????? ?????? ????
        [HttpGet("errors")]
        public async Task<IActionResult> GetErrors([FromQuery] string component = null, [FromQuery] int count = 100)
        {
            return await ExecuteWithLoggingAsync("ErrorRetrieval", async () =>
                _errorHandlingService.GetRecentErrors(component, count)
            );
        }

        // ??? ??????? ?????
        [HttpPost("evaluate-system")]
        public Task<IActionResult> EvaluateSystem([FromBody] EvaluationConfig config)
            => ExecuteWithLoggingAsync("SystemEvaluation", () => _evaluationService.EvaluateSystemAsync(config));
    }
}
