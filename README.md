# AMHS-Management-Backend

## Description

A back-end server to find the optimal path for RGV (Rail-guided Vehicle) given a factory layout.

## How to run the project (Locally)

### Setup (dotnet version 9.0.301)

- Setup the user secrets and add necessary information. Make sure the JWT secret is 256 bits.

``` bash
dotnet user-secrets init --project src/API
dotnet user-secrets set --project src/API "JwtSettings:Secret" "YOUR_SECRET"
dotnet user-secrets set --project src/API "ConnectionStrings:PostgresConnectionString" "Host=HOSTNAME;Database=DBNAME;Username=USERNAME;Password=PASSWORD;SslMode=Require; ChannelBinding=Require"
```

- Go to `appsettings.json`, and fill the directory in which the result will be stored. Make sure to create the directory first.
  
``` json
...
"RoutePlanningSettings": {
    "LocalDirectory": "DIRECTORY_WHERE_THE_RESULT_WILL_BE_STORED"
}
...
```

- Run your first migration if you are using a fresh database (make sure to install dotnet ef 9.0.7)

``` bash
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

- Run the following to make sure everything works:

``` bash
dotnet restore
dotnet build
```

### Usage

- To start the server, run the following command

``` bash
dotnet run --project src/API
