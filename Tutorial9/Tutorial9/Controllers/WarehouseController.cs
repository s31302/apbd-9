using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        
        private readonly IDbService _dbService;

        public WarehouseController(IDbService dbService)
        {
            _dbService = dbService;
        }
        
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WarehouseDTO warehouse)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _dbService.DoSomethingAsync(warehouse);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
