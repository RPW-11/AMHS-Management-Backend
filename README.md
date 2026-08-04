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

- The S3 route planning result store (`S3RoutePlanningResultStore`) is the one registered in `Infrastructure/DependencyInjection.cs`, so its credentials are required. Set them via user secrets rather than `appsettings.json`:

``` bash
dotnet user-secrets set --project src/API "RoutePlanningSettings:S3:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set --project src/API "RoutePlanningSettings:S3:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set --project src/API "RoutePlanningSettings:S3:BucketName" "YOUR_BUCKET_NAME"
dotnet user-secrets set --project src/API "RoutePlanningSettings:S3:EndPointUrl" "YOUR_S3_ENDPOINT_URL"
```

- Go to `appsettings.json`, and fill in the route planning result store settings. The `S3` section only needs the empty placeholder keys here (the actual values come from user secrets above) so configuration binding picks them up. S3 is the only result store, so these credentials are required to run route planning at all.

``` json
...
"RoutePlanningSettings": {
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
