using System;

namespace Domain.Exceptions;

public sealed class AlreadyCheckedInException : DomainException
{
    public AlreadyCheckedInException() : base($"already checked in today")
    {

    }
}

public sealed class MustCheckInFirstException : DomainException
{
    public MustCheckInFirstException() : base($"you must check in first")
    {

    }
}

public sealed class AlreadyCheckedOutException : DomainException
{
    public AlreadyCheckedOutException() : base($"already checked out today")
    {

    }
}

public sealed class NotWithinBreakCheckOutException : DomainException
{
    public NotWithinBreakCheckOutException() : base($"break returns only allowed between 11:50-13:00")
    {

    }
}

public sealed class NoActiveBreakException : DomainException
{
    public NoActiveBreakException() : base("no active break o return from")
    {

    }
}
