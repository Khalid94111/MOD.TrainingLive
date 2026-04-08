$currentFolder = $PSScriptRoot

$certsFolder = Join-Path $currentFolder "certs"

If(!(Test-Path -Path $certsFolder))
{
    New-Item -ItemType Directory -Force -Path $certsFolder
    if(!(Test-Path -Path (Join-Path $certsFolder "localhost.pfx") -PathType Leaf)){
        Set-Location $certsFolder
        dotnet dev-certs https -v -ep localhost.pfx -p 20fc70d9-1865-4a9a-b15f-3b5395acce47 -t        
    }
}

Set-Location $currentFolder
docker-compose up -d
exit $LASTEXITCODE