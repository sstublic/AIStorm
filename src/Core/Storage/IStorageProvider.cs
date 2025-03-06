namespace AIStorm.Core.Storage;

using AIStorm.Core.Models;
using System.Collections.Generic;

public interface IStorageProvider
{
    Agent LoadAgent(string id);
    void SaveAgent(string id, Agent agent);
    bool DeleteAgent(string id);
    
    Session LoadSession(string id);
    void SaveSession(string id, Session session);
    bool DeleteSession(string id);
    
    IReadOnlyList<Session> GetAllSessions();
    IReadOnlyList<Agent> GetAllAgentTemplates();
    
    bool ValidateId(string id, out string errorMessage);
}
