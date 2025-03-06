# AIStorm - System Architecture

This document describes the technical architecture of AIStorm, a multi-agent brainstorming application.

## Related Documentation

- See [Project Overview](project-overview.md) for high-level information about the project

## Component Overview

AIStorm consists of several key components that work together to enable AI-powered brainstorming sessions:

1. **AI System** - Manages communication with AI service providers and handles prompt construction
2. **Data Storage System** - Handles persistence of sessions, agents, and conversation history
3. **Session Management** - Coordinates conversations between multiple AI agents
4. **UI Components** - Reusable Blazor components for consistent user interface elements

## AI System

### Agent Configuration

- Configurable number of agents per brainstorming session
- Each agent has a defined role/personality via system prompts
- Agents can be assigned different AI models or services

### AI Service Integration

- Support for multiple AI service APIs through a provider-based architecture
- Base options class (`AIOptionsBase`) with common configuration:
  - API key provision via user secrets, environment variables, or appsettings.json
  - Configurable list of models available for each provider
- Provider-specific options (e.g., `OpenAIOptions`, `AIMockOptions`) that inherit from the base class
- Interface-based design with `IAIProvider` for common operations
- Provider Manager for dynamic discovery of available providers and models:
  - Uses dependency injection to discover all provider implementations
  - Filters out providers that have no available models
  - Provides a simple way for UI components to get all registered providers with their models
- Multiple providers implemented:
  - OpenAI service with built-in endpoint URL
  - Google Gemini service with built-in endpoint URL
  - Anthropic Claude service with built-in endpoint URL
  - AIMock provider for testing with predefined behaviors:
    - AlwaysThrows model: Always throws an exception (useful for integration tests and error handling testing)
    - AlwaysReturns model: Always returns a predictable response (useful for UI testing and development)
- Configuration structure follows the pattern `AI:{ProviderName}` in appsettings.json:
  ```json
  "AI": {
    "ProviderName": {
      "ApiKey": "",
      "Models": ["model1", "model2", "model3"]
    }
  }
  ```

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
- Deleting agent templates and sessions
- Loading and saving session premises
- Retrieving all available sessions and agent templates

#### Markdown Storage

The markdown implementation in the `AIStorm.Core.Storage.Markdown` namespace includes:

- Parsing and generating markdown files with proper formatting
- Handling file system operations
- Creating the necessary directory structure at initialization
- Scanning file system for all available sessions and agent templates

## UI Components

AIStorm uses a component-based UI architecture with several reusable components that provide consistent styling and behavior throughout the application:

### Core UI Components

1. **AistormCard** - A flexible card component used as the foundation for various content displays:
   - Supports expandable/collapsible content sections
   - Includes title, subtitle, and metadata display areas
   - Provides a standardized container for various content types

2. **MarkdownView** - Renders markdown content in the UI:
   - Uses the Markdig library for markdown processing
   - Supports advanced markdown extensions
   - Renders content as HTML with appropriate styling

3. **AgentCard** - Displays agent information in a consistent format:
   - Shows agent name, service type, and model information
   - Optional color indicators for visual differentiation of agents
   - Expandable system prompt display
   - Conditional edit buttons based on context

4. **ConversationMessage** - Displays individual messages in the conversation:
   - Renders message content with markdown support
   - Shows agent name and timestamp
   - Uses consistent styling with agent-specific color indicators
   - Differentiates between user and AI agent messages

### Styling and Theming

- CSS variables for consistent theming throughout the application
- Agent-specific colors for visual differentiation in conversation view
- Responsive design for different screen sizes
- Bootstrap integration for grid system and basic components
- Custom CSS for specialized components

### Navigation Flow

The application uses a structured navigation flow:

1. **Sessions Overview** (`/sessions`) - The main hub for accessing all content:
   - Lists all available sessions with metadata
   - Provides access to agent templates
   - Entry point for creating new sessions or agents

2. **Session Editor** (`/session-editor[/{SessionId}]`) - For creating or editing sessions:
   - Form for session ID, premise, and agent selection
   - Validation for all inputs
   - Conditionally enables editing based on session state

