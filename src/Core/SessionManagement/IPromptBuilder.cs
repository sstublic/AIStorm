namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using System.Collections.Generic;

public interface IPromptBuilder
{
    PromptMessage[] BuildPrompt(Agent agent, SessionPremise premise, List<StormMessage> history);
}
