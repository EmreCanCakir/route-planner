using Map.Services;
using Microsoft.AspNetCore.Mvc;

namespace Map.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistanceMatrixController : ControllerBase
    {
        private readonly DistanceMatrixService _distanceMatrixService;

        public DistanceMatrixController(DistanceMatrixService distanceMatrixService)
        {
            _distanceMatrixService = distanceMatrixService;
        }

        [HttpPost("matrix")]
        public async Task<IActionResult> GetDistanceMatrix([FromBody] List<Coordinate> coordinates)
        {
            try
            {
                var result = await _distanceMatrixService.GetDistanceMatrixAsync(coordinates);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
