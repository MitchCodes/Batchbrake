# Release Process for Batchbrake

This document outlines the complete release process for Batchbrake, including automated builds and manual steps.

## Prerequisites

### 1. Initial Repository Setup (One-time)
- [ ] Ensure your repository is public or you have GitHub Actions enabled
- [ ] Repository has `main` branch as default
- [ ] `.github/workflows/` directory contains the workflow files
- [ ] Project file includes version information

### 2. Required Secrets (Already configured by default)
- `GITHUB_TOKEN` - Automatically provided by GitHub Actions
- No additional secrets needed for basic releases

### 3. Code Signing (Optional, for future enhancement)
For signed releases, you'll need:
- **Windows**: Code signing certificate
- **macOS**: Apple Developer ID certificate
- **Linux**: GPG key for package signing

## Version Management

### Semantic Versioning
Follow semantic versioning (MAJOR.MINOR.PATCH):
- **MAJOR**: Breaking changes
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

Example: `1.0.0`, `1.1.0`, `1.1.1`, `2.0.0`

## Release Process Steps

### Step 1: Prepare the Release

1. **Update Version in Project File**
   ```xml
   <!-- In Batchbrake/Batchbrake.csproj -->
   <PropertyGroup>
     <Version>1.0.0</Version>
   </PropertyGroup>
   ```

2. **Update CHANGELOG.md**
   ```markdown
   ## [1.0.0] - 2024-01-15
   ### Added
   - Feature X
   ### Fixed
   - Bug Y
   ### Changed
   - Behavior Z
   ```

3. **Update README.md if needed**
   - New features documentation
   - Updated screenshots
   - Installation instructions changes

4. **Run Final Tests Locally**
   ```bash
   dotnet test
   dotnet build --configuration Release
   ```

### Step 2: Create Release Branch (Optional but Recommended)

```bash
git checkout -b release/v1.0.0
git add .
git commit -m "Prepare release v1.0.0"
git push origin release/v1.0.0
```

### Step 3: Trigger Automated Release

#### Option A: Using Git Tags (Recommended)
```bash
# Create and push a version tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

This automatically triggers the release workflow.

#### Option B: Manual Workflow Dispatch
1. Go to GitHub repository
2. Click "Actions" tab
3. Select "Release Build" workflow
4. Click "Run workflow"
5. Enter version number (e.g., "1.0.0")
6. Click "Run workflow"

### Step 4: Monitor Build Process

1. Go to Actions tab on GitHub
2. Watch the "Release Build" workflow
3. Check for any failures in:
   - Windows builds (x64, x86, ARM64)
   - macOS builds (Intel, Apple Silicon)
   - Linux builds (x64, ARM, ARM64)

Expected duration: ~15-30 minutes

### Step 5: Review Draft Release

1. Go to "Releases" page
2. Find the draft release created by the workflow
3. Review:
   - [ ] All assets are uploaded (9+ files)
   - [ ] Checksums file is present
   - [ ] Release notes are accurate

### Step 6: Edit Release Notes

Edit the auto-generated release notes:

```markdown
# Batchbrake v1.0.0

## 🎉 Highlights
- Major feature or improvement
- Performance enhancements
- Critical bug fixes

## ✨ What's New
- Feature 1: Description
- Feature 2: Description

## 🐛 Bug Fixes
- Fixed issue with X (#123)
- Resolved problem with Y (#124)

## 💔 Breaking Changes
- Any breaking changes here

## 📦 Installation

### Windows
Download `Batchbrake-Windows-x64.zip` (or x86/ARM64 variants)

### macOS
- Intel: `Batchbrake-macOS-osx-x64.tar.gz`
- Apple Silicon: `Batchbrake-macOS-osx-arm64.tar.gz`

### Linux
- Standard: `Batchbrake-Linux-x64.tar.gz`
- AppImage: `Batchbrake-Linux-x64.AppImage`
- ARM: `Batchbrake-Linux-arm.tar.gz` or `arm64`

## 📋 Requirements
- .NET 8 Runtime (included in self-contained builds)
- HandBrakeCLI (install separately)
- FFmpeg (optional)

## 🔐 Verification
Check `checksums.txt` for SHA256 hashes

## 👥 Contributors
Thanks to everyone who contributed!

**Full Changelog**: https://github.com/[username]/batchbrake/compare/v0.9.0...v1.0.0
```

### Step 7: Test Release Assets

Before publishing, download and test at least one build:

1. Download a release asset
2. Verify checksum:
   ```bash
   # Windows PowerShell
   Get-FileHash Batchbrake-Windows-x64.zip -Algorithm SHA256
   
   # macOS/Linux
   sha256sum Batchbrake-Linux-x64.tar.gz
   ```
3. Extract and run the application
4. Verify basic functionality

### Step 8: Publish Release

1. If everything looks good, click "Publish release"
2. This makes the release publicly available
3. The release will appear as "Latest release"

### Step 9: Post-Release Tasks

1. **Announce the Release**
   - Social media
   - Project website
   - User forums/Discord

2. **Update Documentation**
   - Wiki pages
   - API documentation
   - User guides

3. **Merge Release Branch**
   ```bash
   git checkout main
   git merge release/v1.0.0
   git push origin main
   ```

4. **Create Next Development Branch**
   ```bash
   git checkout -b develop
   git push origin develop
   ```

## Troubleshooting

### Build Failures

**Windows Build Fails**
- Check for Windows-specific path issues
- Verify no hardcoded paths

**macOS Build Fails**
- Check Info.plist generation
- Verify bundle structure

**Linux Build Fails**
- Check AppImage dependencies
- Verify desktop file syntax

### Asset Upload Failures
- Check GitHub API rate limits
- Verify file sizes (< 2GB per file)
- Check network connectivity

### Missing Architectures
- Verify matrix configuration in workflow
- Check runner availability for architecture

## Hotfix Process

For critical bugs in released versions:

1. Create hotfix branch from tag:
   ```bash
   git checkout -b hotfix/v1.0.1 v1.0.0
   ```

2. Apply fixes and test thoroughly

3. Update version to patch release:
   ```xml
   <Version>1.0.1</Version>
   ```

4. Create and push tag:
   ```bash
   git tag -a v1.0.1 -m "Hotfix release v1.0.1"
   git push origin v1.0.1
   ```

## Manual Build Commands

If you need to build releases manually:

### Windows
```powershell
dotnet publish Batchbrake/Batchbrake.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

### macOS
```bash
dotnet publish Batchbrake/Batchbrake.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

### Linux
```bash
dotnet publish Batchbrake/Batchbrake.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

## Release Checklist Template

Copy this for each release:

```markdown
## Release v_._._. Checklist

### Pre-Release
- [ ] All tests passing
- [ ] Version updated in .csproj
- [ ] CHANGELOG.md updated
- [ ] README.md reviewed
- [ ] Commit all changes
- [ ] Create release branch

### Release
- [ ] Create and push tag
- [ ] Monitor GitHub Actions
- [ ] Verify all builds complete
- [ ] Test at least one platform

### Post-Release
- [ ] Edit release notes
- [ ] Publish release
- [ ] Announce release
- [ ] Merge to main
- [ ] Delete release branch
```

## Security Considerations

1. **Never commit secrets** to the repository
2. **Use GitHub Secrets** for sensitive data
3. **Review dependencies** for vulnerabilities
4. **Sign releases** when possible
5. **Provide checksums** for verification

## Support

If you encounter issues with the release process:
1. Check GitHub Actions logs
2. Review this documentation
3. Open an issue in the repository
4. Contact the maintainers

---

Last updated: 2024
Version: 1.0.0