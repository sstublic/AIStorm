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
├── .clinerules                 # Custom rules for AI assistant
├── .gitignore                  # Git ignore file
├── .markdownlint.json          # Markdown linting configuration
├── AIStorm.sln                 # Solution file
├── README.md                   # Project readme
├── docs/
│   └── specs.md                # Specifications document
├── src/
│   ├── Core/                   # Core business logic
│   │   ├── Models/             # Domain models
│   │   │   ├── Agent.cs        # Agent model
│   │   │   ├── Session.cs      # Session model
│   │   │   ├── SessionPremise.cs # Session premise model
│   │   │   └── StormMessage.cs # Message model
│   │   └── Services/           # Services
│   │       ├── IAIProvider.cs  # AI provider interface
│   │       ├── ISessionRunnerFactory.cs # Session runner factory interface
│   │       ├── IStorageProvider.cs # Storage provider interface
│   │       ├── MarkdownSerializer.cs # Markdown serialization
│   │       ├── MarkdownStorageProvider.cs # Markdown implementation
│   │       ├── OpenAIProvider.cs # OpenAI implementation
│   │       ├── PromptTools.cs  # Tools for AI prompts
│   │       ├── SessionRunner.cs # Session runner implementation
│   │       ├── SessionRunnerFactory.cs # Session runner factory
│   │       └── Options/        # Configuration options
│   │           ├── MarkdownStorageOptions.cs # Storage options
│   │           └── OpenAIOptions.cs # OpenAI options
│   └── Server/                 # Blazor Server application
│       ├── Pages/              # Razor pages
│       ├── Shared/             # Shared components
│       └── wwwroot/            # Static files
└── test/
    ├── Core.Tests/             # Unit tests for Core project
    │   ├── Services/           # Tests for services
    │   │   ├── MarkdownSerializerTests.cs
    │   │   ├── MarkdownStorageProviderTests.cs
    │   │   └── SessionRunnerTests.cs
    │   └── TestData/           # Test data for unit tests
    │       └── SessionExample/ # Example session for testing
    │           ├── Agents/     # Agent definitions
    │           │   ├── Creative Thinker.md
    │           │   ├── Critical Analyst.md
    │           │   └── Practical Implementer.md
    │           ├── SessionExample.ini.md # Session premise
    │           └── SessionExample.log.md # Conversation log
    └── Core.IntegrationTests/  # Integration tests for Core project
        ├── OpenAITests.cs      # OpenAI integration tests
        ├── Program.cs          # Console app entry point for integration tests
        ├── SessionIntegrationTests.cs # Session integration tests
        └── appsettings.json    # Configuration for integration tests
