# Building and Publishing Nazeh.RateLimiting

This guide explains how to build, test, and publish the Nazeh.RateLimiting NuGet package.

## Prerequisites

- .NET SDK 8.0, 9.0, or 10.0
- NuGet account (for publishing)

## Project Structure

```
RateLimiting/
├── Nazeh.RateLimiting/          # Main library project
│   ├── RateLimitingConfiguration.cs
│   ├── RateLimitingExtensions.cs
│   └── Nazeh.RateLimiting.csproj
├── Nazeh.RateLimiting/SampleApp/ # Sample application
│   ├── Program.cs
│   ├── appsettings.json
│   └── SampleApp.csproj
├── README.md
├── EXAMPLES.md
├── LICENSE
└── BUILD.md (this file)
```

## Building the Library

### Debug Build

```bash
cd Nazeh.RateLimiting
dotnet build
```

This will:
- Build for .NET 8.0, 9.0, and 10.0
- Create a NuGet package in `bin/Debug/Nazeh.RateLimiting.1.0.0.nupkg`

### Release Build

```bash
cd Nazeh.RateLimiting
dotnet build -c Release
```

The release package will be created in `bin/Release/Nazeh.RateLimiting.1.0.0.nupkg`

## Testing the Library

### Option 1: Using the Sample Application

```bash
# Build and run the sample app
cd Nazeh.RateLimiting/SampleApp
dotnet run
```

The sample app will start on `http://localhost:5000` (or `https://localhost:5001`).

Test the endpoints:

```bash
# Test global limit
curl http://localhost:5000/

# Test API limit
curl http://localhost:5000/api/products

# Test authentication limit
curl -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}'

# Test IP-based sliding window
curl http://localhost:5000/public/feed
```

### Option 2: Create a Test Project

```bash
# Create a new test project
dotnet new web -n TestApp
cd TestApp

# Add reference to the library
dotnet add reference ../Nazeh.RateLimiting/Nazeh.RateLimiting.csproj

# Run the test app
dotnet run
```

### Option 3: Install from Local NuGet Package

```bash
# Pack the library
cd Nazeh.RateLimiting
dotnet pack -c Release

# Create a new test project
cd ..
dotnet new web -n TestApp
cd TestApp

# Add the local package
dotnet add package Nazeh.RateLimiting --source ../Nazeh.RateLimiting/bin/Release
```

## Publishing to NuGet.org

### Step 1: Update Version

Edit `Nazeh.RateLimiting.csproj` and update the version:

```xml
<Version>1.0.1</Version>
```

### Step 2: Create Release Build

```bash
cd Nazeh.RateLimiting
dotnet build -c Release
```

### Step 3: Get NuGet API Key

