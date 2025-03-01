# AIStorm - Specifications

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
│   └── specs.md       # Specifications document
└── src/
    ├── Server/        # Blazor Server application (includes UI logic)
    └── Core/          # Core business logic (includes storage logic)
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
- One folder per conversation
- Markdown files to store the conversation content
- Potential metadata files for session configuration

### Storage Implementation
- File system operations for reading/writing markdown files
- Indexing mechanism for searching across conversations
- Backup and versioning considerations

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
- **Example Classes**:
  - `ConversationManager` - Manages brainstorming conversations
  - `AgentConfiguration` - Configures AI agent properties
  - `MarkdownStorage` - Handles markdown file operations
- **Example Interfaces**:
  - `IStorageService` - Interface for storage operations
  - `IAgentFactory` - Interface for creating AI agents
  - `IAIServiceClient` - Interface for AI service API clients

### Documentation
- Keep `specs.md` updated with architectural decisions
- Document AI agent system prompts and their intended roles
- Document markdown file structure and format

## Open Questions

- What specific AI service APIs will be supported initially?
- What is the detailed structure of the markdown files?
- Are there any specific visualization requirements for the brainstorming results?
- What export formats should be supported?
- Are there any authentication or user management requirements?
