namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using System.Collections.Generic;

public interface ISessionRunnerFactory
{
    SessionRunner Create(List<Agent> agents, SessionPremise premise);
    
    SessionRunner CreateFromExistingSession(string sessionId);
}
