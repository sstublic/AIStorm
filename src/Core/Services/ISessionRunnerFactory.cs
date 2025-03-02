namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using System.Collections.Generic;

public interface ISessionRunnerFactory
{
    SessionRunner Create(List<Agent> agents, SessionPremise premise);
    
    SessionRunner CreateFromExistingSession(string sessionId, List<Agent> agents, SessionPremise premise);
}
