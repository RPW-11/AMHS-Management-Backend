using Application.Common.Errors;
using Application.Services.Authentication;
using Application.DTOs.Authentication;
using Microsoft.AspNetCore.Mvc;
using API.Contracts.Authentication;

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

        [HttpPost("register")]
        public ActionResult<AuthenticationDto> Register(RegisterRequest request)
        {
            FluentResults.Result<AuthenticationDto> authResult = _authenticationService.Register(request.FirstName, request.LastName, request.Email, request.Password);

            if (authResult.IsSuccess)
            {
                return Ok(authResult.Value);
            }

            var firstError = authResult.Errors[0];

            if (firstError is ApplicationError)
            {
                return Problem(
                    statusCode: (int)firstError.Metadata["statusCode"],
                    title: firstError.Message,
                    detail: (string)firstError.Metadata["detail"]
                );
            }
            return Problem(statusCode: 500);
        }

        [HttpPost("login")]
        public ActionResult<AuthenticationDto> Login(LoginRequest request)
        {
            FluentResults.Result<AuthenticationDto> authResult = _authenticationService.Login(request.Email, request.Password);

            if (authResult.IsSuccess)
            {
                return Ok(authResult.Value);
            }

            var firstError = authResult.Errors[0];

            if (firstError is ApplicationError)
            {
                return Problem(
                    statusCode: (int)firstError.Metadata["statusCode"],
                    title: firstError.Message,
                    detail: (string)firstError.Metadata["detail"]
                );
            }
            return Problem(statusCode: 500);
        }
    }
}
