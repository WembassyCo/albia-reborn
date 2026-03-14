# Build Instructions - Albia Reborn

This document details how to set up your development environment and build the project locally.

## 📋 Prerequisites

### Required Software

| Software | Version | Download |
|----------|---------|----------|
| Unity Hub | Latest | [unity.com](https://unity.com/download) |
| Unity Editor | 6000.0.37f1 LTS | Via Unity Hub |
| Git | 2.30+ | [git-scm.com](https://git-scm.com/) |
| Git LFS | 3.0+ | Included with Git or [git-lfs.github.com](https://git-lfs.github.com/) |

### System Requirements

**Minimum:**
- OS: Windows 10, macOS 12, or Linux Ubuntu 20.04+
- RAM: 16GB (32GB recommended)
- GPU: DirectX 11 / Metal / Vulkan compatible
- Storage: 10GB free space

**Recommended:**
- RAM: 32GB+
- GPU: Dedicated GPU with 4GB+ VRAM
- Storage: SSD with 20GB free space

## 🛠️ Setup Instructions

### 1. Install Unity Hub

Download from [unity.com](https://unity.com/download) and install.

### 2. Install Unity 6 LTS

```bash
# Open Unity Hub
# Navigate to: Installs > Install Editor
# Select Unity 6000.0.37f1 LTS
# Ensure these modules are selected:
# - Android Build Support (if targeting mobile)
# - iOS Build Support (if targeting iOS)
# - Linux Build Support (Mono)
# - Documentation
```

Or via Unity Hub CLI:
```bash
unity-hub install --version 6000.0.37f1
```

### 3. Clone Repository

```bash
# Clone with Git LFS support
git clone https://github.com/WembassyCo/albia-reborn.git
cd albia-reborn

# Initialize Git LFS
git lfs install
git lfs pull
```

### 4. Open in Unity

```bash
# Via Unity Hub GUI
# 1. Open Unity Hub
# 2. Click "Open"
# 3. Navigate to the albia-reborn folder
# 4. Select the project

# Via Unity Hub CLI
unity-hub --open /path/to/albia-reborn

# Via Unity directly
/Applications/Unity/Hub/Editor/6000.0.37f1/Unity.app/Contents/MacOS/Unity -projectPath /path/to/albia-reborn
```

### 5. Package Restoration

Unity will automatically restore packages on first open. Wait for the package manager to complete:

```
Window > Package Manager
```

You should see these key packages installed:
- Universal Render Pipeline (17.0.4)
- AI Navigation (2.0.6)
- Cinemachine (2.10.3)
- Input System (1.14.0)

## 🔨 Local Development Build

### Windows Build

```bash
# From Unity Editor
File > Build Settings > Standalone Windows x86_64 > Build

# Or via command line
/Applications/Unity/Hub/Editor/6000.0.37f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -quit \
  -projectPath /path/to/albia-reborn \
  -buildTarget StandaloneWindows64 \
  -buildWindows64Player /path/to/output/AlbiaReborn.exe
```

### macOS Build

```bash
# From Unity Editor
File > Build Settings > Standalone macOS > Build

# Or via command line
/Applications/Unity/Hub/Editor/6000.0.37f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -quit \
  -projectPath /path/to/albia-reborn \
  -buildTarget StandaloneOSX \
  -buildOSXUniversalPlayer /path/to/output/AlbiaReborn.app
```

### Linux Build

```bash
# From Unity Editor
File > Build Settings > Standalone Linux x86_64 > Build

# Or via command line
unity-editor \
  -batchmode \
  -nographics \
  -quit \
  -projectPath /path/to/albia-reborn \
  -buildTarget StandaloneLinux64 \
  -buildLinux64Player /path/to/output/AlbiaReborn
```

## 🧪 Running Tests

### Edit Mode Tests

```bash
# Via Unity Editor
Window > General > Test Runner > EditMode > Run All

# Via command line
unity-editor \
  -runTests \
  -projectPath /path/to/albia-reborn \
  -testResults /path/to/results.xml \
  -testPlatform EditMode
```

### Play Mode Tests

```bash
# Via Unity Editor
Window > General > Test Runner > PlayMode > Run All

# Via command line
unity-editor \
  -runTests \
  -projectPath /path/to/albia-reborn \
  -testResults /path/to/results.xml \
  -testPlatform PlayMode
```

## 🔧 Troubleshooting

### Package Resolution Issues

If packages fail to restore:

```bash
# Delete Library folder and reopen Unity
rm -rf Library/
# Then reopen in Unity Hub
```

### Git LFS Issues

```bash
# Verify Git LFS is installed
git lfs version

# Pull LFS files manually if needed
git lfs pull

# Track new large files
git lfs track "*.psd"
git lfs track "*.fbx"
git lfs track "*.mp3"
```

### URP Shader Compilation

If shaders appear pink:

1. Window > Rendering > URP Wizard
2. Click "Fix All"
3. Or manually: Edit > Project Settings > Graphics
4. Ensure URP pipeline asset is assigned

### Build Failures

Common causes:
- **Missing modules**: Install build support modules in Unity Hub
- **Script errors**: Check Console for compilation errors
- **Out of memory**: Close other applications or increase system RAM

## 🐳 Docker Build (Optional)

For headless CI/CD builds, a Dockerfile is available:

```bash
docker build -t albia-reborn-builder .
docker run --rm -v $(pwd):/project albia-reborn-builder
```

Note: Docker builds require proper Unity licensing (see CI/CD below).

## 🔄 CI/CD Pipeline

The project uses GitHub Actions for automated builds. To use:

1. Fork the repository
2. Add these GitHub Secrets in Settings > Secrets > Actions:
   - `UNITY_LICENSE`: Your Unity license file (base64 encoded)
   - `UNITY_EMAIL`: Unity account email
   - `UNITY_PASSWORD`: Unity account password

3. Push to main or create a PR to trigger builds

### Manual License Activation

```bash
# Request a license file from Unity
# Encode for GitHub Secrets:
base64 -i Unity_v6000.ulf | pbcopy
```

## 📝 Project Configuration

### Unity Version

This project requires **Unity 6000.0.37f1 LTS** or newer within the 6000.x stream.

Update `ProjectSettings/ProjectVersion.txt` if upgrading:
```
m_EditorVersion: 6000.0.37f1
```

### Render Pipeline

Universal Render Pipeline is configured. To modify:

```
Window > Rendering > URP Pipeline Asset
```

### Input System

The new Input System is enabled. Configure at:

```
Edit > Project Settings > Input System Package
```

## 📚 Additional Resources

- [Unity Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/)
- [URP Documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/)
- [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@1.5/manual/)
- [Git LFS Documentation](https://git-lfs.github.com/)

## 🆘 Getting Help

- Open an [Issue](https://github.com/WembassyCo/albia-reborn/issues)
- Check [Discussions](https://github.com/WembassyCo/albia-reborn/discussions)
- Review [Wiki](https://github.com/WembassyCo/albia-reborn/wiki)

---

**Last Updated**: March 2025
**Unity Version**: 6000.0.37f1 LTS
