using Microsoft.AspNetCore.Mvc;

namespace SimpleAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EchoController : ControllerBase
    {
        [HttpPost]
        public ActionResult<EchoResponse> Post([FromBody] EchoRequest request)
        {
            return Ok(new EchoResponse
            {
                Result = $"Hello! You sent: \"{request.Input}\" (received at {DateTime.Now:HH:mm:ss})"
            });
        }
    }

    public class EchoRequest
    {
        public string Input { get; set; } = string.Empty;
    }

    public class EchoResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}
