using Application.Common.Interfaces;

namespace Application.Services;

public class BaseService
{
    protected IUnitOfWork _unitOfWork;

    public BaseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
}
