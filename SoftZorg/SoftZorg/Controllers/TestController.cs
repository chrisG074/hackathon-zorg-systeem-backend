using Microsoft.AspNetCore.Mvc;

namespace SoftZorg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("test successfull");
        }
    }
}
