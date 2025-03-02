namespace AIStorm.Core.Services;

using AIStorm.Core.Models;

public interface IStorageProvider
{
    Agent LoadAgent(string id);
    void SaveAgent(string id, Agent agent);
    
    Session LoadSession(string id);
    void SaveSession(string id, Session session);
}
