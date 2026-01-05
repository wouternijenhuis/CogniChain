# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/wouternijenhuis/CogniChain/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/wouternijenhuis/CogniChain/releases/tag/v0.1.0
