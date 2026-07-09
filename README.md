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

- Go to `appsettings.json`, and fill in the route planning result store settings. `Local` is used if the store is not overridden to use S3; make sure to create the directory first. `S3` settings (bucket name, endpoint, credentials) are only required if the S3 store is registered in `DependencyInjection.cs`.

``` json
...
"RoutePlanningSettings": {
    "Local": {
        "LocalDirectory": "DIRECTORY_WHERE_THE_RESULT_WILL_BE_STORED"
    },
    "S3": {
        "SecretAccessKey": "",
        "AccessKeyId": "",
        "BucketName": "",
        "EndPointUrl": ""
    }
}
...
```

- Run your first migration if you are using a fresh database (make sure to install dotnet ef 9.0.7 or match your dotnet version)

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
```

- Swagger API docs (make sure run it first locally)

```plaintext
http://localhost:5171/swagger/index.html
```
