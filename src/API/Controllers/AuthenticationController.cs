using Application.Common.Errors;
using Application.Services.Authentication;
using Application.DTOs.Authentication;
using Microsoft.AspNetCore.Mvc;
using API.Contracts.Authentication;
using Application.Common.Interfaces;

namespace API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationController(IAuthenticationService authenticationService, IUnitOfWork unitOfWork)
        {
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationDto>> Register(RegisterRequest request)
        {
            FluentResults.Result<AuthenticationDto> authResult = await _authenticationService.Register(request.FirstName, request.LastName, request.Email, request.Password);

            if (authResult.IsSuccess)
            {
                await _unitOfWork.SaveChangesAsync();
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
        public async Task<ActionResult<AuthenticationDto>> Login(LoginRequest request)
        {
            FluentResults.Result<AuthenticationDto> authResult = await _authenticationService.Login(request.Email, request.Password);

            if (authResult.IsSuccess)
            {
                await _unitOfWork.SaveChangesAsync();
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
