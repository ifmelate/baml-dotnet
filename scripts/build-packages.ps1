# BAML .NET Package Build Script (PowerShell)
# This script builds and packs the BAML .NET packages locally for testing

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./packages",
    [switch]$SkipTests,
    [switch]$TestInstall
)

# Error handling
$ErrorActionPreference = "Stop"

# Function to print colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Check if we're in the right directory
if (-not (Test-Path "Baml.NET.sln")) {
    Write-Error "This script must be run from the repository root directory"
    exit 1
}

$BuildNumber = if ($env:GITHUB_RUN_NUMBER) { $env:GITHUB_RUN_NUMBER } else { "0" }

Write-Status "Starting BAML .NET package build..."
Write-Status "Configuration: $Configuration"
Write-Status "Output Directory: $OutputDir"
Write-Status "Build Number: $BuildNumber"

# Clean previous build
Write-Status "Cleaning previous build..."
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

try {
    # Restore dependencies
    Write-Status "Restoring dependencies..."
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

    # Build solution
    Write-Status "Building solution..."
    dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }

    # Run tests (unless skipped)
    if (-not $SkipTests) {
        Write-Status "Running tests..."
        dotnet test --configuration $Configuration --no-build --verbosity normal
        if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
    } else {
        Write-Warning "Skipping tests as requested"
    }

    # Pack packages
    $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $localVersion = "1.0.0-local.$timestamp"

    Write-Status "Packing Baml.Runtime..."
    dotnet pack src/Baml.Runtime/Baml.Runtime.csproj `
        --configuration $Configuration `
        --no-build `
        --output $OutputDir `
        -p:PackageVersion=$localVersion
    if ($LASTEXITCODE -ne 0) { throw "Packing Baml.Runtime failed" }

    Write-Status "Packing Baml.SourceGenerator..."
    dotnet pack src/Baml.SourceGenerator/Baml.SourceGenerator.csproj `
        --configuration $Configuration `
        --no-build `
        --output $OutputDir `
        -p:PackageVersion=$localVersion
    if ($LASTEXITCODE -ne 0) { throw "Packing Baml.SourceGenerator failed" }

    # Display results
    Write-Success "Build completed successfully!"
    Write-Host ""
    Write-Status "Generated packages:"
    
    Get-ChildItem -Path $OutputDir -Filter "*.nupkg" | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 1)
        Write-Host "  📦 $($_.Name) ($size KB)"
    }

    Write-Host ""
    Write-Status "Package details:"
    Get-ChildItem -Path $OutputDir -Filter "*.nupkg" | ForEach-Object {
        Write-Host ""
        Write-Host "📋 $($_.Name):"
        
        # Use PowerShell's built-in ZIP handling
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::OpenRead($_.FullName)
        $zip.Entries | Select-Object -First 15 | ForEach-Object {
            Write-Host "    $($_.FullName)"
        }
        if ($zip.Entries.Count -gt 15) {
            Write-Host "    ..."
        }
        $zip.Dispose()
    }

    Write-Host ""
    Write-Success "All packages built successfully in $OutputDir"
    Write-Warning "These are local test packages - not suitable for distribution"

    # Optional: Test package installation
    if ($TestInstall) {
        Write-Status "Testing package installation..."
        
        # Create a temporary test project
        $TestDir = "./test-package-install"
        if (Test-Path $TestDir) {
            Remove-Item -Recurse -Force $TestDir
        }
        New-Item -ItemType Directory -Path $TestDir -Force | Out-Null
        
        Push-Location $TestDir
        try {
            dotnet new console -n TestInstall
            Push-Location TestInstall
            try {
                # Add local package source
                $fullOutputPath = Resolve-Path "../../$OutputDir"
                dotnet nuget add source $fullOutputPath --name "local-packages"
                
                # Try to install the packages
                Get-ChildItem -Path "../../$OutputDir" -Filter "*.nupkg" | ForEach-Object {
                    $pkgName = $_.BaseName -replace '\.[0-9].*$', ''
                    $pkgVersion = ($_.BaseName -split '\.')[-3..-1] -join '.'
                    Write-Status "Testing installation of $pkgName version $pkgVersion..."
                    dotnet add package $pkgName --version $pkgVersion --source "local-packages"
                }
                
                # Try to build the test project
                dotnet build
                Write-Success "Package installation test completed!"
            }
            finally {
                Pop-Location
            }
        }
        finally {
            Pop-Location
            Remove-Item -Recurse -Force $TestDir -ErrorAction SilentlyContinue
        }
    }

    Write-Success "Script completed successfully! 🎉"
}
catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    exit 1
}
