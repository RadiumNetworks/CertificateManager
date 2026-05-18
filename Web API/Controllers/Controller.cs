using Microsoft.AspNetCore.Mvc;
using CertificateManager.Services;
using CertificateManager.Models;
using System.Runtime.InteropServices;

namespace CertificateManager.Controllers
{
    [ApiController]
    [Route("api/parse")]
    public class ParseController : ControllerBase
    {
        private readonly CertificateService _certificateService;

        private readonly Validation _validation;

        public ParseController(Validation validation)
        {
            _validation = validation;
        }

        [HttpPost]
        public ActionResult<ParseResponse> Post([FromBody] ParseRequest request)
        {
            var result = _validation.ParseRequest(request.Input);

            return Ok(new ParseResponse
            {
                Result = $"{result.Status}; {result.ParsedData}; {result.ChallengeData}; {result.Message}"
            });
        }
    }

    [ApiController]
    [Route("api/submit")]
    public class SubmitController : ControllerBase
    {
        private readonly Validation _validation;

        public SubmitController(Validation validation)
        {
            _validation = validation;
        }

        [HttpPost]
        public async Task<ActionResult<SubmitResponse>> Post([FromBody] SubmitRequest request)
        {
            var verifyResult = await _validation.VerifyAllChallenges(request.Input);
            bool allChallengesPassed = verifyResult.Status == "Success";

            var submitResult = _validation.SubmitToCA(request.Input, request.CAConfig, allChallengesPassed);
            return Ok(new SubmitResponse
            {
                Result = $"{submitResult.Status}; {verifyResult.Output}; {submitResult.Output}; {submitResult.Message}"
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
        public string CAConfig { get; set; } = string.Empty;
    }

    public class SubmitResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}
