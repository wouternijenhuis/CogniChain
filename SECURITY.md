# Security Policy

## Supported versions

| Version | Supported |
|---|---|
| 0.3.x | ✅ |
| < 0.3.0 | ❌ (upgrade — see [migration guide](docs/migration.md)) |

## Reporting a vulnerability

Please **do not** open a public GitHub issue for a security vulnerability. Instead, use GitHub's
[private vulnerability reporting](https://github.com/wouternijenhuis/CogniChain/security/advisories/new)
for this repository, or open a
[GitHub Discussion](https://github.com/wouternijenhuis/CogniChain/discussions) marked private if
reporting isn't enabled, and a maintainer will follow up.

Include, where possible:

- A description of the vulnerability and its impact.
- Steps to reproduce, or a minimal repro project.
- The CogniChain version(s) affected.

We aim to acknowledge reports within 5 business days and to release a fix or mitigation guidance within
30 days for confirmed issues, sooner for anything critical.

## Scope notes

CogniChain composes calls to a model you provide the `IChatClient` for. It does not sanitize prompts,
tool arguments, or model output — see [`docs/best-practices.md`](docs/best-practices.md#tools) for
guidance on treating both as untrusted input. Vulnerabilities in an underlying provider SDK
(`OpenAI`, `Azure.AI.OpenAI`, `Microsoft.Extensions.AI`, `Microsoft.Agents.AI`) should be reported to
those projects directly; we'll track and adopt fixes via Dependabot.
