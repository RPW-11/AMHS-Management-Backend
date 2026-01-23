using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.RoutePlanning;
using Application.DTOs.Common;
using Application.DTOs.Mission;
using Application.DTOs.Mission.RoutePlanning;
using Domain.Employee;
using Domain.Employee.ValueObjects;
using Domain.Mission;
using Domain.Mission.ValueObjects;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Application.Services.MissionService;

public class MissionService : BaseService, IMissionService
{
    private readonly IMissionRepository _missionRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRgvRoutePlanning _rgvRoutePlanning;
    private readonly ILogger<MissionService> _logger;

    public MissionService(IMissionRepository missionRepository,
                          IEmployeeRepository employeeRepository,
                          IRgvRoutePlanning rgvRoutePlanning,
                          IUnitOfWork unitOfWork,
                          ILogger<MissionService> logger)
    : base(unitOfWork)
    {
        _missionRepository = missionRepository;
        _employeeRepository = employeeRepository;
        _rgvRoutePlanning = rgvRoutePlanning;
        _logger = logger;
    }

    public async Task<Result<AddMissionDto>> AddMission(string employeeId,
                                                        string name,
                                                        string category,
                                                        string description,
                                                        DateTime finishedAt)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EmployeeId"] = employeeId,
            ["MissionName"] = name,
        });

        _logger.LogInformation("Add mission request started");

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid employee ID format: {ErrorMessage}",
                employeeIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid employee id"));
        }

        var existing = await _employeeRepository.GetEmployeeByIdAsync(employeeIdResult.Value);
        if (existing is null)
        {
            _logger.LogWarning("Employee not found - cannot create mission");
            return Result.Fail(ApplicationError.Validation("Non employee can't create a mission"));
        }

        _logger.LogDebug("Creator employee validated: {EmployeeId}", employeeId);

        var missionDomainResult = MissionFactory.CreateMission(employeeId,
                                                               name,
                                                               category,
                                                               description,
                                                               finishedAt);

        if (missionDomainResult.IsFailed)
        {
            var errorMsg = missionDomainResult.Errors[0].Message;
            _logger.LogWarning("Mission creation failed due to domain rules: {ErrorMessage}", errorMsg);
            return Result.Fail(ApplicationError.Validation(errorMsg));
        }
        
        var newMission = missionDomainResult.Value;

        var addMissionResult = await _missionRepository.AddMissionAsync(missionDomainResult.Value);
        if (addMissionResult.IsFailed)
        {
            _logger.LogError("Failed to persist new mission to repository. Mission name: {MissionName}: {ErrorMessage}",
            name,
            addMissionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Mission created successfully. Mission ID: {MissionId}, Name: {MissionName}",
                newMission.Id.ToString(),
                newMission.Name);

            return Result.Ok(new AddMissionDto(newMission.Id.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Database commit failed after adding mission. Mission ID: {MissionId}, Name: {MissionName}",
                newMission.Id.ToString(),
                newMission.Name);

            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result<PagedResult<MissionDto>>> GetAllMission(
        int page,
        int pageSize,
        string? status, 
        string? name, 
        string? employeeId
    )
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Page"] = page,
            ["PageSize"] = pageSize,
            ["HasStatus"] = !string.IsNullOrWhiteSpace(status),
            ["HasName"] = !string.IsNullOrWhiteSpace(name),
            ["HasEmployee"] = !string.IsNullOrWhiteSpace(employeeId)
        });

        var missionFilterResult = MissionFilterDto.Create(page, pageSize, employeeId, status, name);
        if (missionFilterResult.IsFailed)
        {
            return Result.Fail<PagedResult<MissionDto>>(ApplicationError.Validation(missionFilterResult.Errors[0].Message));
        }

        _logger.LogInformation("Get all missions paged request started");

        var missionFilterDto = missionFilterResult.Value;

        var getMissionsResult = await _missionRepository.GetAllMissionsAsync(missionFilterDto);
        if (getMissionsResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve paged missions (page {Page}, size {PageSize}): {ErrorMessage}",
            missionFilterDto.Page, missionFilterDto.PageSize, getMissionsResult.Errors[0].Message);
            return Result.Fail<PagedResult<MissionDto>>(ApplicationError.Internal);
        }

        var missions = getMissionsResult.Value.Items;
        _logger.LogDebug("Retrieved {FetchedCount} missions for page {Page}", 
            missions.Count(), missionFilterDto.Page);

        List<MissionDto> missionsDto = [];
        foreach (var mission in missions)
        {
            missionsDto.Add(MapToMissionDto(mission));
        }

        var pagedResult = new PagedResult<MissionDto>
        {
            Items = missionsDto,
            Page = getMissionsResult.Value.Page,
            PageSize = getMissionsResult.Value.PageSize,
            TotalCount = getMissionsResult.Value.TotalCount
        };

        _logger.LogInformation("Successfully returned {ItemCount} missions (page {Page} of {TotalPages})",
            pagedResult.Items.Count(),
            pagedResult.Page,
            (int)Math.Ceiling((double)getMissionsResult.Value.TotalCount / getMissionsResult.Value.PageSize));

        return Result.Ok(pagedResult);
    }

    public async Task<Result<MissionDetailDto>> GetMission(string id)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = id
        });

        _logger.LogInformation("Get mission detail request started");

        var missionIdResult = MissionId.FromString(id);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}",
                missionIdResult.Errors[0].Message);
            return Result.Fail<MissionDetailDto>(ApplicationError.Validation(missionIdResult.Errors[0].Message));
        }

        var missionId = missionIdResult.Value;

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionId);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
        }

        if (missionResult.Value is null)
        {
            _logger.LogInformation("Mission not found");
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The mission is not found"));
        }

        var mission = missionResult.Value;

        var leaderAssignment = mission.AssignedEmployees.FirstOrDefault(ae => ae.MissionRole == MissionRole.Leader);
        if (leaderAssignment is null) {
            _logger.LogWarning("No leader assigned to mission");
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The mission has no leader"));
        }

        var leaderResult = await _employeeRepository.GetEmployeeByIdAsync(leaderAssignment.EmployeeId);
        if (leaderResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve leader information. LeaderId: {LeaderId}: {ErrorMessage}",
                leaderAssignment.EmployeeId,
                leaderResult.Errors[0].Message);
            return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
        }
        if (leaderResult.Value is null)
        {
            _logger.LogWarning("Leader entity not found. LeaderId: {LeaderId}", leaderAssignment.EmployeeId);
            return Result.Fail<MissionDetailDto>(ApplicationError.NotFound("The leader of this mission does not exist"));
        }

        var leader = leaderResult.Value;

        if (mission.Category == MissionCategory.RoutePlanning
            && mission.ResourceLink is not null)
        {
            _logger.LogDebug("Mission is of type RoutePlanning - loading summary from JSON");

            RoutePlanningSummaryDto routePlanningSummary;
            try
            {
                routePlanningSummary = _rgvRoutePlanning.ReadFromJson(mission.ResourceLink);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read route planning summary from JSON. Path: {ResourceLink}",
                    mission.ResourceLink);
                return Result.Fail<MissionDetailDto>(ApplicationError.Internal);
            }

            var base64Images = new List<string>();
            foreach (var imgPath in routePlanningSummary.ImageUrls)
            {
                try
                {
                    var bytes = await File.ReadAllBytesAsync(imgPath);
                    base64Images.Add(Convert.ToBase64String(bytes));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read or convert image to base64. Path: {ImagePath}", imgPath);
                }
            }
            

            routePlanningSummary = new RoutePlanningSummaryDto(routePlanningSummary.Algorithm,
                                                               base64Images,
                                                               routePlanningSummary.RgvMap,
                                                               routePlanningSummary.Score);
                                        
            _logger.LogInformation("Successfully loaded route planning summary with {ImageCount} images",
                base64Images.Count);

            return MapToMissionDetailDto(mission, leaderResult.Value, routePlanningSummary);
        }

        _logger.LogInformation("Mission detail retrieved successfully. Category: {Category}",
            mission.Category);

        return MapToMissionDetailDto(mission, leaderResult.Value);
    }

    public async Task<Result> UpdateMission(UpdateMissionDto updateMissionDto, string missionId)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = missionId,
        });

        _logger.LogInformation("Update mission operation started");

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}",
                missionIdResult.Errors[0].Message ?? "Unknown error");
            return Result.Fail(ApplicationError.Validation("Invalid mission id"));
        }

        var missionRepoResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionRepoResult.IsFailed)
        {
            _logger.LogError("Failed to load mission from repository: {ErrorMessage}", missionRepoResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionRepoResult.Value is null)
        {
            _logger.LogInformation("Mission not found");
            return Result.Fail(ApplicationError.NotFound("The specified mission is not found"));
        }

        var statusResult = MissionStatus.FromString(updateMissionDto.Status);
        if (statusResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission status value: {ErrorMessage}",
                statusResult.Errors[0].Message ?? "Unknown error");
            return Result.Fail(ApplicationError.Validation("Invalid mission status"));
        }

        missionRepoResult.Value.SetMissionName(updateMissionDto.Name);
        missionRepoResult.Value.SetMissionDescription(updateMissionDto.Description);
        missionRepoResult.Value.SetMissionStatus(statusResult.Value);

        var updateMissionResult = _missionRepository.UpdateMission(missionRepoResult.Value);
        if (updateMissionResult.IsFailed)
        {
            _logger.LogError("Failed to mark mission for update in repository: {ErrorMessage}", updateMissionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Mission successfully updated");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed while updating mission");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result> DeleteMission(string missionId)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = missionId
        });

        _logger.LogInformation("Delete mission operation started");

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}",
                missionIdResult.Errors[0].Message ?? "Unknown parsing error");
            return Result.Fail(ApplicationError.Validation("Invalid missionId"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (missionResult.Value is null)
        {
            _logger.LogInformation("Mission not found, treated as success");
            return Result.Ok();
        }

        var deleteResult = _missionRepository.DeleteMission(missionResult.Value);
        if (deleteResult.IsFailed)
        {
            _logger.LogError("Failed to mark mission for deletion in repository: {ErrorMessage}", deleteResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Mission successfully deleted");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed during mission deletion");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result> DeleteMissions(IEnumerable<string> missionIds)
    {
        var missionIdList = missionIds?.ToList() ?? [];

        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionIds"] = string.Join(", ", missionIdList),
            ["MissionCountRequested"] = missionIdList.Count
        });

        _logger.LogInformation("Bulk delete missions request started. Count: {Count}", 
            missionIdList.Count);

        if (missionIdList.Count == 0)
        {
            _logger.LogWarning("No mission IDs provided for deletion");
            return Result.Fail(ApplicationError.NotFound("No mission ids provided"));
        }

        var missionIdsObjs = new List<MissionId>(missionIdList.Count);
        var invalidIds = new List<string>();

        foreach (var rawId in missionIdList)
        {
            var parseResult = MissionId.FromString(rawId);
            if (parseResult.IsFailed)
            {
                invalidIds.Add(rawId);
                continue;
            }
            missionIdsObjs.Add(parseResult.Value);
        }

        if (invalidIds.Count != 0)
        {
            _logger.LogWarning("Invalid mission ID format detected. Count: {InvalidCount}. Examples: {InvalidIds}", 
                invalidIds.Count, 
                string.Join(", ", invalidIds.Take(3)));

            return Result.Fail(ApplicationError.Validation(
                $"Invalid mission ID format for {invalidIds.Count} IDs"));
        }

        var deleteResult = await _missionRepository.DeleteMissionsAsync(missionIdsObjs);

        if (deleteResult.IsFailed)
        {
            _logger.LogError("Bulk delete failed in repository. Attempted count: {Count}: {ErrorMessage}",
                missionIdsObjs.Count, deleteResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        int deletedCount = deleteResult.Value;

        _logger.LogInformation("Bulk delete completed successfully. Deleted: {DeletedCount} / Requested: {RequestedCount}",
            deletedCount, missionIdList.Count);

        return Result.Ok();
    }

    public async Task<Result> AddMemberToMission(string employeeId, string missionId, string memberId)
    {   
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"]  = missionId,
            ["RequestedBy"] = employeeId,
            ["TargetMember"] = memberId
        });

        _logger.LogInformation("Add member to mission operation started");

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}", 
                missionIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid requester employee ID format: {ErrorMessage}", 
                employeeIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid employee Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to load mission from repository: {ErrorMessage}",
            employeeIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            _logger.LogInformation("Mission not found");
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        // Check if the requester is valid
        bool isRequesterValid = missionResult.Value.AssignedEmployees
        .Any(m => 
            (m.MissionRole == MissionRole.Leader || m.MissionRole == MissionRole.CoLeader) &&
            m.EmployeeId == employeeIdResult.Value);

        if (!isRequesterValid)
        {
            _logger.LogWarning("Permission denied: requester is not Leader or CoLeader");
            return Result.Fail(ApplicationError.Forbidden("The employee is not a leader nor a co-leader"));
        }

        // Check if the added member exists
        var memberIdResult = EmployeeId.FromString(memberId);
        if (memberIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid target member ID format: {ErrorMessage}", 
                memberIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid added member id"));
        }

        var memberResult = await _employeeRepository.GetEmployeeByIdAsync(memberIdResult.Value);
        if (memberResult.IsFailed)
        {
            _logger.LogError("Failed to load target employee from repository: {ErrorMessage}", memberResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        if (memberResult.Value is null)
        {
            _logger.LogInformation("Target employee not found");
            return Result.Fail(ApplicationError.NotFound("The added member is not found"));
        }

        // Check if the added member is already part of the mission
        if (missionResult.Value.AssignedEmployees.Any(m => m.EmployeeId == memberIdResult.Value))
        {
            _logger.LogInformation("Member is already assigned to the mission");
            return Result.Fail(ApplicationError.Duplicated("This member is already in the project"));
        }

        var addResult = missionResult.Value.AddMember(memberIdResult.Value, MissionRole.Member);
        if (addResult.IsFailed)
        {
            _logger.LogWarning("Business rule violation when adding member: {ErrorMessage}", 
                addResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation(addResult.Errors[0].Message));    
        }

        var updateResult = _missionRepository.UpdateMission(missionResult.Value);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission in repository: {ErrorMessage}", updateResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);    
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Member successfully added to mission");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed while adding member to mission");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result> DeleteMemberFromMission(string employeeId, string missionId, string memberId)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"]    = missionId,
            ["RequestedBy"]  = employeeId, 
            ["TargetMember"] = memberId  
        });

        _logger.LogInformation("Delete member from mission operation started");
        if (employeeId == memberId)
        {
            _logger.LogWarning("Attempted self-removal from mission - forbidden");
            return Result.Fail(ApplicationError.Validation("You cannot delete yourself"));    
        }

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}",
                missionIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid requester employee ID format: {ErrorMessage}",
                employeeIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid employee Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to retrieve mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            _logger.LogInformation("Mission not found");
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        // Check if the requester is valid
        bool isRequesterValid = missionResult.Value.AssignedEmployees
        .Any(m =>
            (m.MissionRole == MissionRole.Leader || m.MissionRole ==  MissionRole.CoLeader) &&
            m.EmployeeId == employeeIdResult.Value);

        if (!isRequesterValid)
        {
            _logger.LogWarning("Permission denied: requester is not Leader or CoLeader");
            return Result.Fail(ApplicationError.Forbidden("The employee is not a leader nor a co-leader"));
        }

        var memberIdResult = EmployeeId.FromString(memberId);
        if (memberIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid target member ID format: {ErrorMessage}",
                memberIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid member Id"));
        }

        var deleteResult = missionResult.Value.DeleteMember(memberIdResult.Value);
        if (deleteResult.IsFailed)
        {   
            var errorMsg = deleteResult.Errors[0].Message;
            _logger.LogWarning("Cannot delete member: {ErrorMessage}", errorMsg);
            return Result.Fail(ApplicationError.Validation(errorMsg));    
        }

        var updateResult = _missionRepository.UpdateMission(missionResult.Value);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission in repository: {ErrorMessage}", updateResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Member successfully removed from mission");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed while removing member from mission");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result> ChangeMemberRole(string employeeId, string missionId, string memberId, string missionRole)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"]    = missionId,
            ["RequestedBy"]  = employeeId,  
            ["TargetMember"] = memberId,    
            ["TargetRole"]   = missionRole  
        });

        _logger.LogInformation("Change member role operation started");

        if (employeeId == memberId)
        {
            _logger.LogWarning("Attempt to change own role - forbidden");
            return Result.Fail(ApplicationError.Validation("You cannot change your own role yourself"));    
        }

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}",
                missionIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var employeeIdResult = EmployeeId.FromString(employeeId);
        if (employeeIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid requester employee ID format: {ErrorMessage}",
                employeeIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid employee Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to load mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            _logger.LogInformation("Mission not found");
            return Result.Fail(ApplicationError.NotFound("Mission is not found"));
        }

        // Check if the requester is valid
        bool isRequesterLeader = missionResult.Value.AssignedEmployees
            .Any(m => m.MissionRole == MissionRole.Leader &&
                    m.EmployeeId == employeeIdResult.Value);

        if (!isRequesterLeader)
        {
            return Result.Fail(ApplicationError.Forbidden("The employee is not a leader"));
        }

        var memberIdResult = EmployeeId.FromString(memberId);
        if (memberIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid target member ID format: {ErrorMessage}",
                memberIdResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid member Id"));
        }

        var targetRoleResult = MissionRole.FromString(missionRole);
        if (targetRoleResult.IsFailed)
        {
            _logger.LogWarning("Invalid target mission role: {ErrorMessage}",
                targetRoleResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Validation("Invalid mission role"));
        }

        var changeResult = missionResult.Value.ChangeMemberRole(memberIdResult.Value, targetRoleResult.Value);
        if (changeResult.IsFailed)
        {
            var errorMsg = changeResult.Errors[0].Message;
            _logger.LogWarning("Business rule violation when changing role: {ErrorMessage}",
                errorMsg);
            return Result.Fail(ApplicationError.Validation(errorMsg));
        }

        var updateResult = _missionRepository.UpdateMission(missionResult.Value);
        if (updateResult.IsFailed)
        {
            _logger.LogError("Failed to update mission in repository: {ErrorMessage}", updateResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

       try
        {
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Member role successfully changed to {NewRole}",
                targetRoleResult.Value.ToString());
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database commit failed while changing member role");
            return Result.Fail(ApplicationError.Internal);
        }
    }

    public async Task<Result<IEnumerable<AssignedEmployeeDto>>> GetMissionMembers(string missionId)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MissionId"] = missionId
        });

        _logger.LogInformation("Get mission members request started");

        var missionIdResult = MissionId.FromString(missionId);
        if (missionIdResult.IsFailed)
        {
            _logger.LogWarning("Invalid mission ID format: {ErrorMessage}",
                missionIdResult.Errors[0].Message ?? "Unknown error");
            return Result.Fail(ApplicationError.Validation("Invalid mission Id"));
        }

        var missionResult = await _missionRepository.GetMissionByIdAsync(missionIdResult.Value);
        if (missionResult.IsFailed)
        {
            _logger.LogError("Failed to load mission from repository: {ErrorMessage}", missionResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }
        if (missionResult.Value is null)
        {
            _logger.LogInformation("Mission not found");
            return Result.Fail(ApplicationError.NotFound("The mission does not exist"));
        }

        Dictionary<EmployeeId, MissionRole> employeeRoleDict = [];
        foreach (var assignedEmployee in missionResult.Value.AssignedEmployees)
        {
            employeeRoleDict.Add(assignedEmployee.EmployeeId, assignedEmployee.MissionRole);
        }

        var employeesResult = await _employeeRepository.GetEmployeesByIdsAsync(missionResult.Value.AssignedEmployees.Select(emp => emp.EmployeeId));
        if (employeesResult.IsFailed)
        {
            _logger.LogError("Failed to load employees by IDs: {ErrorMessage}", employeesResult.Errors[0].Message);
            return Result.Fail(ApplicationError.Internal);
        }

        List<AssignedEmployeeDto> assignedEmployees = [];
        foreach (var employee in employeesResult.Value)
        {
            var missionRole = employeeRoleDict[employee.Id];
            assignedEmployees.Add(EmployeeToAssignedEmployeeDto(employee, missionRole));
        }

        _logger.LogInformation("Successfully retrieved {MemberCount} mission members",
            assignedEmployees.Count);

        return assignedEmployees;
    }

    private static AssignedEmployeeDto EmployeeToAssignedEmployeeDto(Employee employee, MissionRole missionRole)
    {
        return new AssignedEmployeeDto(employee.Id.ToString(),
                                        employee.FirstName,
                                        employee.LastName,
                                        employee.ImgUrl,
                                        missionRole.ToString());
    }

    private static MissionDto MapToMissionDto(MissionBase mission)
    {
        return new MissionDto(
                        mission.Id.ToString(),
                        mission.Name,
                        mission.Description,
                        mission.Category.ToString(),
                        mission.Status.ToString(),
                        mission.FinishedAt,
                        mission.ResourceLink,
                        mission.CreatedAt,
                        mission.UpdatedAt
                    );
    }
    
    private static Result<MissionDetailDto> MapToMissionDetailDto(MissionBase mission, Employee leader, RoutePlanningSummaryDto? routePlanningSummary = null)
    {
        return new MissionDetailDto(mission.Id.ToString(),
                                    mission.Name,
                                    mission.Description,
                                    mission.Category.ToString(),
                                    mission.Status.ToString(),
                                    new(leader.Id.ToString(),
                                        leader.FirstName,
                                        leader.LastName,
                                        leader.ImgUrl,
                                        MissionRole.Leader.ToString()),
                                    mission.FinishedAt,
                                    mission.ResourceLink,
                                    mission.CreatedAt,
                                    mission.UpdatedAt,
                                    mission.AssignedEmployees.Count,
                                    routePlanningSummary);
    }
}
