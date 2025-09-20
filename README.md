# Ad Platforms Service (C# .NET 8)

Simple Minimal API that loads advertising platforms and their locations from a text file and returns platforms matching a requested location (with nested prefix logic).

## Endpoints
- `POST /api/ad-platforms/load` - body: `text/plain` with lines `Name:/loc1,/loc2`
- `GET  /api/ad-platforms?location=/ru/svrd/revda` - returns platforms for that location
- `GET  /health`
- Swagger UI: `/swagger`

## Run
```bash
dotnet run --project AdPlatforms/AdPlatforms.csproj
# then open http://localhost:XXXX/swagger
