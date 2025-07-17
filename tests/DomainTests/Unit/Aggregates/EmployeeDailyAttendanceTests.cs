using Domain.Aggregates;
using Domain.Exceptions;

namespace Domain.UnitTests.Aggregates;

public class EmployeeDailyAttendanceTests
{
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly DateTime _today = DateTime.Today;
    
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Act
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        
        // Assert
        Assert.Equal(_employeeId, attendance.EmployeeId);
        Assert.Equal(_today, attendance.Date);
        Assert.Equal(EmployeeAttendanceStatus.Absent, attendance.Status);
        Assert.Null(attendance.CheckInTime);
        Assert.Null(attendance.CheckOutTime);
        Assert.Empty(attendance.Breaks);
    }

    [Theory]
    [InlineData("07:59:59")]
    [InlineData("08:00:00")]
    public void CheckIn_OnTime_ShouldMarkAsPresent(string checkInTime)
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        var checkInDateTime = _today.Add(TimeSpan.Parse(checkInTime));
        
        // Act
        attendance.CheckIn(checkInDateTime);
        
        // Assert
        Assert.Equal(TimeSpan.Parse(checkInTime), attendance.CheckInTime);
        Assert.Equal(EmployeeAttendanceStatus.Present, attendance.Status);
    }

    [Theory]
    [InlineData("08:00:01")]
    [InlineData("09:30:00")]
    public void CheckIn_Late_ShouldMarkAsLate(string checkInTime)
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        var checkInDateTime = _today.Add(TimeSpan.Parse(checkInTime));
        
        // Act
        attendance.CheckIn(checkInDateTime);
        
        // Assert
        Assert.Equal(TimeSpan.Parse(checkInTime), attendance.CheckInTime);
        Assert.Equal(EmployeeAttendanceStatus.Late, attendance.Status);
    }

    [Fact]
    public void CheckIn_WhenAlreadyCheckedIn_ShouldThrowException()
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        attendance.CheckIn(_today.AddHours(8));
        
        // Act & Assert
        Assert.Throws<AlreadyCheckedInException>(() => 
            attendance.CheckIn(_today.AddHours(8.5)));
    }

    [Fact]
    public void CheckOut_WithoutCheckIn_ShouldThrowException()
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        
        // Act & Assert
        Assert.Throws<MustCheckInFirstException>(() => 
            attendance.CheckOut(_today.AddHours(17)));
    }

    [Fact]
    public void CheckOut_WhenAlreadyCheckedOut_ShouldThrowException()
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        attendance.CheckIn(_today.AddHours(8));
        attendance.CheckOut(_today.AddHours(17));
        
        // Act & Assert
        Assert.Throws<AlreadyCheckedOutException>(() => 
            attendance.CheckOut(_today.AddHours(17.5)));
    }

    [Theory]
    [InlineData("17:00:00")]
    [InlineData("18:30:00")]
    public void CheckOut_AfterWorkHours_ShouldSucceed(string checkOutTime)
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        attendance.CheckIn(_today.AddHours(8));
        var checkOutDateTime = _today.Add(TimeSpan.Parse(checkOutTime));
        
        // Act
        attendance.CheckOut(checkOutDateTime);
        
        // Assert
        Assert.Equal(TimeSpan.Parse(checkOutTime), attendance.CheckOutTime);
        Assert.Empty(attendance.Breaks);
    }

    [Theory]
    [InlineData("11:50:00")]
    [InlineData("12:30:00")]
    [InlineData("13:00:00")]
    public void CheckOut_DuringBreakPeriod_ShouldAddBreak(string checkOutTime)
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        attendance.CheckIn(_today.AddHours(8));
        var checkOutDateTime = _today.Add(TimeSpan.Parse(checkOutTime));
        
        // Act
        attendance.CheckOut(checkOutDateTime);
        
        // Assert
        Assert.Equal(TimeSpan.Parse(checkOutTime), attendance.CheckOutTime);
        Assert.Single(attendance.Breaks);
        Assert.Equal(TimeSpan.Parse(checkOutTime), attendance.Breaks[0].StartTime);
        Assert.Null(attendance.Breaks[0].EndTime);
    }

    [Theory]
    [InlineData("11:49:59")]
    [InlineData("13:00:01")]
    [InlineData("16:59:59")]
    public void CheckOut_OutsideBreakPeriod_ShouldThrowException(string checkOutTime)
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        attendance.CheckIn(_today.AddHours(8));
        var checkOutDateTime = _today.Add(TimeSpan.Parse(checkOutTime));
        
        // Act & Assert
        Assert.Throws<NotWithinBreakCheckOutException>(() => 
            attendance.CheckOut(checkOutDateTime));
    }

    [Fact]
    public void ReturnFromBreak_WithoutActiveBreak_ShouldThrowException()
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        
        // Act & Assert
        Assert.Throws<NoActiveBreakException>(() => 
            attendance.ReturnFromBreak(_today.AddHours(12)));
    }

    [Theory]
    [InlineData("11:50:00", "12:00:00")]
    [InlineData("12:30:00", "12:45:00")]
    public void ReturnFromBreak_WithActiveBreak_ShouldCompleteBreak(
        string breakStartTime, string returnTime)
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        attendance.CheckIn(_today.AddHours(8));
        attendance.CheckOut(_today.Add(TimeSpan.Parse(breakStartTime)));
        
        // Act
        attendance.ReturnFromBreak(_today.Add(TimeSpan.Parse(returnTime)));
        
        // Assert
        var breakPeriod = attendance.Breaks.Single();
        Assert.Equal(TimeSpan.Parse(breakStartTime), breakPeriod.StartTime);
        Assert.Equal(TimeSpan.Parse(returnTime), breakPeriod.EndTime);
    }

    [Theory]
    [InlineData("11:49:59")]
    [InlineData("13:00:01")]
    public void ReturnFromBreak_OutsideBreakHours_ShouldThrowException(string returnTime)
    {
        // Arrange
        var attendance = new EmployeeDailyAttendance(_employeeId, _today);
        attendance.CheckIn(_today.AddHours(8));
        attendance.CheckOut(_today.AddHours(12)); // Start break at 12:00
        
        // Act & Assert
        Assert.Throws<NotWithinBreakCheckOutException>(() => 
            attendance.ReturnFromBreak(_today.Add(TimeSpan.Parse(returnTime))));
    }
}