```

The application creates an `AIStormSessions` directory at runtime to store user sessions.

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

Our approach for AIStorm uses a standardized message format with agent name prefixes:

1. All messages in the conversation history include a prefix with the sender's name in brackets: `[SenderName]: `
2. The OpenAIProvider automatically prepends the agent name to all responses: `[AgentName]: response content`
3. The SessionRunner formats user messages with a Human prefix: `[Human]: user content`
4. System prompts are enhanced with instructions about the agent's identity and message format

Implementation details:

- The system prompt now includes the agent's name and instructions about the conversation format:
  ```
  You are {AgentName}. {Original System Prompt}
  
  You will be provided with the history of the conversation so far with each participant's message prefixed by the name of the speaker in the form `[<SpeakerName>]: `
  
  When responding, DO NOT add the prefix to your response!
  ```

- For mapping to API roles, we use this strategy:
  1. The current agent's previous responses are marked as "assistant" role
  2. All other messages (from human user and other agents) are marked as "user" role
  3. Each message already includes a sender prefix (`[SenderName]: `) in its content

- To handle cases where AI providers may incorrectly add agent prefixes to their responses despite the instructions:
  1. We use the `PromptTools.CleanupResponse` method to remove any `[AgentName]:` prefixes from responses
  2. The cleanup uses regex to detect and remove any leading agent prefixes in the format `[Something]:` 
  3. This ensures that responses don't have duplicate prefixes like `[Agent]: [Agent]: Response`

Example API request format when sending a message to "Agent B":

```json
{
  "messages": [
    { 
      "role": "system", 
      "content": "You are Agent B, a critical analyst in a multi-agent brainstorming session. Your role is to analyze ideas critically and identify potential issues or improvements.\n\nYou will be provided with the history of the conversation so far with each participant's message prefixed by the name of the speaker in the form `[<SpeakerName>]: `\n\nWhen responding, DO NOT add the prefix to your response!"
    },
    { "role": "user", "content": "[Human]: What are some ideas for a weekend project?" },
    { "role": "assistant", "content": "[Agent B]: From an analytical perspective, we should consider time constraints and resource availability..." },
    { "role": "user", "content": "[Agent A]: Here are some creative ideas: 1. Build a herb garden..." },
    { "role": "user", "content": "[Agent C]: From a practical perspective, consider these factors..." },
    { "role": "user", "content": "Premise: This is a brainstorming session about weekend projects.\n\nContinue the conversation based on the above premise and the conversation history." }
  ]
}
```

Benefits of this approach:

- Message formatting is handled consistently at the provider level
- Conversation history maintains a clear format regardless of message source
- Agents receive explicit instructions about their identity and expected format
- The prefixed format in StormMessage objects simplifies display in the UI
- Cleanup of responses prevents duplicate agent prefixes

## Data Storage

### Markdown File Structure

- Root folder: `AIStormSessions`
- One folder per session with a user-descriptive name (e.g., "Example")
- Each session folder contains:
  - An `Agents` subfolder for agent definitions (e.g., `Creative Thinker.md`)
  - A session log file with `.log.md` extension (e.g., `SessionExample.log.md`) for conversation content
  - A session premise file with `.ini.md` extension (e.g., `SessionExample.ini.md`) for initial context

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

An integration test is included that demonstrates a complete session workflow:

- Loading agents and premise from markdown files
- Initializing a SessionRunner with multiple agents via SessionRunnerFactory
- Simulating a conversation with agent responses
- Handling user intervention mid-conversation
- Displaying the full conversation with clear formatting
- Detailed logging for debugging purposes

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

### Integration Tests Logging

- The Core.IntegrationTests project uses NLog for structured logging
- Configured to write logs to file in addition to console output
- Log files are stored in the `logs` directory relative to the output directory with pattern `aistorm-integration-tests-{date}.log`
- Logs include timestamp, log level, logger name, and message with structured data
- All test classes use ILogger<T> for consistent structured logging
- NLog is properly initialized and shut down via the LogManager to ensure all logs are flushed

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
  - `SessionRunner` - Manages conversation flow by relaying messages between agents in sequential rotation. Can initialize from a new premise or continue from an existing session
  - `SessionRunnerFactory` - Creates SessionRunner instances for new sessions or for continuing existing sessions
  - `MarkdownStorageProvider` - Handles reading and writing markdown files
  - `MarkdownSerializer` - Handles serialization and deserialization of markdown documents
  - `MarkdownSegment` - Represents a segment of a markdown document with properties and content
  - `OrderedProperties` - Maintains deterministic property ordering in markdown tags
  - `Tools` - Provides utility functions for handling dates and times
  - `OpenAIProvider` - Implementation of IAIProvider for OpenAI API
  - `OpenAIOptions` - Configuration options for OpenAI service
  - `MarkdownStorageOptions` - Configuration options for markdown storage
  - `ServiceCollectionExtensions` - Extension methods for dependency injection
  - `PromptTools` - Utility class for handling AI prompts and responses, includes methods for cleaning up responses and creating extended system prompts
- **Implemented Interfaces**:
  - `IStorageProvider` - Interface for storage operations
  - `IAIProvider` - Interface for AI provider API clients
  - `ISessionRunnerFactory` - Factory interface for creating SessionRunner instances
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
