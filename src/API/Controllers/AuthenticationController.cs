using Application.Services.Authentication;
using Contract;
using Contract.Authentication;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Register(RegisterRequest request)
        {
            var authResult = _authenticationService.Register(request.FirstName, request.LastName, request.Email, request.Password);

            var response = new AuthenticationResponse(
                authResult.Employee.Id,
                authResult.Employee.FirstName,
                authResult.Employee.LastName,
                authResult.Employee.Email.Value,
                authResult.Employee.ImgUrl,
                authResult.Token
            );

            return Ok(response);
        }
 
        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var authResult = _authenticationService.Login( request.Email, request.Password);

            var response = new AuthenticationResponse(
                authResult.Employee.Id,
                authResult.Employee.FirstName,
                authResult.Employee.LastName,
                authResult.Employee.Email.Value,
                authResult.Employee.ImgUrl,
                authResult.Token
            );

            return Ok(response);
        }
    }
}