1. Go to [NuGet.org](https://www.nuget.org/)
2. Sign in to your account
3. Go to Account Settings → API Keys
4. Create a new API key with "Push" permission

### Step 4: Publish

```bash
dotnet nuget push bin/Release/Nazeh.RateLimiting.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Step 5: Verify

After publishing (it may take a few minutes to index):

```bash
dotnet add package Nazeh.RateLimiting
```

## Publishing to a Private NuGet Feed

### Azure Artifacts

```bash
# Add the feed
dotnet nuget add source "https://pkgs.dev.azure.com/{organization}/_packaging/{feed}/nuget/v3/index.json" \
  --name AzureArtifacts \
  --username {username} \
  --password {PAT}

# Push the package
dotnet nuget push bin/Release/Nazeh.RateLimiting.1.0.0.nupkg \
  --source AzureArtifacts
```

### GitHub Packages

```bash
# Add the feed
dotnet nuget add source "https://nuget.pkg.github.com/{owner}/index.json" \
  --name GitHubPackages \
  --username {username} \
  --password {PAT}

# Push the package
dotnet nuget push bin/Release/Nazeh.RateLimiting.1.0.0.nupkg \
  --source GitHubPackages
```

### Local NuGet Feed

```bash
# Create a local feed directory
mkdir C:\LocalNuGet

# Push to local feed
dotnet nuget push bin/Release/Nazeh.RateLimiting.1.0.0.nupkg \
  --source C:\LocalNuGet

# Add the local feed
dotnet nuget add source C:\LocalNuGet --name LocalFeed
```

## Versioning

This package follows [Semantic Versioning](https://semver.org/):

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Add functionality in a backward-compatible manner
- **PATCH** version: Backward-compatible bug fixes

Example versions:
- `1.0.0` - Initial release
- `1.0.1` - Bug fix
- `1.1.0` - New feature (backward-compatible)
- `2.0.0` - Breaking change

## Package Metadata

The package includes the following metadata (configured in `.csproj`):

```xml
<PackageId>Nazeh.RateLimiting</PackageId>
<Version>1.0.0</Version>
<Authors>Ahmed Nazeh</Authors>
<Title>Easy Rate Limiting for ASP.NET Core</Title>
<Description>A simple, configurable wrapper around ASP.NET Core Rate Limiting</Description>
<PackageTags>ratelimiting;aspnetcore;middleware;throttling;api</PackageTags>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageProjectUrl>https://github.com/AhmedNazeh/Nazeh.RateLimiting</PackageProjectUrl>
<RepositoryUrl>https://github.com/AhmedNazeh/Nazeh.RateLimiting</RepositoryUrl>
<PackageReadmeFile>README.md</PackageReadmeFile>
```

## Troubleshooting

### Build Errors

**Error: "Project file does not exist"**
- Ensure you're in the correct directory
- Use the full path: `dotnet build C:\Nuget\RateLimiting\Nazeh.RateLimiting\Nazeh.RateLimiting.csproj`

**Error: "The type or namespace name 'X' could not be found"**
- Run `dotnet restore` first
- Check that you're targeting the correct framework

### Publishing Errors

**Error: "409 (Conflict) - Package already exists"**
- You cannot republish the same version
- Increment the version number in `.csproj`

**Error: "403 (Forbidden)"**
- Check that your API key is valid
- Ensure the API key has "Push" permissions

### Runtime Errors

**Error: "Rate limiting is not working"**
- Ensure `app.UseRateLimiter()` is called in `Program.cs`
- Check that `RateLimiting.Enabled` is `true` in `appsettings.json`
- Verify the middleware order (UseRateLimiter should be early in the pipeline)

## CI/CD Integration

### GitHub Actions

Create `.github/workflows/publish.yml`:

```yaml
name: Publish NuGet Package

on:
  release:
    types: [created]

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore Nazeh.RateLimiting/Nazeh.RateLimiting.csproj
      
      - name: Build
        run: dotnet build Nazeh.RateLimiting/Nazeh.RateLimiting.csproj -c Release --no-restore
      
      - name: Pack
        run: dotnet pack Nazeh.RateLimiting/Nazeh.RateLimiting.csproj -c Release --no-build
      
      - name: Publish to NuGet
        run: dotnet nuget push Nazeh.RateLimiting/bin/Release/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

### Azure DevOps

Create `azure-pipelines.yml`:

```yaml
trigger:
  tags:
    include:
      - v*

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'
    projects: 'Nazeh.RateLimiting/Nazeh.RateLimiting.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    projects: 'Nazeh.RateLimiting/Nazeh.RateLimiting.csproj'
    arguments: '-c Release'

- task: DotNetCoreCLI@2
  displayName: 'Pack'
  inputs:
    command: 'pack'
    packagesToPack: 'Nazeh.RateLimiting/Nazeh.RateLimiting.csproj'
    configuration: 'Release'

- task: NuGetCommand@2
  displayName: 'Publish to NuGet'
  inputs:
    command: 'push'
    packagesToPush: '$(Build.ArtifactStagingDirectory)/**/*.nupkg'
    nuGetFeedType: 'external'
    publishFeedCredentials: 'NuGet'
```

## Support

For issues or questions:
- GitHub Issues: https://github.com/AhmedNazeh/Nazeh.RateLimiting/issues
- Email: your-email@example.com

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

