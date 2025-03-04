# AIStorm - System Architecture

This document describes the technical architecture of AIStorm, a multi-agent brainstorming application.

## Related Documentation

- See [Project Overview](project-overview.md) for high-level information about the project

## Component Overview

AIStorm consists of several key components that work together to enable AI-powered brainstorming sessions:

1. **AI System** - Manages communication with AI service providers and handles prompt construction
2. **Data Storage System** - Handles persistence of sessions, agents, and conversation history
3. **Session Management** - Coordinates conversations between multiple AI agents

## AI System

### Agent Configuration

- Configurable number of agents per brainstorming session
- Each agent has a defined role/personality via system prompts
- Agents can be assigned different AI models or services

### AI Service Integration

- Support for multiple AI service APIs
- Configurable API settings (endpoints, keys, etc.)
- Interface-based design with `IAIProvider` for common operations
- OpenAI service implemented as the first provider
- Configuration options for OpenAI integration:
  - API key provision via user secrets, environment variables, or appsettings.json
  - Configurable base URL for custom endpoints

### Prompt Building System

AIStorm uses a dedicated prompt building system to ensure consistency across different AI providers:

- System prompts include the agent's name, prompt content, and session premise
- Conversation history is mapped to appropriate roles based on the current agent
- Agent name prefixes are managed consistently
- Response cleanup prevents duplicate prefixes

### Multi-Agent Conversation Format

Our approach for handling multi-agent conversations uses a standardized format with agent name prefixes:

1. All messages include sender name in markdown header format: `## [SenderName]:\n\n message content`
2. System prompts are enhanced with instructions about agent identity and expected format
3. Agent message history is properly mapped to provider-specific formats
4. SessionRunner is responsible for formatting messages with the proper markdown header

System prompt template:

```
You are {AgentName}. {Original System Prompt}

Context: {Premise Content}

You will be provided with conversation history with each message prefixed by the speaker's name in brackets.
When responding, DO NOT add the prefix to your response!
```

Example API request structure (simplified):

```json
{
  "messages": [
    { "role": "system", "content": "You are Agent B, a critical analyst..." },
    { "role": "user", "content": "[Human]: What are some ideas...?" },
    { "role": "assistant", "content": "[Agent B]: From an analytical perspective..." },
    { "role": "user", "content": "[Agent A]: Here are some creative ideas..." }
  ]
}
```

## Data Storage System

### Agent Template Format

Agent templates are stored as markdown files with the agent name as the filename. XML tags contain metadata, and the content functions as the system prompt:

```markdown
<aistorm type="OpenAI" model="gpt-4o" />

## Creative Thinker

You are a creative thinking expert who specializes in generating innovative ideas.
Always think outside the box and challenge conventional wisdom.
```

### Session Format

Sessions are stored as markdown files with structured sections for metadata, premise, agents, and messages:

```markdown
<aistorm type="session" created="2025-03-01T15:00:00" description="Brainstorming session" />

# Session title

<aistorm type="premise" />
## Session Premise
Premise content here...

<aistorm type="agent" name="Agent Name" service="OpenAI" model="gpt-4o" />
## Agent Name
Agent system prompt here...

<aistorm type="message" from="user" timestamp="2025-03-01T15:01:00" />
## [user]:
User message content...
```

### Storage Implementation

#### Provider Interface

The storage provider interface defines operations for:

- Loading and saving agent templates
- Loading and saving complete sessions
- Loading and saving session premises
- Retrieving all available sessions and agent templates

#### Markdown Storage

The markdown implementation in the `AIStorm.Core.Storage.Markdown` namespace includes:

- Parsing and generating markdown files with proper formatting
- Handling file system operations
- Creating the necessary directory structure at initialization
- Scanning file system for all available sessions and agent templates

## Session Management

The Session Management system coordinates conversations between multiple AI agents:

### Session Runner

- `SessionRunner` - Runs a session with a given set of AI agents, manages turn-taking, and handles message exchange
  - Takes a pre-constructed `Session` object as input
  - Determines which agent responds next based on conversation history
  - Handles sending prompts to AI providers and processing responses
  - Manages adding user messages to the conversation
  - Maintains the state of the conversation

### Session Runner Factory

- `SessionRunnerFactory` - Creates `SessionRunner` instances with different initialization strategies:
  - `CreateWithNewSession` - Creates a new session with the given agents and premise
  - `CreateWithStoredSession` - Loads an existing session from storage by ID

This design separates the responsibility of Session creation from Session running, making the code more maintainable and testable.

### Session Creation

The user interface provides a workflow for creating new sessions:

1. **Session ID Validation** - Ensures session IDs are valid for the storage implementation
   - The `IStorageProvider` interface includes a `ValidateId` method to check for valid filenames
   - The `MarkdownStorageProvider` implementation validates IDs against filesystem constraints
   - Meaningful error messages help users understand validation requirements

2. **Agent Selection** - Users can select one or more agents from available templates
   - Agent templates display their name, AI service, model, and system prompt
   - Selection is enforced (at least one agent must be selected)

3. **Session Premise** - Users define the context and goals for the brainstorming session
   - The premise is saved as part of the session metadata
   - The premise is included in system prompts to provide context to agents

## Logging

- Different log levels for appropriate information detail
- API requests and responses logging
- Error details and operation information
- Log files can be found in the application's log directory
  - Default location: `logs/` relative to the application's output directory
  - File naming pattern: `aistorm-{date}.log`

### Integration Tests Logging

- Integration tests use NLog for structured logging
- Logs are written to both console and file output
- Log files are stored in the `logs` directory with pattern `aistorm-integration-tests-{date}.log`
- Logs include timestamp, log level, logger name, and structured data

## Testing

The system includes comprehensive tests for:

- Storage operations (loading/saving agents and sessions)
- Serialization/deserialization
- Complete session workflows
- AI provider integration

### Integration Testing

The Core.IntegrationTests project provides end-to-end testing:

- Tests for OpenAI API integration
- Session flow integration tests
- Configuration via appsettings.json
- Console application entry point for manual testing
- Tests realistic scenarios with multiple agent interactions
