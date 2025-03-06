namespace AIStorm.Core.AI;

using System.Collections.Generic;

public abstract class AIOptionsBase
{
    public required string ApiKey { get; set; }
    public required IReadOnlyList<string> Models { get; set; }
}
