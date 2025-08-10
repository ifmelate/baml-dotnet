# NuGet Publishing Setup

This document explains how to set up and use the GitHub workflow for publishing BAML .NET packages to NuGet.org and GitHub Packages.

## Overview

The workflow automatically:
- Builds both `Baml.Runtime` and `Baml.SourceGenerator` packages
- Runs all tests to ensure quality
- Creates preview packages from `main` branch commits
- Creates release packages from git tags
- Publishes to NuGet.org and GitHub Packages

## Versioning Strategy

### Preview Versions (main branch)
- Format: `1.0.0-preview.{BUILD_NUMBER}`
- Example: `1.0.0-preview.42`
- Published to both NuGet.org and GitHub Packages
- Triggered on every push to `main` branch

### Release Versions (tags)
- Format: `{MAJOR}.{MINOR}.{PATCH}` (from git tag)
- Example: `1.2.3` (from tag `v1.2.3`)
- Published to NuGet.org only
- Triggered when creating a git tag matching `v*` pattern

## Required Setup

### 1. NuGet API Key

You need to create a NuGet API key and add it as a GitHub secret:

1. Go to [NuGet.org](https://www.nuget.org/)
2. Sign in to your account
3. Navigate to **Account Settings** → **API Keys**
4. Click **Create** and configure:
   - **Key Name**: `BAML-GitHub-Actions`
   - **Select Scopes**: `Push new packages and package versions`
   - **Select Packages**: `*` (or specify `Baml.*` if you prefer)
   - **Glob Pattern**: `*`
5. Copy the generated API key
6. In your GitHub repository, go to **Settings** → **Secrets and variables** → **Actions**
7. Click **New repository secret**
8. Name: `NUGET_API_KEY`
9. Value: Paste your NuGet API key
10. Click **Add secret**

### 2. GitHub Environments (Optional but Recommended)

For better security and visibility, set up GitHub environments:

1. Go to **Settings** → **Environments**
2. Create environment: `nuget-org`
   - Add protection rules if desired (e.g., require review for releases)
   - Add the `NUGET_API_KEY` secret to this environment
3. Create environment: `github-packages` (no additional setup needed)

## Publishing Workflow

### Publishing Preview Packages

Preview packages are automatically published when you push to the `main` branch:

```bash
git checkout main
git add .
git commit -m "feat: add new feature"
git push origin main
```

This will create a package like `Baml.Runtime.1.0.0-preview.42.nupkg`.

### Publishing Release Packages

Release packages require creating a git tag:

```bash
# Make sure you're on main and up to date
git checkout main
git pull origin main

# Create and push a version tag
git tag v1.2.3
git push origin v1.2.3
```

This will create packages like `Baml.Runtime.1.2.3.nupkg`.

**Important**: The tag must follow the format `v{MAJOR}.{MINOR}.{PATCH}` (e.g., `v1.0.0`, `v2.1.5`).

### Manual Trigger

You can also manually trigger the workflow:

1. Go to **Actions** tab in your repository
2. Select **Build and Publish NuGet Packages**
3. Click **Run workflow**
4. Select the branch and click **Run workflow**

## Package Structure

### Baml.Runtime Package
- **Target Framework**: .NET 8.0
- **Package Type**: Standard library
- **Contains**: Runtime components for BAML client execution
- **Dependencies**: Microsoft.SourceLink.GitHub (build-time only)

### Baml.SourceGenerator Package
- **Target Framework**: .NET Standard 2.0
- **Package Type**: Analyzer/Source Generator
- **Contains**: MSBuild targets, source generator, and build integration
- **Dependencies**: Microsoft.CodeAnalysis.* (build-time only)

## Troubleshooting

### Build Failures

If the build fails:
1. Check the **Actions** tab for detailed error logs
2. Common issues:
   - Missing dependencies
   - Test failures
   - Compilation errors

### Publishing Failures

If publishing fails:
1. Verify the `NUGET_API_KEY` secret is correctly set
2. Check if the package version already exists on NuGet.org
3. Ensure the API key has sufficient permissions

### Version Conflicts

If you get version conflicts:
- For preview packages: They automatically increment with build numbers
- For release packages: Make sure to use a new tag version

## Monitoring

### Package Status

Monitor your packages at:
- **NuGet.org**: https://www.nuget.org/packages?q=Baml
- **GitHub Packages**: https://github.com/{your-username}/baml-dotnet/packages

### Workflow Status

Monitor workflow runs at:
- **GitHub Actions**: https://github.com/{your-username}/baml-dotnet/actions

## Best Practices

1. **Always test locally first**:
   ```bash
   dotnet restore
   dotnet build --configuration Release
   dotnet test --configuration Release
   dotnet pack --configuration Release
   ```

2. **Use semantic versioning**:
   - `v1.0.0` → `v1.0.1` (patch)
   - `v1.0.0` → `v1.1.0` (minor)
   - `v1.0.0` → `v2.0.0` (major)

3. **Update release notes**: Consider updating the README or creating GitHub releases

4. **Monitor download stats**: Keep track of package adoption on NuGet.org

## Security Considerations

- The `NUGET_API_KEY` has push permissions - keep it secure
- Consider using environment protection rules for production releases
- Regularly rotate API keys (recommended every 6-12 months)
- Review GitHub Actions logs for any credential exposure

## Support

If you encounter issues:
1. Check this documentation
2. Review the workflow file: `.github/workflows/nuget-publish.yml`
3. Examine recent GitHub Actions runs
4. Open an issue in the repository
