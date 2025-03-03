# AIStorm - Project Overview

## Introduction

AIStorm is a Blazor Server application that allows users to set up brainstorming sessions with multiple AI agents as participants. The application uses markdown files as a database to store conversations, with one folder per conversation.

## Purpose and Goals

- Create an interactive brainstorming environment with AI agents
- Enable users to configure multiple AI agents with different roles/personalities
- Provide a simple and intuitive user interface
- Store conversations in a human-readable format (markdown)

## Key Features

- Configurable number of AI agents in a brainstorming session
- Customizable agent roles and personalities via system prompts
- Integration with various AI service APIs
- Markdown-based storage system for conversations
- HTML/JS frontend for user interaction

## Technical Architecture

### Blazor Server Application

- Server-side Blazor application
- Real-time communication using SignalR
- HTML/JS frontend for user interface

### Project Structure

```
AIStorm/
├── .clinerules                 # Custom rules for AI assistant
├── .gitignore                  # Git ignore file
├── .markdownlint.json          # Markdown linting configuration
├── AIStorm.sln                 # Solution file
├── README.md                   # Project readme
├── docs/
│   ├── project-overview.md     # High-level project overview
│   └── system-architecture.md  # System architecture and implementation details
├── src/
│   ├── Core/                   # Core business logic
│   │   ├── Models/             # Domain models
│   │   ├── AI/                 # AI provider implementations
│   │   ├── Storage/            # Storage interfaces and base implementations
│   │   │   └── Markdown/       # Markdown-specific storage implementation
│   │   ├── SessionManagement/  # Session management components
│   │   └── Common/             # Common utilities
│   └── Server/                 # Blazor Server application
│       ├── Pages/              # Razor pages
│       ├── Shared/             # Shared components
│       └── wwwroot/            # Static files
└── test/
    ├── Core.Tests/             # Unit tests for Core project
    │   ├── Services/           # Tests for services
    │   └── TestData/           # Test data for unit tests
    └── Core.IntegrationTests/  # Integration tests for Core project
```

The application creates storage directories at runtime to store sessions and agent templates.

## File Structure

The storage structure for sessions and agents is organized as follows:

```
StorageRoot/
├── AgentTemplates/           # Reusable agent definitions (flat files)
│   ├── Creative Thinker.md
│   ├── Critical Analyst.md
│   └── ...
├── Sessions/                 # Self-contained sessions (flat files)
│   ├── Session1.session.md   # Complete session with embedded agents and premise
│   ├── Session2.session.md
│   └── ...
```

Benefits of this approach:
- Clear separation between templates and instances
- Simplified file structure with fewer nested directories
- Self-contained session files with embedded agents and premise
- Consistent naming convention
- Use of `.session.md` extension makes the file type clear

## User Experience

### Basic Workflow

1. Create a new brainstorming session
2. Configure AI agents (number, roles, personalities)
3. Start the brainstorming session
4. Interact with the agents
5. Save/export the conversation

## Project-Specific Conventions

### Naming and Organization

- **Namespaces**: `AIStorm.{Module}` (e.g., `AIStorm.Core`, `AIStorm.Server`)
- **Documentation**:
  - Keep documentation updated with architectural decisions
  - Document AI agent system prompts and their intended roles
  - Document markdown file structure and format

## Open Questions

- What specific AI service APIs will be supported initially? (OpenAI is implemented)
- How should we implement the UI for the brainstorming session?
- What visualization features should be included for the brainstorming results?
- What export formats should be supported?
- Are there any authentication or user management requirements?

## Related Documentation

- See [System Architecture](system-architecture.md) for technical implementation details

## Useful info

Cline data is at `%APPDATA%\Roaming\Code\User\globalStorage\saoudrizwan.claude-dev\tasks`.
