namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using System.Collections.Generic;

public interface ISessionRunnerFactory
{
    SessionRunner Create(IEnumerable<Agent> agents, SessionPremise premise);
    
    SessionRunner CreateFromExistingSession(string sessionId);
}
