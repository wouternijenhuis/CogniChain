# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.3.0] - 2026-08-23

A full rebuild on [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)'s
`IChatClient`. Breaking change — see [`docs/migration.md`](docs/migration.md). The 0.2.x API's
headline features (conversation memory, tool calling, real streaming) never fully worked end to end;
0.3.0 delegates all of that to the platform instead of reimplementing it.

### Added

- `Chain<TIn, TOut>` / `ChainBuilder<TIn, TCurrent>`: typed, composable steps (`Prompt<T>`, `Then`,
  `Map`, `Branch`) over an `IChatClient`, with compile-time step-adjacency checking.
- Structured output via `Prompt<T>()` (JSON schema, deserialized automatically).
- Real tool calling via `AIFunction` (`WithTools`, `WithToolsFrom`) and `.UseFunctionInvocation()`.
- Working multi-turn conversation history via a reusable `ChainContext` and `IChatReducer`
  (`MessageCountReducer` ships built in).
- Real token-by-token streaming via `RunStreamingAsync`.
- `CogniChain.Middleware.RetryingChatClient`: correctly classifies transient failures, honors
  `Retry-After`, never retries cancellation, and never exceeds `RetryPolicy.MaxDelay`.
- Per-step OpenTelemetry spans via `ChainActivitySource`.
- `AddChain<TIn, TOut>(...)` for keyed DI registration of named chains.
- `Chain<TIn, TOut>.AsAIFunction()`: expose a chain as a tool another model can call.
- New optional package `CogniChain.Agents`: bridges chains and the Microsoft Agent Framework
  (`AsAIAgent()`, `AsChainStep()`).
- New example project `CogniChain.Examples.AgentFramework`, including a Model Context Protocol
  tool-calling sample.
- `PromptTemplate`: `{{`/`}}` escaping for literal braces (so JSON-bearing prompts work), and no
  re-entrant substitution of placeholder values.
- Migrated tests to xUnit v3 / Microsoft Testing Platform, with new coverage for retry, streaming, and
  history behavior that had none before.

### Changed

- .NET 10 target retained; all dependencies updated to their current stable versions
  (`Microsoft.Extensions.AI` 10.9.0, `Microsoft.Agents.AI` 1.19.0, `Azure.Identity` 1.21.0, etc.).
- CI/release workflows fixed (`package` job's pack path was broken on every push to `main`), action
  versions updated, and a `dotnet format` check added.

### Removed

- `IChainStep` (string-based), `ChainResult` (non-generic), `LLMOrchestrator`, `OrchestratorConfig`,
  `WorkflowBuilder`, `ConversationMemory`, `Message`, `ITool`/`ToolBase`/`ToolRegistry`, `RetryHandler`/
  `RetryException`, `StreamingHandler`/`StreamingResponse`. See the migration guide for replacements.
- `CogniChain.Examples.SemanticKernel` (replaced by `CogniChain.Examples.AgentFramework`).

## [0.2.1] - 2026-08-17

### Changed

- Refactored project files onto central package management (`Directory.Packages.props`).
- Replaced the NuGet publish workflow with a tag-triggered release workflow that generates release
  notes and publishes both the package and a GitHub release.
- Updated the Azure OpenAI package version and API-version handling in example settings.

## [0.2.0] - 2026-08-15

### Added

- Azure OpenAI, OpenAI, and Semantic Kernel example projects.
- Package icon (`icon.svg` / generated `icon.png`) and richer NuGet metadata.

## [0.1.0] - 2026-01-04

### Added

- Initial release of CogniChain
- Prompt template system with variable substitution
- Chain orchestration for sequential workflow execution
- Tool framework for LLM function calling
- Conversation memory with configurable history limits
- Retry logic with exponential backoff
- Streaming support for real-time LLM responses
- High-level LLM orchestrator combining all features
- Fluent workflow builder API
- Comprehensive XML documentation
- Unit tests with 100% coverage of core functionality
- Example project demonstrating all features
- Complete documentation:
  - README with quickstart guide
  - API reference documentation
  - Architecture guide
  - Best practices guide
- Community files:
  - CONTRIBUTING.md
  - CODE_OF_CONDUCT.md
  - SECURITY.md
  - MIT LICENSE
- NuGet package configuration
- GitHub Actions CI workflow

### Core Components

#### PromptTemplate
- Variable extraction and substitution
- Support for dictionary and object-based variables
- Input validation

#### Chain System
- Sequential step execution
- Output piping between steps
- Metadata collection
- Streaming support
- Error handling

#### ConversationMemory
- Message storage with roles (user, assistant, system)
- Configurable history limits
- System message preservation
- Message filtering and querying
- Formatted history output

#### ToolRegistry
- Tool registration and management
- Tool execution with async support
- Tool description generation
- Base class for custom tools

#### RetryHandler
- Exponential backoff
- Configurable retry policies
- Jitter support
- Max delay caps
- Generic execution wrapper

#### StreamingHandler
- IAsyncEnumerable-based streaming
- Event-driven chunk notification
- Simulation support for testing

#### LLMOrchestrator
- Unified API for all components
- Integrated retry logic
- Fluent workflow builder
- Centralized configuration

### Dependencies

- .NET 10.0
- xUnit 2.9.3 (testing)

### Known Limitations

- No built-in LLM API integration (bring your own)
- Sequential chain execution only (parallel coming soon)
- In-memory storage only for conversation history

---

[Unreleased]: https://github.com/wouternijenhuis/CogniChain/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/wouternijenhuis/CogniChain/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/wouternijenhuis/CogniChain/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/wouternijenhuis/CogniChain/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/wouternijenhuis/CogniChain/releases/tag/v0.1.0
