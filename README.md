# AIStorm

AIStorm is a multi-agent brainstorming application that enables interactive conversations between multiple AI agents with different roles and personalities. This Blazor Server application facilitates creative idea generation, problem-solving, and collaborative thinking through AI-powered conversations.

## Developer Note

**This project was created as a test and an exercise in AI assisted coding. Entire project and documentation (> 99% of it) was written using Cline/Claude 3.7. This includes this README file as well, except this paragraph. It worked well, but needed constant direction and strict supervision.**

## Overview

AIStorm allows you to create brainstorming sessions with multiple AI agents as participants, each with customized roles and personalities. The application uses a markdown-based storage system to preserve conversations in a human-readable format.

### Key Features

- Configure multiple AI agents with different roles and personalities in a single session
- Support for multiple AI service providers (OpenAI, Google Gemini, Anthropic Claude)
- Color-coded agents for visual differentiation in conversation view
- Markdown-based storage for easy reading and sharing of sessions
- Rich markdown rendering for message content
- Create reusable agent templates with customized system prompts
- Clean, intuitive user interface for session management

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet) or later
- API keys for one or more supported AI service providers:
  - [OpenAI API key](https://platform.openai.com/api-keys)
  - [Google Gemini API key](https://ai.google.dev/tutorials/setup)
  - [Anthropic API key](https://console.anthropic.com/settings/keys)

### Installation

1. Create a folder for the project
2. Inside the newly created folder, clone the repository:
   ```bash
   git clone https://github.com/sstublic/AIStorm.git .
   ```

3. Build and run the application:
   ```bash
   dotnet build
   cd src/Server
   dotnet run
   ```

4. Open your browser and navigate to `https://localhost:5001`

### Configuration

The application requires API keys to communicate with AI service providers. You can configure these in one of three ways:

1. **Create an `appsettings.local.json` file** (recommended for development):
   
   Create this file in the `src/Server` directory with the following structure:
   ```json
   {
     "AI": {
       "OpenAI": {
         "ApiKey": "your-openai-api-key"
       },
       "Gemini": {
         "ApiKey": "your-gemini-api-key"
       },
       "Anthropic": {
         "ApiKey": "your-anthropic-api-key"
       }
     }
   }
   ```
   This file is excluded from git (via .gitignore) to avoid committing API keys.

2. **User Secrets** (Development environment only):
   ```bash
   cd src/Server
   dotnet user-secrets set "AI:OpenAI:ApiKey" "your-openai-api-key"
   dotnet user-secrets set "AI:Gemini:ApiKey" "your-gemini-api-key"
   dotnet user-secrets set "AI:Anthropic:ApiKey" "your-anthropic-api-key"
   ```

3. **Environment Variables** - System environment variables can be set according to .NET configuration standards

### Sample Data

The application includes a sample data folder (`src/Server/SampleDataStorage`) with:
- Predefined agent templates (e.g., Creative Thinker, Critical Analyst)
- Example sessions

This provides users with examples right out of the box, without requiring manual setup.

## Usage Example

### Creating a New Brainstorming Session

1. Navigate to the Sessions Overview page
2. Click "Create New Session"
3. Enter a session ID (will be used as the filename)
4. Write a premise that defines the session's context and goals
5. Select one or more agent templates to participate in the session
6. Click "Create Session"

### Starting a Brainstorming Session

1. From the Sessions Overview, click on the session you created
2. Enter your initial message in the input area at the bottom
3. Submit your message to start the conversation
4. AI agents will respond based on their roles and personalities
5. Continue the conversation by submitting additional messages

The session will be automatically saved to the storage location specified in your configuration.

## Documentation

For more detailed information about the project, refer to:

- [Project Overview](docs/project-overview.md) - High-level description of the project's purpose, goals, and features
- [System Architecture](docs/system-architecture.md) - Technical details about the implementation, components, and design patterns

## License

(License information to be determined)
