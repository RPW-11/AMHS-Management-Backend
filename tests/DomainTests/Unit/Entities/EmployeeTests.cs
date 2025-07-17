using Domain.Entities;
using Domain.ValueObjects;
using Domain.Exceptions;

namespace DomainTests.Unit.Entities;

public class EmployeeTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesEmployee()
    {
        var email = new EmployeeEmail("valid@example.com");
        var position = EmployeePositionExtension.ToEmployeePosition("Staff");
        var firstName = "John";
        var lastName = "Doe";

        var employee = new Employee(email, firstName, lastName, position);

        Assert.Equal(firstName, employee.FirstName);
        Assert.Equal(lastName, employee.LastName);
        Assert.Equal(position, employee.Position);
    }

    [Fact]
    public void Constructor_EmptyPosition_ThrowsDomainException()
    {
        var email = new EmployeeEmail("valid@example.com");
        var invalidPosition = "";

        Assert.Throws<InvalidEmployeePositionException>(() => 
        {
            var position = EmployeePositionExtension.ToEmployeePosition(invalidPosition);
            new Employee(email, "John", "Doe", position);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidFirstName_ThrowsException(string invalidFirstName)
    {
        var email = new EmployeeEmail("valid@example.com");
        var position = EmployeePositionExtension.ToEmployeePosition("Staff");

        Assert.Throws<EmptyEmployeeNameException>(() => 
            new Employee(email, invalidFirstName, "Doe", position));
    }
}