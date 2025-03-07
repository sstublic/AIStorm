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
- Overview of all sessions and agent templates
- Color-coded agents for visual differentiation in the conversation view
- Markdown rendering for rich message content

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

## UI Components

AIStorm uses a component-based UI architecture with reusable components for consistency:

### Core Components

- **AistormCard** - A flexible card component serving as the foundation for various content displays
  - Supports three content display states: Collapsed, Preview, and Expanded
  - Configurable toggle states via the `AvailableToggleStates` parameter
  - AgentCard toggles between Preview and Expanded (default behavior)
  - SessionCard toggles between Collapsed and Expanded
- **MarkdownView** - Renders markdown content with support for advanced markdown extensions
- **AgentCard** - Displays agent information with optional color indicators and expandable content
- **ConversationMessage** - Renders conversation messages with markdown support and agent styling

### Pages

- **SessionsOverview** - Main hub for accessing sessions and agent templates
- **AgentEditor** - For creating and editing agent templates
- **SessionEditor** - For creating and editing sessions
- **Conversation** - For viewing and participating in brainstorming sessions

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

1. Create and manage agent templates
   - Create new agent templates with name, service type, model, and system prompt
   - Edit existing agent templates at any time

2. Create a new brainstorming session
   - Enter a valid session ID (which will be used as the filename)
   - Write a premise that defines the session's context and goals
   - Select one or more agent templates to participate in the session
   - Agent templates are copied into the session (changes to templates won't affect existing sessions)

3. Start the brainstorming session with the configured agents
   - Interact with the agents in the conversation interface
   - Messages are styled with agent-specific colors for visual differentiation
   - Markdown content is rendered for rich message formatting
   - Save/export the conversation (happens automatically)

4. Manage sessions
   - Review past sessions and available agent templates in the sessions overview
   - Edit sessions (only possible for sessions with no conversation messages)
   - View session details for sessions with existing conversations

## Navigation Flow

The application uses a structured navigation flow:

1. **Sessions Overview** (`/sessions`) - The main entry point that provides access to:
   - Available sessions with their details
   - Agent templates with expandable details
   - Creation of new sessions and agent templates

2. **Agent Editor** (`/agent-editor[/{AgentName}]`) - For creating or editing agent templates:
   - Form for agent name, service type, model, and system prompt
   - Dynamic model selection based on selected service type
   - Input validation with clear error messages

3. **Session Editor** (`/session-editor[/{SessionId}]`) - For creating or editing sessions:
   - Form for session ID, premise, and agent template selection
   - Input validation for all fields
   - Restriction on editing sessions with existing messages

4. **Conversation View** (`/conversation/{SessionId}`) - For interacting with sessions:
   - Display of all messages with agent styling
   - Input area for adding user messages
   - Automatic updating when agents respond

## Project-Specific Conventions

### Naming and Organization

- **Namespaces**: `AIStorm.{Module}` (e.g., `AIStorm.Core`, `AIStorm.Server`)
- **Documentation**:
  - Keep documentation updated with architectural decisions
  - Document AI agent system prompts and their intended roles
  - Document markdown file structure and format

## Open Questions

- What specific AI service APIs will be supported initially? (OpenAI, Google Gemini, and Anthropic Claude are implemented)
- How should we implement the UI for the brainstorming session?
- What visualization features should be included for the brainstorming results?
- What export formats should be supported?
- Are there any authentication or user management requirements?

## Related Documentation

- See [System Architecture](system-architecture.md) for technical implementation details

## Useful info

Cline data is at `%APPDATA%\Code\User\globalStorage\saoudrizwan.claude-dev\tasks`.
