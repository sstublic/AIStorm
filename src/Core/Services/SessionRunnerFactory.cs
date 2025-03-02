namespace AIStorm.Core.Services;

using AIStorm.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

public class SessionRunnerFactory : ISessionRunnerFactory
{
    private readonly IAIProvider aiProvider;
    private readonly ILoggerFactory loggerFactory;

    public SessionRunnerFactory(IAIProvider aiProvider, ILoggerFactory loggerFactory)
    {
        this.aiProvider = aiProvider;
        this.loggerFactory = loggerFactory;
    }

    public SessionRunner Create(List<Agent> agents, SessionPremise premise)
    {
        var logger = loggerFactory.CreateLogger<SessionRunner>();
        return new SessionRunner(agents, premise, aiProvider, logger);
    }
}
