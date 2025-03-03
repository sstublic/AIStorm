# Session Redesign Plan

## Implementation Checklist

- [x] Update `Session` class to include agents and premise
- [x] Create new folder structure (`AgentTemplates` and `Sessions` directories)
- [x] Update `MarkdownStorageProvider` to save/load embedded agents and premise
- [x] Create example session file with the new format
- [x] Update `IStorageProvider` interface (simplified `CreateFromExistingSession`)
- [x] Update `SessionRunner` to use embedded agents
- [x] Update `SessionRunnerFactory` to create sessions with templates
- [x] Update unit tests for storage provider
- [x] Update unit tests for session runner
- [x] Fix bugs in SessionRunner with embedded agents (index out of range)
- [x] Clean up old file structure and unnecessary legacy files
- [x] Add backward compatibility for loading legacy files
- [x] Ensure specs.md is up to date with all the changes
- [x] Update UI components to display embedded agents
- [x] Update integration tests

## Current Implementation

Currently, the AIStorm system maintains a separation between different entities:

- **Sessions**: Only store conversation messages (in `.log.md` files)
- **Agents**: Defined in separate files with name, AI service type, model, and system prompt
- **Premises**: Stored in separate files with session context (in `.ini.md` files)

This approach has the following characteristics:

- Sessions are lightweight but incomplete without their associated agents and premise
- To continue a conversation, the system needs to load multiple files (session, premise, and all agents)
- Changes to agent definitions affect all sessions using them
- Sessions, agents, and premises are stored in separate files with different extensions (`.log.md`, agent files, `.ini.md`)
- The current folder structure looks like this:
  ```
  StorageRoot/
  ├── SessionName/
  │   ├── Agents/
  │   │   ├── Agent1.md
  │   │   ├── Agent2.md
  │   │   └── ...
  │   ├── SessionName.log.md     # Contains messages
  │   └── SessionName.ini.md     # Contains premise
  ```

## Redesign Goals

The primary goal is to make Sessions self-contained, logically complete entities:

1. Sessions should embed copies of the agents and premise used
2. A session should be continuable by loading just the session file
3. Changes to agent templates should not affect existing sessions
4. Maintain the ability to reuse agent definitions as templates
5. Reorganize storage structure to better reflect the template vs. instance distinction

## Proposed Changes

### Storage Structure

We will reorganize the storage structure to separate agent templates from session instances:

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
- Self-contained session files
- Consistent naming convention
- Use of `.session.md` extension makes the file type clear

### Model Changes

#### Agent Implementation

- Keep using a single `Agent` class for both templates and session instances
- No special immutability handling needed as serialization provides deep cloning by default
- Agents in sessions will be exact copies of agent templates

#### Session Model Expansion

Update the `Session` class to include:

```csharp
public class Session
{
    public string Id { get; set; }
    public DateTime Created { get; set; }
    public string Description { get; set; }
    public List<StormMessage> Messages { get; set; }
    
    // New properties
    public List<Agent> Agents { get; set; }
    public string Premise { get; set; }
    
    public Session(string id, DateTime created, string description, string premise)
    {
        Id = id;
        Created = created;
        Description = description;
        Premise = premise;
        Messages = new List<StormMessage>();
        Agents = new List<Agent>();
    }
}
```

#### SessionPremise Integration

- Embed premise content directly in the `Session`
- Keep `SessionPremise` as an internal implementation detail
- Clean redesign with no legacy code

### Storage Format Changes

The Markdown format for sessions will be extended to include sections for embedded agents and premise:

```markdown
<aistorm type="session" created="2025-03-01T15:00:00" description="Brainstorming session for new product ideas" />

# Brainstorming Session: New Product Ideas

<aistorm type="premise" />

This is an example session premise for a brainstorming session about weekend projects.

<aistorm type="agent" name="Creative Thinker" service="OpenAI" model="gpt-4" />

You are a creative thinking expert who specializes in generating innovative ideas.
Always think outside the box and challenge conventional wisdom.
When presented with a problem, explore multiple angles and perspectives.
Provide ideas that are both creative and practical.

<aistorm type="agent" name="Critical Analyst" service="OpenAI" model="gpt-4" />

You are a critical analyst who evaluates ideas rigorously.
Your role is to identify potential issues, flaws, or weaknesses in proposals.
Consider practical constraints, resource requirements, and potential obstacles.
Suggest improvements and alternatives when pointing out problems.

<aistorm type="message" from="user" timestamp="2025-03-01T15:01:00" />

## [user]:

Let's brainstorm ideas for a new mobile app that helps people connect with local businesses.

<aistorm type="message" from="Creative Thinker" timestamp="2025-03-01T15:01:30" />

## [Creative Thinker]:

Here are some innovative ideas for a local business connection app...
```

### Implementation Approach

- **Incremental Development**: Implement changes in atomic steps that keep the solution buildable and working
- **No Interface Changes**: Keep the existing `IStorageProvider` interface; files serve as templates that become `Agent` objects when loaded
- **Reuse Existing Parsers**: Use existing segment parsers and syntax for embedded agents and premise
- **Extraction in SessionRunner**: SessionRunner will extract agents from the session at initialization

### Implementation Steps

#### 1. Model Updates

First, make necessary changes to the core models:
- Update the `Session` class to include agents and premise
- Update the constructors and related methods

#### 2. Storage Provider Updates

Update the `MarkdownStorageProvider` class to:

- Parse and extract embedded agents and premise when loading sessions
- Include embedded agents and premise when saving sessions

#### 3. Markdown Serialization Updates

Update the `MarkdownSerializer` class to handle:

- Serialization of embedded agents and premise in session files
- Deserialization of embedded agents and premise from session files

#### 4. UI Updates

Update the UI components to:

- Display the agents embedded in a session
- Use card view for selecting agent templates when creating a new session
- Templates will be used exactly as defined, without modification at session creation
- No special visual distinction needed for agents in sessions, as context makes this clear

## Testing Strategy

1. Create unit tests for the updated models and serialization logic
2. Add integration tests that verify the complete flow with embedded agents and premise
3. Transform existing test data to follow the new format
4. Test files should reflect the new folder structure with AgentTemplates and Sessions
5. Ensure thorough coverage of all aspects:
   - Session loading/saving with embedded agents
   - Template selection and instantiation
   - Session runner using embedded agents

## Test Updates

Several test classes will need to be updated:

1. **MarkdownStorageProviderTests.cs**:
   - Update agent loading/saving tests to use the AgentTemplates path
   - Add tests for the new folder structure

2. **MarkdownStorageProviderSessionTests.cs**:
   - Update tests to handle embedded agents and premise
   - Test the new self-contained session format

3. **SessionRunnerTests.cs**:
   - Update to use embedded agents rather than loading them separately

4. **Integration Tests**:
   - Update to demonstrate the complete workflow with embedded agents

## Example File Creation

Create new examples in the test directories:

1. **Test Data Updates**:
   - Create examples of the new self-contained session format
   - Add examples using the new folder structure
   - Completely convert all test data to the new format

## Implementation Timeline

The implementation should follow this sequence:

1. Update `Session` class to include agents and premise
2. Implement new folder structure
3. Extend MarkdownSerializer to handle embedded agents and premise
4. Update MarkdownStorageProvider
5. Create new test data examples
6. Update tests
7. Update UI components

## Future Considerations

1. **Agent Versioning**: Consider including version information for agents to track changes
2. **Premise Templates**: Similar to agent templates, premise templates could be reusable
3. **Export/Import**: Allow exporting sessions with their embedded data for sharing
4. **UI for Template Management**: Specialized UI for managing agent templates separately from sessions
