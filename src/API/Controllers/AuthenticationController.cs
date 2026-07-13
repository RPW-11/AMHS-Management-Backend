using Application.DTOs.Authentication;
using Microsoft.AspNetCore.Mvc;
using API.Contracts.Authentication;
using Application.Services.AuthenticationService;

namespace API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ApiBaseController
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Authenticate with an email and password and receive a JWT access token.
        /// </summary>
        /// <param name="request">The login email and password.</param>
        /// <returns>The issued access token and associated authentication details.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationDto>> Login(LoginRequest request)
        {
            FluentResults.Result<AuthenticationDto> authResult = await _authenticationService.Login(request.Email, request.Password);

            return HandleResult(authResult);
        }
    }
}
