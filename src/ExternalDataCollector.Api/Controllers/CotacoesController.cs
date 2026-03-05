using ExternalDataCollector.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace ExternalDataCollector.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CotacoesController : Controller
    {

        private readonly GetLatestRates _getLatestRates;


        public CotacoesController(GetLatestRates getLatestRates)
        {
            _getLatestRates = getLatestRates;
        }


        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest(string? quote, int? take, CancellationToken ct)
        {
            var data = await _getLatestRates.ExecuteAsync(quote, take ?? 50, ct);

            var dto = data.Select(x => new
            {
                x.BaseCurrency,
                x.QuoteCurrency,
                x.Rate,
                x.AsOfDate,
                x.RetrievedAt
            });

            return Ok(dto);
        }


        [HttpGet]
        public async Task<IActionResult> GetByDate([FromQuery] string date, [FromQuery] string? quote, [FromQuery] int? take, CancellationToken ct)
        {
            if (!DateOnly.TryParse(date, out var d))
                return BadRequest(new { error = "Parâmetro 'date' inválido. Use yyyy-MM-dd." });

            var data = await _getLatestRates.ExecuteByDateAsync(d, quote, take ?? 50, ct);

            var dto = data.Select(x => new
            {
                x.BaseCurrency,
                x.QuoteCurrency,
                x.Rate,
                x.AsOfDate,
                x.RetrievedAt
            });

            return Ok(dto);
        }
    }
}
