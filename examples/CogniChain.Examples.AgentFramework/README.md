# CogniChain + Microsoft Agent Framework

Three runnable examples bridging CogniChain chains and the [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/), via the optional `CogniChain.Agents` package.

## Setup

```bash
export OPENAI_API_KEY="your-api-key-here"
export OPENAI_MODEL="gpt-5-mini"   # optional, this is the default
dotnet run
```

## What's included

| Example | Shows |
|---|---|
| Chain as an Agent | `chain.AsAIAgent(name, description)` — a chain usable anywhere an `AIAgent` is expected |
| Agent as a Chain Step | `agent.AsChainStep()` — a `ChatClientAgent` dropped into a chain as an ordinary `Then` step |
| MCP Tools | Tools listed from a Model Context Protocol server, wired straight into `WithTools(...)` |

The MCP example launches a public demo server via `npx` over stdio; it needs Node.js and network
access, and reports why it was skipped if either is unavailable — it's meant to show the wiring, not
to be a hard dependency of the sample suite.

## Why a separate package

Semantic Kernel is in maintenance mode; new agent orchestration work in the .NET ecosystem lands in
Microsoft Agent Framework (`Microsoft.Agents.AI`). `CogniChain.Agents` is a thin, optional bridge —
the core `CogniChain` package has no dependency on it.

## Learn more

- [CogniChain documentation](../../docs/)
- [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/overview/)
- [Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
