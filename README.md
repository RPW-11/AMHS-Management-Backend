# AMHS-Management-Backend

### How to run the project
1. After cloning the project, run the following:
``` bash
dotnet restore
dotnet build
```

2. Setup the user secrets and add necessary information
``` bash
dotnet user-secrets init --project src/API
dotnet user-secrets set --project src/API "JwtSettings:Secret" "YOUR_SECRET"
dotnet user-secrets set --project src/API "ConnectionStrings:PostgresConnectionString" "Host=HOSTNAME;Database=DBNAME;Username=USERNAME;Password=PASSWORD;SslMode=Require"
```

3. Run your first migration (make sure to install dotnet ef 9.0.7)
``` bash
dotnet ef migrations list --project src/Infrastructure --startup-project src/API
```
```

AMHS-Management-Backend
├─ .DS_Store
├─ .config
│  └─ dotnet-tools.json
├─ AMHSDashboardBackEnd.sln
├─ LICENSE
├─ README.md
├─ src
│  ├─ .DS_Store
│  ├─ API
│  │  ├─ API.csproj
│  │  ├─ API.http
│  │  ├─ Contracts
│  │  │  ├─ Authentication
│  │  │  │  └─ LoginRequest.cs
│  │  │  ├─ Employee
│  │  │  │  └─ AddEmployeeRequest.cs
│  │  │  └─ Mission
│  │  │     ├─ AddMissionRequest.cs
│  │  │     ├─ ChangeMemberRoleRequest.cs
│  │  │     ├─ CreateRoutePlanningRequest.cs
│  │  │     ├─ DeleteMissionsRequest.cs
│  │  │     └─ UpdateMissionRequest.cs
│  │  ├─ Controllers
│  │  │  ├─ ApiBaseController.cs
│  │  │  ├─ AuthenticationController.cs
│  │  │  ├─ EmployeeController.cs
│  │  │  ├─ MissionController.cs
│  │  │  └─ NotificationController.cs
│  │  ├─ Program.cs
│  │  ├─ Properties
│  │  │  └─ launchSettings.json
│  │  ├─ appsettings.Development.json
│  │  └─ appsettings.json
│  ├─ Application
│  │  ├─ Application.csproj
│  │  ├─ Common
│  │  │  ├─ Errors
│  │  │  │  └─ ApplicationError.cs
│  │  │  ├─ Interfaces
│  │  │  │  ├─ Authentication
│  │  │  │  │  └─ IJwtTokenGenerator.cs
│  │  │  │  ├─ IDomainDispatcher.cs
│  │  │  │  ├─ IUnitOfWork.cs
│  │  │  │  ├─ Persistence
│  │  │  │  │  ├─ IEmployeeRepository.cs
│  │  │  │  │  ├─ IMissionRepository.cs
│  │  │  │  │  └─ INotificationRepository.cs
│  │  │  │  ├─ RoutePlanning
│  │  │  │  │  └─ IRgvRoutePlanning.cs
│  │  │  │  ├─ Security
│  │  │  │  │  └─ IPasswordHasher.cs
│  │  │  │  └─ Services
│  │  │  │     └─ IDateTimeProvider.cs
│  │  │  └─ Utilities
│  │  │     └─ RouteIntersection.cs
│  │  ├─ DTOs
│  │  │  ├─ Authentication
│  │  │  │  └─ AuthenticationDto.cs
│  │  │  ├─ Common
│  │  │  │  └─ PagedResult.cs
│  │  │  ├─ Employee
│  │  │  │  ├─ EmployeeDto.cs
│  │  │  │  └─ EmployeeSearchDto.cs
│  │  │  ├─ Mission
│  │  │  │  ├─ AddMissionDto.cs
│  │  │  │  ├─ AssignedEmployeeDto.cs
│  │  │  │  ├─ MissionDetailDto.cs
│  │  │  │  ├─ MissionDto.cs
│  │  │  │  ├─ MissionFilterDto.cs
│  │  │  │  └─ UpdateMissionDto.cs
│  │  │  ├─ Notification
│  │  │  │  └─ NotificationDto.cs
│  │  │  └─ RoutePlanning
│  │  │     └─ RgvRoutePlanningDto.cs
│  │  ├─ DependencyInjection.cs
│  │  └─ Services
│  │     ├─ AuthenticationService
│  │     │  ├─ AuthenticationService.cs
│  │     │  └─ IAuthenticationService.cs
│  │     ├─ BaseService.cs
│  │     ├─ EmployeeService
│  │     │  ├─ EmployeeService.cs
│  │     │  └─ IEmployeeService.cs
│  │     ├─ MissionService
│  │     │  ├─ IMissionService.cs
│  │     │  └─ MissionService.cs
│  │     ├─ NotificationService
│  │     │  ├─ INotificationService.cs
│  │     │  └─ NotificationService.cs
│  │     └─ RoutePlanningService
│  │        ├─ IRoutePlanningService.cs
│  │        └─ RoutePlanningService.cs
│  ├─ Domain
│  │  ├─ Common
│  │  │  └─ Models
│  │  │     ├─ AggregateRoot.cs
│  │  │     ├─ Entity.cs
│  │  │     └─ ValueObject.cs
│  │  ├─ Domain.csproj
│  │  ├─ Employees
│  │  │  ├─ Employee.cs
│  │  │  └─ ValueObjects
│  │  │     ├─ EmployeeId.cs
│  │  │     ├─ EmployeePosition.cs
│  │  │     └─ EmployeeStatus.cs
│  │  ├─ Errors
│  │  │  ├─ DomainError.cs
│  │  │  ├─ EmployeeAttendance
│  │  │  │  ├─ AlreadyCheckedInError.cs
│  │  │  │  ├─ AlreadyCheckedOutError.cs
│  │  │  │  ├─ MustCheckInFirstError.cs
│  │  │  │  ├─ NoActiveBreakException.cs
│  │  │  │  └─ NotWithinBreakCheckOutError.cs
│  │  │  ├─ Employees
│  │  │  │  ├─ EmptyEmployeeNameError.cs
│  │  │  │  ├─ InvalidEmployeeDateOfBirthError.cs
│  │  │  │  ├─ InvalidEmployeeEmailFormatError.cs
│  │  │  │  ├─ InvalidEmployeeIdError.cs
│  │  │  │  ├─ InvalidEmployeePhoneNumberError.cs
│  │  │  │  ├─ InvalidEmployeePositionError.cs
│  │  │  │  └─ InvalidEmployeeStatusError.cs
│  │  │  ├─ Missions
│  │  │  │  ├─ EmptyMissionNameError.cs
│  │  │  │  ├─ EmptyMissionRoleError.cs
│  │  │  │  ├─ InvalidMemberSwitchError.cs
│  │  │  │  ├─ InvalidMissionCategoryError.cs
│  │  │  │  ├─ InvalidMissionIdError.cs
│  │  │  │  ├─ InvalidMissionRoleError.cs
│  │  │  │  ├─ InvalidMissionStatusError.cs
│  │  │  │  ├─ MaximumMissionRoleError.cs
│  │  │  │  ├─ NotFoundMemberError.cs
│  │  │  │  └─ RoutePlanning
│  │  │  │     ├─ InvalidAlgorithmError.cs
│  │  │  │     ├─ InvalidColPosValueError.cs
│  │  │  │     ├─ InvalidNumberOfStationsOrderError.cs
│  │  │  │     ├─ InvalidRgvMapActualDimensionError.cs
│  │  │  │     ├─ InvalidRgvMapDimensionError.cs
│  │  │  │     └─ InvalidRowPosValueError.cs
│  │  │  └─ Notifications
│  │  │     ├─ InvalidNotificationIdError.cs
│  │  │     └─ InvalidNotificationTypeError.cs
│  │  ├─ Interfaces
│  │  │  └─ IDomainEvent.cs
│  │  ├─ Missions
│  │  │  ├─ Entities
│  │  │  │  └─ AssignedEmployee.cs
│  │  │  ├─ Events
│  │  │  │  └─ MissionFinishedEvent.cs
│  │  │  ├─ MissionBase.cs
│  │  │  ├─ MissionFactory.cs
│  │  │  ├─ RoutePlanningMission.cs
│  │  │  └─ ValueObjects
│  │  │     ├─ AssignedEmployeeId.cs
│  │  │     ├─ MissionCategory.cs
│  │  │     ├─ MissionId.cs
│  │  │     ├─ MissionRole.cs
│  │  │     ├─ MissionStatus.cs
│  │  │     ├─ PathPoint.cs
│  │  │     ├─ RgvMap.cs
│  │  │     └─ RoutePlanningAlgorithm.cs
│  │  └─ Notifications
│  │     ├─ Notification.cs
│  │     └─ ValueObjects
│  │        ├─ NotificationId.cs
│  │        ├─ NotificationTarget.cs
│  │        └─ NotificationType.cs
│  └─ Infrastructure
│     ├─ Authentication
│     │  ├─ JwtSettings.cs
│     │  └─ JwtTokenGenerator.cs
│     ├─ DependencyInjection.cs
│     ├─ Infrastructure.csproj
│     ├─ Migrations
│     │  ├─ 20250722031529_InitialCreate.Designer.cs
│     │  ├─ 20250722031529_InitialCreate.cs
│     │  ├─ 20250722061005_DateOfBirthColType.Designer.cs
│     │  ├─ 20250722061005_DateOfBirthColType.cs
│     │  ├─ 20250724005456_AddPhoneNumberAttributeEmployee.Designer.cs
│     │  ├─ 20250724005456_AddPhoneNumberAttributeEmployee.cs
│     │  ├─ 20250725015827_EmployeePosition.Designer.cs
│     │  ├─ 20250725015827_EmployeePosition.cs
│     │  ├─ 20250725025147_EmployeeStatus.Designer.cs
│     │  ├─ 20250725025147_EmployeeStatus.cs
│     │  ├─ 20250730030006_AddMissionEntity.Designer.cs
│     │  ├─ 20250730030006_AddMissionEntity.cs
│     │  ├─ 20250730055455_MissionTableName.Designer.cs
│     │  ├─ 20250730055455_MissionTableName.cs
│     │  ├─ 20250808001156_AssignedEmployeeTable.Designer.cs
│     │  ├─ 20250808001156_AssignedEmployeeTable.cs
│     │  ├─ 20260203015633_NotificationTable.Designer.cs
│     │  ├─ 20260203015633_NotificationTable.cs
│     │  └─ AppDbContextModelSnapshot.cs
│     ├─ Persistence
│     │  ├─ AppDbContext.cs
│     │  ├─ Config
│     │  │  ├─ AssignedEmployeeConfiguration.cs
│     │  │  ├─ DataSchemaConstants.cs
│     │  │  ├─ EmployeeConfiguration.cs
│     │  │  ├─ MissionConfiguration.cs
│     │  │  └─ NotificationConfiguration.cs
│     │  ├─ Repositories
│     │  │  ├─ EmployeeRepository.cs
│     │  │  ├─ MissionRepository.cs
│     │  │  └─ NotificationRepository.cs
│     │  └─ UnitOfWork.cs
│     ├─ RoutePlanning
│     │  ├─ Rgv
│     │  │  ├─ DfsSolver.cs
│     │  │  ├─ GeneticAlgorithmSolver.cs
│     │  │  ├─ ModifiedAStar.cs
│     │  │  ├─ PostProcessingRoute.cs
│     │  │  ├─ RandomTreeStar.cs
│     │  │  ├─ RgvRoutePlanning.cs
│     │  │  ├─ RouteDrawer.cs
│     │  │  └─ RouteEvaluator.cs
│     │  └─ RoutePlanningSettings.cs
│     ├─ Security
│     │  └─ PasswordHasher.cs
│     └─ Services
│        └─ DateTimeProvider.cs
└─ tests
   └─ DomainTests
      └─ Tests.csproj

```