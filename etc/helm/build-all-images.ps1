./build-image.ps1 -ProjectPath "../../src/MOD.Training.DbMigrator/MOD.Training.DbMigrator.csproj" -ImageName training/dbmigrator
./build-image.ps1 -ProjectPath "../../src/MOD.Training.HttpApi.Host/MOD.Training.HttpApi.Host.csproj" -ImageName training/httpapihost
./build-image.ps1 -ProjectPath "../../angular" -ImageName training/angular -ProjectType "angular"
