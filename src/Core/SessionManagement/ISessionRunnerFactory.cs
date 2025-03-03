namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using System.Collections.Generic;

public interface ISessionRunnerFactory
{
    SessionRunner CreateWithNewSession(IEnumerable<Agent> agents, SessionPremise premise);
    
    SessionRunner CreateWithStoredSession(string sessionId);
}
