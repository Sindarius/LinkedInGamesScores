using Microsoft.AspNetCore.Mvc;

namespace game.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "API is working!";
        }
    }
}