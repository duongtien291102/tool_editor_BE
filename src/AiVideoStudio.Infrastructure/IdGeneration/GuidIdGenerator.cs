using AiVideoStudio.Shared.Interfaces;
using System;

namespace AiVideoStudio.Infrastructure.IdGeneration;

public class GuidIdGenerator : IIdGenerator
{
    public string GenerateId()
    {
        return Guid.NewGuid().ToString();
    }
}
