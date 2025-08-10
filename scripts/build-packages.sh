#!/bin/bash

# BAML .NET Package Build Script
# This script builds and packs the BAML .NET packages locally for testing

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the right directory
if [[ ! -f "Baml.NET.sln" ]]; then
    print_error "This script must be run from the repository root directory"
    exit 1
fi

# Configuration
CONFIGURATION="Release"
OUTPUT_DIR="./packages"
BUILD_NUMBER="${GITHUB_RUN_NUMBER:-0}"

print_status "Starting BAML .NET package build..."
print_status "Configuration: $CONFIGURATION"
print_status "Output Directory: $OUTPUT_DIR"
print_status "Build Number: $BUILD_NUMBER"

# Clean previous build
print_status "Cleaning previous build..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Restore dependencies
print_status "Restoring dependencies..."
dotnet restore

# Build solution
print_status "Building solution..."
dotnet build --configuration "$CONFIGURATION" --no-restore

# Run tests
print_status "Running tests..."
dotnet test --configuration "$CONFIGURATION" --no-build --verbosity normal

# Pack packages
print_status "Packing Baml.Runtime..."
dotnet pack src/Baml.Runtime/Baml.Runtime.csproj \
    --configuration "$CONFIGURATION" \
    --no-build \
    --output "$OUTPUT_DIR" \
    -p:PackageVersion="1.0.0-local.$(date +%s)" || {
    print_error "Failed to pack Baml.Runtime"
    exit 1
}

print_status "Packing Baml.SourceGenerator..."
dotnet pack src/Baml.SourceGenerator/Baml.SourceGenerator.csproj \
    --configuration "$CONFIGURATION" \
    --no-build \
    --output "$OUTPUT_DIR" \
    -p:PackageVersion="1.0.0-local.$(date +%s)" || {
    print_warning "Source generator packaging failed, but package may have been created successfully"
    # Check if package was actually created
    if find "$OUTPUT_DIR" -name "Baml.SourceGenerator*.nupkg" -type f | grep -q .; then
        print_success "Source generator package was created successfully despite warnings"
    else
        print_error "Failed to pack Baml.SourceGenerator and no package was created"
        exit 1
    fi
}

# Display results
print_success "Build completed successfully!"
echo ""
print_status "Generated packages:"
for pkg in "$OUTPUT_DIR"/*.nupkg; do
    if [[ -f "$pkg" ]]; then
        size=$(du -h "$pkg" | cut -f1)
        echo "  📦 $(basename "$pkg") ($size)"
    fi
done

echo ""
print_status "Package details:"
for pkg in "$OUTPUT_DIR"/*.nupkg; do
    if [[ -f "$pkg" ]]; then
        echo ""
        echo "📋 $(basename "$pkg"):"
        unzip -l "$pkg" | head -15 | tail -n +4
        echo "    ..."
    fi
done

echo ""
print_success "All packages built successfully in $OUTPUT_DIR"
print_warning "These are local test packages - not suitable for distribution"

# Optional: Test package installation
read -p "Do you want to test package installation? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    print_status "Testing package installation..."
    
    # Create a temporary test project
    TEST_DIR="./test-package-install"
    rm -rf "$TEST_DIR"
    mkdir -p "$TEST_DIR"
    
    cd "$TEST_DIR"
    dotnet new console -n TestInstall
    cd TestInstall
    
    # Add local package source
    dotnet nuget add source "$(realpath "../../$OUTPUT_DIR")" --name "local-packages"
    
    # Try to install the packages
    for pkg in ../../"$OUTPUT_DIR"/*.nupkg; do
        if [[ -f "$pkg" ]]; then
            pkg_name=$(basename "$pkg" | sed 's/\.[0-9].*\.nupkg$//')
            pkg_version=$(basename "$pkg" | sed 's/.*\.\([0-9].*\)\.nupkg$/\1/')
            print_status "Testing installation of $pkg_name version $pkg_version..."
            dotnet add package "$pkg_name" --version "$pkg_version" --source "local-packages"
        fi
    done
    
    # Try to build the test project
    dotnet build
    
    # Cleanup
    cd ../../
    rm -rf "$TEST_DIR"
    
    print_success "Package installation test completed!"
fi

print_success "Script completed successfully! 🎉"
