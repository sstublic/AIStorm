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
└── test/
    ├── Core.Tests/             # Unit tests for Core project
    │   ├── Services/           # Tests for services
    │   │   └── MarkdownStorageProviderTests.cs # Tests for MarkdownStorageProvider
    │   └── TestData/           # Test data for unit tests
    │       └── SessionExample/ # Example session for testing
    │           ├── Agents/     # Agent definitions
    │           │   ├── Creative Thinker.md
    │           │   ├── Critical Analyst.md
    │           │   └── Practical Implementer.md
    │           └── SessionExample.log.md # Conversation log
    └── Core.IntegrationTests/  # Integration tests for Core project
        ├── Program.cs          # Console app entry point for integration tests
        └── appsettings.json    # Configuration for integration tests
```

The application will create an `AIStormSessions` directory at runtime to store user sessions.

### File Structure Changes

Recent changes to the file structure:

- Session data is now stored in a single file with the session ID as the filename (e.g., `SessionExample.log.md`) rather than in a `conversation-log.md` file within a session directory
- Session ID is derived from the filename without extension (excluding the `.log` suffix) rather than the directory name
- Session files use the `.log.md` extension to distinguish them from `.ini.md` files that contain initial premises of the brainstorming session
- Session premise files use the `.ini.md` extension and contain the initial premise or context for a brainstorming session

## AI Agent System

### Agent Configuration

- Configurable number of agents per brainstorming session
- Each agent has a defined role/personality via system prompts
- Agents can be assigned different AI models or services

### AI Service Integration

- Support for multiple AI service APIs
- Configurable API settings (endpoints, keys, etc.)
- Abstraction layer to handle different API implementations
- Interface-based design with `IAIProvider` for common operations across providers
- Implemented OpenAI service as the first provider
- Comprehensive logging of API requests and responses, with Debug level including full request/response content
- Configuration options for OpenAI integration:
  - API key is required and can be provided via:
    - User secrets (in development): `dotnet user-secrets set "OpenAIOptions:ApiKey" "your-api-key"`
    - Environment variable: `OPENAIAPIOPTIONS_APIKEY=your-api-key`
    - appsettings.json (not recommended for production)
  - BaseUrl defaults to "https://api.openai.com/v1/" but can be configured for custom endpoints

#### Multi-Agent Conversation Format

When implementing conversations with multiple AI agents, we need a strategy for representing the conversation context when sending requests to AI APIs. Most AI APIs (like OpenAI) only support basic "user" and "assistant" roles, which doesn't directly map to our multi-agent scenario.

The recommended approach for AIStorm is to use a custom format in the system prompt:

1. Use the system prompt to explain the multi-agent conversation format
2. Mark the current agent's previous responses as "assistant" role
3. Mark all other messages (from human user and other agents) as "user" role with clear name prefixes
4. Include explicit instructions for when the agent should respond

Example API request format when sending a message to "Agent B":

```json
{
  "messages": [
    { 
      "role": "system", 
      "content": "You are Agent B, a critical analyst in a multi-agent brainstorming session. Messages will be prefixed with the sender's name in [brackets]. Only respond when asked to provide Agent B's perspective. Your role is to analyze ideas critically and identify potential issues or improvements."
    },
    { "role": "user", "content": "[Human]: What are some ideas for a weekend project?" },
    { "role": "assistant", "content": "[Agent B]: From an analytical perspective, we should consider time constraints and resource availability..." },
    { "role": "user", "content": "[Agent A]: Here are some creative ideas: 1. Build a herb garden..." },
    { "role": "user", "content": "[Agent C]: From a practical perspective, consider these factors..." },
    { "role": "user", "content": "[Human]: Agent B, what do you think about these ideas?" }
  ]
}
```

Implementation considerations:

- The `IAIProvider` interface should include methods for sending conversation context to an AI service
- When preparing a request for a specific agent, the conversation history needs to be transformed into the appropriate format
- The system prompt should combine the agent's base prompt (from its definition file) with instructions about the conversation format
- Messages should be clearly labeled with the sender's identity to maintain conversation clarity

## Data Storage

### Markdown File Structure

- Root folder: `AIStormSessions`
- One subfolder per session with a user-descriptive name (e.g., "Example")
- Each session folder contains:
  - An `Agents` subfolder for agent definitions
  - A markdown file with the session ID as the filename (e.g., `SessionExample.log.md`) for the conversation content

### Agent Definition Files

Agent definition files are stored in the `Agents` subfolder. The filename is used as the agent name (e.g., `Creative Thinker.md`). XML tags are used for metadata, and the content after the tag is used as the system prompt:

```markdown
<aistorm type="OpenAI" model="gpt-4" />

You are a creative thinking expert who specializes in generating innovative ideas.
Always think outside the box and challenge conventional wisdom.
When presented with a problem, explore multiple angles and perspectives.
Provide ideas that are both creative and practical.
```

### Session Premise Format

The session premise is stored in a markdown file with the session ID as the filename (e.g., `SessionExample.ini.md`). It contains an empty `<aistorm>` tag and the premise content:

```markdown
<aistorm />

