# Contributing to CogniChain

Thank you for your interest in contributing to CogniChain! This document provides guidelines and instructions for contributing.

## Code of Conduct

Please read and follow our [Code of Conduct](CODE_OF_CONDUCT.md).

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:

- A clear, descriptive title
- Detailed steps to reproduce
- Expected vs. actual behavior
- Code samples (if applicable)
- Environment details (.NET version, OS, etc.)

### Suggesting Features

We welcome feature suggestions! Please:

- Check if the feature has already been requested
- Provide a clear use case
- Explain the expected behavior
- Consider backward compatibility

### Pull Requests

1. **Fork the repository** and create your branch from `main`:
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Make your changes**:
   - Write clear, concise commit messages
   - Follow the existing code style
   - Add tests for new functionality
   - Update documentation as needed

3. **Test your changes**:
   ```bash
   dotnet build
   dotnet test
   ```

4. **Submit your PR** with:
   - A clear title and description
   - Reference to any related issues
   - Screenshots for UI changes (if applicable)

## Development Setup

### Prerequisites

- .NET 10 SDK or later
- A code editor (Visual Studio, VS Code, or Rider)
- Git

### Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/wouternijenhuis/CogniChain.git
   cd CogniChain
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run tests:
   ```bash
   dotnet test
   ```

5. Run examples:
   ```bash
   dotnet run --project examples/CogniChain.Examples/CogniChain.Examples/CogniChain.Examples.csproj
   ```

## Coding Standards

### C# Style Guidelines

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods focused and concise
- Add XML documentation comments for public APIs

### Code Structure

```csharp
namespace CogniChain;

/// <summary>
/// Brief description of the class.
/// </summary>
public class MyClass
{
    /// <summary>
    /// Description of the method.
    /// </summary>
    /// <param name="parameter">Parameter description.</param>
    /// <returns>Return value description.</returns>
    public string MyMethod(string parameter)
    {
        // Implementation
    }
}
```

### Naming Conventions

- **Classes**: PascalCase (e.g., `PromptTemplate`)
- **Methods**: PascalCase (e.g., `ExecuteAsync`)
- **Parameters**: camelCase (e.g., `inputText`)
- **Private fields**: _camelCase (e.g., `_repository`)
- **Constants**: PascalCase (e.g., `MaxRetries`)

### Testing

- Write unit tests for all new functionality
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`
- Follow the Arrange-Act-Assert pattern
- Aim for high code coverage

Example:
```csharp
[Fact]
public async Task ExecuteAsync_WithValidInput_ReturnsSuccess()
{
    // Arrange
    var step = new MyStep();
    var input = "test";

    // Act
    var result = await step.ExecuteAsync(input);

    // Assert
    Assert.True(result.Success);
    Assert.Equal("expected", result.Output);
}
```

## Documentation

- Update README.md for user-facing changes
- Update API reference for new public APIs
- Add examples for new features
- Keep documentation clear and concise

## Commit Messages

Write clear commit messages:

```
Add retry logic to chain execution

- Implement exponential backoff
- Add configurable retry policy
- Include unit tests

Fixes #123
```

Format:
- First line: Brief summary (50 chars or less)
- Blank line
- Detailed description (if needed)
- Reference issues/PRs

## Review Process

1. **Automated checks** run on all PRs:
   - Build verification
   - Test execution
   - Code quality checks

2. **Code review** by maintainers:
   - Code quality and style
   - Test coverage
   - Documentation completeness

3. **Approval and merge**:
   - At least one maintainer approval required
   - All checks must pass
   - Squash and merge for clean history

## Release Process

Maintainers handle releases:

1. Update CHANGELOG.md
2. Update version in project file
3. Create release tag
4. Publish to NuGet

## Getting Help

- 💬 [GitHub Discussions](https://github.com/wouternijenhuis/CogniChain/discussions) for questions
- 🐛 [GitHub Issues](https://github.com/wouternijenhuis/CogniChain/issues) for bugs
- 📧 Contact maintainers for security issues (see SECURITY.md)

## Recognition

Contributors are recognized in:
- CHANGELOG.md for their contributions
- README.md contributors section
- Release notes

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to CogniChain! 🎉
