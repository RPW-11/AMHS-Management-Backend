using System.Net;
using API.Contracts.Employee;
using Application.Common.Errors;
using Application.DTOs.Employee;
using Application.Services.EmployeeService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/employees")]
    [ApiController]
    public class EmployeeController : ApiBaseController
    {
        private readonly IEmployeeService _employeeService;

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        /// <summary>
        /// Get all employees
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAllEmployees()
        {
            FluentResults.Result<IEnumerable<EmployeeDto>> employeesResult = await _employeeService.GetAllEmployees();

            return HandleResult(employeesResult);
        }

        /// <summary>
        /// Get an employee by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeById(string id)
        {
            FluentResults.Result<EmployeeDto> employeeResult = await _employeeService.GetEmployee(id);

            return HandleResult(employeeResult);
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
        public async Task<ActionResult<object>> AddEmploye(AddEmployeeRequest addEmployeeRequest)
        {
            FluentResults.Result<object> addEmployeeResult = await _employeeService.AddEmployee(
                addEmployeeRequest.FirstName,
                addEmployeeRequest.LastName,
                addEmployeeRequest.Email,
                addEmployeeRequest.Password,
                addEmployeeRequest.Position,
                addEmployeeRequest.PhoneNumber,
                addEmployeeRequest.DateOfBirth
            );

            if (addEmployeeResult.IsFailed)
            {
                return HandleResult(addEmployeeResult);
            }

            return Created();
        }
    }
}
