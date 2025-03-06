namespace AIStorm.Core.SessionManagement;

using AIStorm.Core.Models;
using AIStorm.Core.AI;
using AIStorm.Core.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

public class SessionRunnerFactory : ISessionRunnerFactory
{
    private readonly AIProviderManager providerManager;
    private readonly ILoggerFactory loggerFactory;
    private readonly IStorageProvider storageProvider;

    public SessionRunnerFactory(AIProviderManager providerManager, ILoggerFactory loggerFactory, IStorageProvider storageProvider)
    {
        this.providerManager = providerManager;
        this.loggerFactory = loggerFactory;
        this.storageProvider = storageProvider;
    }

    public SessionRunner CreateWithNewSession(IEnumerable<Agent> agents, SessionPremise premise)
    {
        var logger = loggerFactory.CreateLogger<SessionRunner>();
        
        var session = new Session(
            id: premise.Id,
            created: DateTime.UtcNow,
            premise: premise,
            agents: agents
        );
        
        return new SessionRunner(session, providerManager, logger);
    }
    
    public SessionRunner CreateWithStoredSession(string sessionId)
    {
        var logger = loggerFactory.CreateLogger<SessionRunner>();
        var existingSession = storageProvider.LoadSession(sessionId);
        return new SessionRunner(existingSession, providerManager, logger);
    }
}
