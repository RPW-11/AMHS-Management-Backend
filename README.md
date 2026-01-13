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