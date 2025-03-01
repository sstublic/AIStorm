# AIStorm - Specifications

## Useful info

Cline data is at `%APPDATA%\Roaming\Code\User\globalStorage\saoudrizwan.claude-dev\tasks`.

## Project Overview

AIStorm is a Blazor Server application that allows users to set up brainstorming sessions with multiple AI agents as participants. The application uses markdown files as a database to store conversations, with one folder per conversation.

### Purpose and Goals

- Create an interactive brainstorming environment with AI agents
- Enable users to configure multiple AI agents with different roles/personalities
- Provide a simple and intuitive user interface
- Store conversations in a human-readable format (markdown)

### Key Features

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
├── docs/
│   └── specs.md                # Specifications document
├── src/
│   ├── Server/                 # Blazor Server application (includes UI logic)
│   └── Core/                   # Core business logic (includes storage logic)
│       ├── Models/             # Domain models
│       │   └── Agent.cs        # Agent model
│       └── Services/           # Services
│           ├── IStorageProvider.cs      # Storage provider interface
│           └── MarkdownStorageProvider.cs # Markdown implementation
├── test/
│   └── Core.Tests/             # Tests for Core project
│       └── Services/           # Tests for services
│           └── MarkdownStorageProviderTests.cs # Tests for MarkdownStorageProvider
└── AIStormSessions/           # Root folder for brainstorming sessions
    └── Example/               # Example session
        ├── Agents/            # Agent definitions
        │   ├── Creative Thinker.md
        │   ├── Critical Analyst.md
        │   └── Practical Implementer.md
        └── conversation-log.md # Conversation log
```

## AI Agent System

### Agent Configuration

- Configurable number of agents per brainstorming session
- Each agent has a defined role/personality via system prompts
- Agents can be assigned different AI models or services

### AI Service Integration

- Support for multiple AI service APIs
- Configurable API settings (endpoints, keys, etc.)
- Abstraction layer to handle different API implementations

## Data Storage

### Markdown File Structure

- Root folder: `AIStormSessions`
- One subfolder per session with a user-descriptive name (e.g., "Example")
- Each session folder contains:
  - An `Agents` subfolder for agent definitions
  - A `conversation-log.md` file for the conversation content

### Agent Definition Files

Agent definition files are stored in the `Agents` subfolder. The filename is used as the agent name (e.g., `Creative Thinker.md`). XML tags are used for metadata:

```markdown
<aistorm type="OpenAI" model="gpt-4" />

# Creative Thinker

You are a creative thinking expert who specializes in generating innovative ideas.
Always think outside the box and challenge conventional wisdom.
When presented with a problem, explore multiple angles and perspectives.
Provide ideas that are both creative and practical.
```

### Conversation Log Format

The conversation is stored in `conversation-log.md` with XML tags as separators:

```markdown
<aistorm type="session" created="2025-03-01T15:00:00Z" description="Brainstorming session for new product ideas" />

# Brainstorming Session: New Product Ideas

<aistorm type="message" from="user" timestamp="2025-03-01T15:01:00Z" />

Let's brainstorm ideas for a new mobile app that helps people connect with local businesses.

<aistorm type="message" from="Creative Thinker" timestamp="2025-03-01T15:01:30Z" />

Here are some innovative ideas for a local business connection app:

1. **Neighborhood Pulse**: An app that uses AR to display real-time information about businesses as you walk past them.

<aistorm type="message" from="Practical Analyst" timestamp="2025-03-01T15:02:00Z" />

Building on those creative ideas, here are some practical considerations...
```

### Storage Implementation

#### Storage Provider Interface

The `IStorageProvider` interface defines the contract for storage providers:

- `LoadAgent` - Loads an agent from storage by ID
- `SaveAgent` - Saves an agent to storage

#### Markdown Storage Provider

The `MarkdownStorageProvider` implements the `IStorageProvider` interface for markdown files:

- Parses agent markdown files to extract metadata and system prompts
- Generates markdown files for agents with proper formatting
- Handles file system operations for reading and writing files

#### Testing

The storage implementation is tested with xUnit tests:

- Tests for loading agents from markdown files
- Tests for saving agents to markdown files
- Round-trip tests to ensure data integrity

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
- **Implemented Classes**:
  - `Agent` - Represents an AI agent with name, service type, model, and system prompt
  - `MarkdownStorageProvider` - Handles reading and writing markdown files
- **Implemented Interfaces**:
  - `IStorageProvider` - Interface for storage operations
- **Planned Classes**:
  - `BrainstormingSession` - Manages a brainstorming session with multiple agents
  - `Message` - Represents a message in a conversation
- **Planned Interfaces**:
  - `IAIServiceClient` - Interface for AI service API clients

### Documentation

- Keep `specs.md` updated with architectural decisions
- Document AI agent system prompts and their intended roles
- Document markdown file structure and format

## Open Questions

- What specific AI service APIs will be supported initially? (OpenAI is implemented)
- How should we handle the parsing of the conversation log to extract messages?
- How should we implement the UI for the brainstorming session?
- What visualization features should be included for the brainstorming results?
- What export formats should be supported?
- Are there any authentication or user management requirements?
