param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

dotnet ef migrations add $Name `
    --project .\src\CustomerApi.Infrastructure `
    --startup-project .\src\CustomerApi