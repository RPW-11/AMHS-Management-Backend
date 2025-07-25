using API.Contracts.Employee;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.DTOs.Employee;
using Application.Services.EmployeeService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/employees")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IUnitOfWork _unitOfWork;

        public EmployeeController(IEmployeeService employeeService, IUnitOfWork unitOfWork)
        {
            _employeeService = employeeService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all employees
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAllEmployees()
        {
            FluentResults.Result<IEnumerable<EmployeeDto>> employeesResult = await _employeeService.GetAllEmployees();

            if (employeesResult.IsSuccess)
            {
                await _unitOfWork.SaveChangesAsync();
                return Ok(employeesResult.Value);
            }

            var firstError = employeesResult.Errors[0];

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

        /// <summary>
        /// Get all employee by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeById(string id)
        {
            FluentResults.Result<EmployeeDto> employeeResult = await _employeeService.GetEmployee(id);

            if (employeeResult.IsSuccess)
            {
                await _unitOfWork.SaveChangesAsync();
                return Ok(employeeResult.Value);
            }

            var firstError = employeeResult.Errors[0];

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

        /// <summary>
        /// Add an employee
        /// </summary>
        /// <remarks>
        /// Position options: "Staff", "SeniorStaff", "Supervisor", "Manager"
        /// 
        /// Sample request:
        ///
        ///     POST /employees
        ///     {
        ///         "firstName": "John",
        ///         "lastName": "Doe",
        ///         "email": "john.doe@example.com",
        ///         "password": "securePassword123",
        ///         "position": "staff",
        ///         "phoneNumber": "+1234567890",
        ///         "dateOfBirth": "1990-01-01"
        ///     }
        ///
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult> AddEmploye(AddEmployeeRequest addEmployeeRequest)
        {
            FluentResults.Result addEmployeeResult = await _employeeService.AddEmployee(
                addEmployeeRequest.FirstName,
                addEmployeeRequest.LastName,
                addEmployeeRequest.Email,
                addEmployeeRequest.Password,
                addEmployeeRequest.Position,
                addEmployeeRequest.PhoneNumber,
                addEmployeeRequest.DateOfBirth
            );

            if (addEmployeeResult.IsSuccess)
            {
                await _unitOfWork.SaveChangesAsync();
                return Created();
            }

            var firstError = addEmployeeResult.Errors[0];

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
