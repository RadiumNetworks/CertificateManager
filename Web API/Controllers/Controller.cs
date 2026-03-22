using Microsoft.AspNetCore.Mvc;

namespace CertificateManager.Controllers
{
    [ApiController]
    [Route("api/parse")]
    public class ParseController : ControllerBase
    {
        [HttpPost]
        public ActionResult<ParseResponse> Post([FromBody] ParseRequest request)
        {
            return Ok(new ParseResponse
            {
                Result = $"Hello! You sent: \"{request.Input}\" (received at {DateTime.Now:HH:mm:ss})"
            });
        }
    }

    [ApiController]
    [Route("api/submit")]
    public class SubmitController : ControllerBase
    {
        [HttpPost]
        public ActionResult<SubmitResponse> Post([FromBody] SubmitRequest request)
        {
            return Ok(new SubmitResponse
            {
                Result = $"Hello! You sent: \"{request.Input}\" (received at {DateTime.Now:HH:mm:ss})"
            });
        }
    }

    public class ParseRequest
    {
        public string Input { get; set; } = string.Empty;
    }

    public class ParseResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    public class SubmitRequest
    {
        public string Input { get; set; } = string.Empty;
    }

    public class SubmitResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}