This is an example session premise for a brainstorming session about weekend projects.
```

### Conversation Log Format

The conversation is stored in a markdown file with the session ID as the filename (e.g., `SessionExample.log.md`) with XML tags as separators:

```markdown
<aistorm type="session" created="2025-03-01T15:00:00" description="Brainstorming session for new product ideas" />

# Brainstorming Session: New Product Ideas

<aistorm type="message" from="user" timestamp="2025-03-01T15:01:00" />

## [user]:

Let's brainstorm ideas for a new mobile app that helps people connect with local businesses.

<aistorm type="message" from="Creative Thinker" timestamp="2025-03-01T15:01:30" />

## [Creative Thinker]:

Here are some innovative ideas for a local business connection app:

1. **Neighborhood Pulse**: An app that uses AR to display real-time information about businesses as you walk past them.

<aistorm type="message" from="Practical Analyst" timestamp="2025-03-01T15:02:00" />

## [Practical Analyst]:

Building on those creative ideas, here are some practical considerations...
```

### Storage Implementation

#### Storage Provider Interface

The `IStorageProvider` interface defines the contract for storage providers:

- `LoadAgent` - Loads an agent from storage by ID
- `SaveAgent` - Saves an agent to storage
- `LoadSession` - Loads a session from storage by ID
- `SaveSession` - Saves a session to storage
- `LoadSessionPremise` - Loads a session premise from storage by ID
- `SaveSessionPremise` - Saves a session premise to storage

#### Markdown Storage Provider

The `MarkdownStorageProvider` implements the `IStorageProvider` interface for markdown files:

- Parses agent markdown files to extract metadata and system prompts
- Generates markdown files for agents with proper formatting
- Parses session markdown files to extract metadata and messages
- Generates markdown files for sessions with proper formatting
- Handles file system operations for reading and writing files

#### Markdown Serialization

The storage implementation uses several classes to handle markdown serialization:

- `MarkdownSerializer` - Handles serialization and deserialization of markdown documents
- `MarkdownSegment` - Represents a segment of a markdown document with properties and content
- `OrderedProperties` - Maintains deterministic property ordering in markdown tags
- `Tools` - Provides utility functions for handling dates and times

#### Testing

The storage implementation is tested with xUnit tests:

- Tests for loading agents from markdown files
- Tests for saving agents to markdown files
- Tests for loading sessions from markdown files
- Tests for saving sessions to markdown files
- Tests for loading session premises from markdown files
- Tests for saving session premises to markdown files
- Tests for markdown serialization and deserialization
- Round-trip tests to ensure data integrity for agents, sessions, and session premises

## Logging

- The Core project uses Microsoft.Extensions.Logging for logging support
- Logging is integrated with dependency injection via `services.AddLogging()`
- Different log levels are used appropriately:
  - Debug: Detailed information including full API request/response payloads
  - Information: General flow information like initialization and operation completion
  - Warning: Issues that don't prevent operation but may indicate problems
  - Error: Errors and exceptions that affect functionality
- The OpenAIProvider logs:
  - API requests with full content (at Debug level)
  - API responses with full content (at Debug level)
  - Error details in case of failures
  - Basic operation information (at Information level)

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
  - `Session` - Represents a brainstorming session with metadata and a list of messages
  - `SessionPremise` - Represents the initial premise or context for a brainstorming session
  - `StormMessage` - Represents a message in a conversation with agent name, timestamp, and content
  - `MarkdownStorageProvider` - Handles reading and writing markdown files
  - `MarkdownSerializer` - Handles serialization and deserialization of markdown documents
  - `MarkdownSegment` - Represents a segment of a markdown document with properties and content
  - `OrderedProperties` - Maintains deterministic property ordering in markdown tags
  - `Tools` - Provides utility functions for handling dates and times
  - `OpenAIProvider` - Implementation of IAIProvider for OpenAI API
  - `OpenAIOptions` - Configuration options for OpenAI service
  - `MarkdownStorageOptions` - Configuration options for markdown storage
  - `ServiceCollectionExtensions` - Extension methods for dependency injection
- **Implemented Interfaces**:
  - `IStorageProvider` - Interface for storage operations
  - `IAIProvider` - Interface for AI provider API clients
- **Planned Classes**:
  - `BrainstormingSession` - Manages a brainstorming session with multiple agents

### Documentation

- Keep `specs.md` updated with architectural decisions
- Document AI agent system prompts and their intended roles
- Document markdown file structure and format

## Open Questions

- What specific AI service APIs will be supported initially? (OpenAI is implemented)
- How should we implement the UI for the brainstorming session?
- What visualization features should be included for the brainstorming results?
- What export formats should be supported?
- Are there any authentication or user management requirements?