3. **Agent Editor** (`/agent-editor[/{AgentName}]`) - For creating or editing agent templates:
   - Form for agent name, service type, model, and system prompt
   - Dynamic model selection based on service type
   - Validation for all inputs

4. **Conversation View** (`/conversation/{SessionId}`) - For viewing and participating in sessions:
   - Displays conversation history with agent-specific styling
   - Input area for adding new user messages
   - Real-time updates when agents respond

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

### Management UI Components

#### Agent Template Management

The user interface provides a comprehensive workflow for creating and managing agent templates:

1. **Agent Name Validation** - Ensures agent names are valid for the storage implementation
   - Uses the same `ValidateId` method as session validation to ensure consistent rules
   - Checks for name uniqueness when creating new agents
   - Names cannot be changed after creation (to maintain referential integrity)

2. **Agent Service and Model Selection** - Users can select from available AI service providers and models
   - Dropdown lists for available services (OpenAI, Google Gemini, Anthropic Claude)
   - Service-specific model options
     - OpenAI: gpt-4o, gpt-4o-mini, o1, o1-mini, o3-mini
     - Google Gemini: gemini-2.0-flash, gemini-2.0-flash-lite, gemini-2.0-flash-thinking-exp-01-21
     - Anthropic Claude: claude-3-5-sonnet-latest, claude-3-5-haiku-latest, claude-3-7-sonnet-latest
   - Extensible design for adding more services and models in the future

3. **Agent System Prompt Editing** - Users can define the agent's personality and behavior
   - Multi-line text area for entering detailed system prompts

4. **Agent Editing** - All agent templates can be edited at any time
   - Unlike sessions, there are no restrictions on when agents can be edited
   - Changes to agent templates do not affect existing sessions (since agents are copied)
   - The AgentEditor component provides a form interface for both creating and editing agents

#### Session Management

The user interface provides a comprehensive workflow for creating and managing sessions:

1. **Session ID Validation** - Ensures session IDs are valid for the storage implementation
   - The `IStorageProvider` interface includes a `ValidateId` method to check for valid filenames
   - The `MarkdownStorageProvider` implementation validates IDs against filesystem constraints
   - Meaningful error messages help users understand validation requirements

2. **Agent Selection** - Users can select one or more agents from available templates
   - Agent templates display their name, AI service, model, and system prompt
   - Selection is enforced (at least one agent must be selected)
   - Users are informed that agent templates are copied into the session (no live relationship)

3. **Session Premise** - Users define the context and goals for the brainstorming session
   - The premise is saved as part of the session metadata
   - The premise is included in system prompts to provide context to agents

4. **Session Editing** - Users can edit sessions under specific conditions
   - Sessions can only be edited if they have no conversation messages
   - When editing is not possible, users can view session details in read-only mode
   - The SessionEditor component handles both creation and editing functionality

#### Sessions Overview

The SessionsOverview page serves as the central hub for managing both sessions and agent templates:

1. **Session Management**
   - View all available sessions with summary information (name, creation date, agent count, message count)
   - Quick access to open, edit, or view session details
   - Visual indication of which sessions can be edited (based on message count)

2. **Agent Template Management**
   - View all available agent templates with details and expandable system prompts
   - Quick access to edit any agent template
   - Create new agent templates

## Logging

The application uses a structured logging approach with clearly defined log levels:

- **Information Level**: Used only for top-level operations
  - Service initialization and startup
  - Session creation/loading (top-level entry points)
  - User-initiated actions completion (creating/updating agents and sessions)
  - Critical path operations

- **Debug Level**: Used for implementation details and secondary operations
  - Function entry/exit logging
  - Data loading/processing details
  - Session and message handling details
  - UI component lifecyle events
  - File operations details

- **Trace Level**: Used for highly detailed and frequently executed operations
  - Provider and model details
  - Detailed message content
  - Request/response payloads
  - Low-level implementation details

Log files can be found in the application's log directory:
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
