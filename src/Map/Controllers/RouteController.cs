using Map.Services;
using Microsoft.AspNetCore.Mvc;

namespace Map.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RouteController : ControllerBase
    {
        private readonly RouteOptimizationService _routeOptimizationService;
        private readonly VrpGlobalSpan _vrpGlobalSpan;

        public RouteController(RouteOptimizationService routeOptimizationService, VrpGlobalSpan vrpGlobalSpan)
        {
            _routeOptimizationService = routeOptimizationService;
            _vrpGlobalSpan = vrpGlobalSpan;
        }

        [HttpPost("optimize")]
        public IActionResult OptimizeRoutes([FromBody] RouteOptimizationRequest request)
        {
            try
            {
                //_vrpGlobalSpan.Main(request.DistanceMatrix, request.VehicleCount);
                var result = _routeOptimizationService.CalculateOptimalRoutes(request.DistanceMatrix, request.VehicleCount);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class RouteOptimizationRequest
    {
        public List<List<int>> DistanceMatrix { get; set; } // Mesafe matrisi
        public int VehicleCount { get; set; } // Araç (kurye) sayısı
    }

}
