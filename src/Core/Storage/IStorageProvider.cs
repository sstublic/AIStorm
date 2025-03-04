namespace AIStorm.Core.Storage;

using AIStorm.Core.Models;
using System.Collections.Generic;

public interface IStorageProvider
{
    Agent LoadAgent(string id);
    void SaveAgent(string id, Agent agent);
    
    Session LoadSession(string id);
    void SaveSession(string id, Session session);
    
    IReadOnlyList<Session> GetAllSessions();
    IReadOnlyList<Agent> GetAllAgentTemplates();
    
    bool ValidateId(string id, out string errorMessage);
}
