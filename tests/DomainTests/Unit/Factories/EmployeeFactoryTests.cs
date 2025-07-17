using Domain.Exceptions;
using Domain.Factories;
using Domain.Interfaces;
using Domain.ValueObjects;
using Moq;

namespace DomainTests.Unit.Factories;

public class EmployeeFactoryTests
{
    private readonly Mock<IEmployeeUniquenessChecker> _uniquenessCheckerMock;
    private readonly EmployeeFactory _factory;

    public EmployeeFactoryTests()
    {
        _uniquenessCheckerMock = new Mock<IEmployeeUniquenessChecker>();
        _factory = new EmployeeFactory(_uniquenessCheckerMock.Object);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithUniqueEmail_CreatesEmployeeSuccessfully()
    {
        // Arrange
        const string email = "unique@example.com";
        const string firstName = "John";
        const string lastName = "Doe";
        const string position = "Staff";
        
        _uniquenessCheckerMock
            .Setup(x => x.IsEmailUniqueAsync(email))
            .ReturnsAsync(true);

        // Act
        var employee = await _factory.CreateEmployeeAsync(email, firstName, lastName, position);

        // Assert
        Assert.NotNull(employee);
        Assert.Equal(email, employee.Email.Value);
        Assert.Equal(firstName, employee.FirstName);
        Assert.Equal(lastName, employee.LastName);
        Assert.Equal(EmployeePosition.Staff, employee.Position);
        
        _uniquenessCheckerMock.Verify(x => x.IsEmailUniqueAsync(email), Times.Once);
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithDuplicateEmail_ThrowsDuplicateEmployeeEmailException()
    {
        // Arrange
        const string duplicateEmail = "duplicate@example.com";
        
        _uniquenessCheckerMock
            .Setup(x => x.IsEmailUniqueAsync(duplicateEmail))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DuplicateEmployeeEmailException>(
            () => _factory.CreateEmployeeAsync(duplicateEmail, "John", "Doe", "Manager"));
        
        Assert.Equal(duplicateEmail, exception.Email);
        _uniquenessCheckerMock.Verify(x => x.IsEmailUniqueAsync(duplicateEmail), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateEmployeeAsync_WithInvalidFirstName_ThrowsDomainException(string invalidFirstName)
    {
        // Arrange
        _uniquenessCheckerMock
            .Setup(x => x.IsEmailUniqueAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<EmptyEmployeeNameException>(
            () => _factory.CreateEmployeeAsync("test@example.com", invalidFirstName, "Doe", "Staff"));
    }

    [Theory]
    [InlineData("invalidPosition")]
    [InlineData("")]
    public async Task CreateEmployeeAsync_WithInvalidPosition_ThrowsDomainException(string invalidPosition)
    {
        // Arrange
        _uniquenessCheckerMock
            .Setup(x => x.IsEmailUniqueAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidEmployeePositionException>(
            () => _factory.CreateEmployeeAsync("test@example.com", "John", "Doe", invalidPosition));
    }

    [Fact]
    public async Task CreateEmployeeAsync_WithValidPosition_CorrectlyMapsToEnum()
    {
        // Arrange
        const string email = "test@example.com";
        _uniquenessCheckerMock
            .Setup(x => x.IsEmailUniqueAsync(email))
            .ReturnsAsync(true);

        // Act
        var seniorStaff = await _factory.CreateEmployeeAsync(email, "John", "Doe", "Senior Staff");
        var manager = await _factory.CreateEmployeeAsync(email, "Jane", "Smith", "Manager");

        // Assert
        Assert.Equal(EmployeePosition.SeniorStaff, seniorStaff.Position);
        Assert.Equal(EmployeePosition.Manager, manager.Position);
    }
}