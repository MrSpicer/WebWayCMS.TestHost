# WebWayCMS.TestHost
integratino testing host for primary project

### Make Scripts Executable
```
chmod +x ./scripts/*
```

### Setup Local Database
```
./scripts/SetupLocalPostgresDBContainer.sh
```

This creates a `postgres:18-alpine` Docker container. It prompts for database name, user,
password, and host port, defaulting to the values already hardcoded in
`WebWayCMS.TestHost/appsettings.json` (`integration-host` on port `16196`).

### Pack the CMS packages
A host project consumes the CMS as the `WebWayCMS` NuGet package from a local feed. Pack the
libraries after cloning (and after changing CMS source) before building or running such a host:
```
./scripts/PackLocalPackages.sh
```

> The integration host (`WebWayCMS.TestHost`) uses **project references** rather than the
> package feed, so it does not need this step.

### Development - Hot Reload
```
./scripts/HotReloadRun.sh
```

Runs `dotnet watch run` against the integration host with `ASPNETCORE_ENVIRONMENT=Development`, so
C# and Razor changes are picked up without a manual rebuild.