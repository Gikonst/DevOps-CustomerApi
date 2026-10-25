param(
    [string]$Tag = "latest"
)

$image = "customer-api:$Tag"

docker build -f .\cicd\Dockerfile -t $image .
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

minikube image load $image
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }