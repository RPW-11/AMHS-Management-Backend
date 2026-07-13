using API.Contracts.Employee;
using Application.DTOs.Common;
using Application.DTOs.Employee;
using Application.Services.EmployeeService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/employees")]
    [ApiController]
    [Authorize]
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
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of employees per page (clamped between 5 and 100).</param>
        /// <param name="searchTerm">Optional filter matched against employee name/email.</param>
        [HttpGet]
        public async Task<ActionResult<PagedResult<EmployeeDto>>> GetAllEmployees(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null
        )
        {
            FluentResults.Result<PagedResult<EmployeeDto>> employeesResult = await _employeeService.GetAllEmployees(page, pageSize, searchTerm);

            return HandleResult(employeesResult);
        }

        /// <summary>
        /// Get employees by name (starts with First Name or Last Name)
        /// </summary>
        /// <param name="name">The name prefix to search for.</param>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<EmployeeSearchDto>>> GetEmployeesByName(string name)
        {
            FluentResults.Result<IEnumerable<EmployeeSearchDto>> employeesResult = await _employeeService.GetEmployeesByName(name);

            return HandleResult(employeesResult);
        }

        /// <summary>
        /// Get an employee by id
        /// </summary>
        /// <param name="id">The employee id.</param>
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
        /// <param name="addEmployeeRequest">The new employee's details.</param>
        /// <returns>201 Created on success.</returns>
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
