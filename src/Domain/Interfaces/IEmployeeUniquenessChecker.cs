using System;

namespace Domain.Interfaces;

public interface IEmployeeUniquenessChecker
{
    Task<bool> IsEmailUniqueAsync(string email);
}
