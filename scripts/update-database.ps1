param(
    [string]$Migration
)

if ([string]::IsNullOrWhiteSpace($Migration))
{
    dotnet ef database update `
        --project .\src\CustomerApi.Infrastructure `
        --startup-project .\src\CustomerApi
}
else
{
    dotnet ef database update $Migration `
        --project .\src\CustomerApi.Infrastructure `
        --startup-project .\src\CustomerApi
}