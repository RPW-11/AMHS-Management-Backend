using Application.Common.Errors;
using Application.DTOs.Authentication;
using Microsoft.AspNetCore.Mvc;
using API.Contracts.Authentication;
using Application.Services.AuthenticationService;
using System.Net;

namespace API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationDto>> Login(LoginRequest request)
        {
            FluentResults.Result<AuthenticationDto> authResult = await _authenticationService.Login(request.Email, request.Password);

            if (authResult.IsSuccess)
            {
                return Ok(authResult.Value);
            }

            var firstError = authResult.Errors[0];

            if (firstError is ApplicationError)
            {
                return Problem(
                    statusCode: (int)HttpStatusCode.BadRequest,
                    title: firstError.Message,
                    detail: (string)firstError.Metadata["detail"]
                );
            }
            return Problem(statusCode: 500);
        }
    }
}
