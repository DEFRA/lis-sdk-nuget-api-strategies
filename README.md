# Defra Livestock API SDK (NuGet)
**Root Namespace**: *Defra.Livestock.Sdk.Api*

---

## Strategies

**Namespace**: *Defra.Livestock.Sdk.Api.Strategies*

This package contains fluent strategy builders.

### Publishing

Publishing details are currently being established.

### Local Development

This project uses private NuGet packages hosted on GitHub Packages. To restore dependencies locally, you need to:

1. Create a [GitHub Personal Access Token (PAT)](https://github.com/settings/tokens) with `read:packages` scope.
2. Configure the source in your local `nuget.config` or via command line:
   ```bash
   dotnet nuget add source --name github --username YOUR_GITHUB_USERNAME --password YOUR_PAT --store-password-in-clear-text https://nuget.pkg.github.com/DEFRA/index.json
   ```

