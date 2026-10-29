param(
    [string]$Tag = "latest"
)

$images = @(
    @{ Name = "customer-api:$Tag";            Target = "final" },
    @{ Name = "customer-api-migrations:$Tag"; Target = "migrations" }
)

foreach ($i in $images) {
    docker build -f .\infrastructure\Dockerfile --target $i.Target -t $i.Name .
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    minikube image load $i.Name
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}