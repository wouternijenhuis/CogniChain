# Contributing to CogniChain

Thanks for your interest in contributing! This document covers the essentials — see the
[Code of Conduct](CODE_OF_CONDUCT.md) too.

## Reporting bugs / suggesting features

Open an issue with a clear title, repro steps (or a use case, for a feature request), and your .NET
version. Check existing issues first.

## Pull requests

1. Fork the repo and branch from `main`: `git checkout -b feature/my-change`.
2. Make your change: follow the existing code style, add tests, update docs for user-facing changes.
3. Verify locally (see below).
4. Open a PR with a clear title/description and a link to any related issue.

## Development setup

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (the version pinned in
`global.json`), a code editor, Git.

```bash
git clone https://github.com/wouternijenhuis/CogniChain.git
cd CogniChain
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes   # what CI checks; drop --verify-no-changes to auto-fix
```

To run an example (needs a real provider key — see each project's README):

```bash
dotnet run --project examples/CogniChain.Examples.OpenAI/CogniChain.Examples.OpenAI.csproj
```

### Regenerating the package icon

`icon.png` is committed and used as-is by the packing target; it isn't regenerated automatically. To
rebuild it from `icon.svg` after an edit:

```bash
dotnet tool restore
dotnet Svg.Skia.Converter -f icon.svg --outputFiles icon.png
```

## Coding standards

- Follow the [C# coding conventions](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- XML doc comments on every public member.
- Naming: `PascalCase` types/methods, `camelCase` parameters/locals, `_camelCase` private fields.

## Testing

- xUnit v3 on Microsoft Testing Platform (`tests/CogniChain.Tests`). Test names:
  `MethodName_Scenario_ExpectedBehavior`. Arrange/Act/Assert, with those comments in place.
- Test against `Microsoft.Extensions.AI.IChatClient`, not a real provider — see
  `tests/CogniChain.Tests/Fakes/FakeChatClient.cs` and
  [`docs/best-practices.md`](docs/best-practices.md#testing).

```csharp
[Fact]
public async Task RunAsync_SequentialThenSteps_PipesOutputThroughEachStep()
{
    // Arrange
    var client = new FakeChatClient();
    var chain = Chain.Create<string>(client).Then<string>(input => input.ToUpperInvariant()).Build();

    // Act
    var result = await chain.RunAsync("hello");

    // Assert
    Assert.Equal("HELLO", result.Value);
}
```

## Commit messages

First line a brief summary (≤ 50 chars), blank line, detail if needed, issue reference if applicable.

## Review process

CI (build, `dotnet format` check, tests) must pass. At least one maintainer approval is required;
PRs are squash-merged.

## Release process (maintainers)

1. Update `CHANGELOG.md`.
2. Bump `<Version>` in `src/CogniChain/CogniChain.csproj` and `src/CogniChain.Agents/CogniChain.Agents.csproj`.
3. Tag `vX.Y.Z` and push — `release.yml` packs, tests, publishes the GitHub release, and pushes to
   NuGet for stable (non-prerelease) tags.

## Getting help

- 💬 [GitHub Discussions](https://github.com/wouternijenhuis/CogniChain/discussions)
- 🐛 [GitHub Issues](https://github.com/wouternijenhuis/CogniChain/issues)
- 🔒 Security issues: see [SECURITY.md](SECURITY.md)

## License

By contributing, you agree your contributions are licensed under the MIT License.

---

Thank you for contributing to CogniChain! 🎉
