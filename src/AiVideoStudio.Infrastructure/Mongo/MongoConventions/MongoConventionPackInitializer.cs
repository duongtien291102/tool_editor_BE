using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System;
using AiVideoStudio.Domain.Entities;

namespace AiVideoStudio.Infrastructure.Mongo.MongoConventions;

public static class MongoConventionPackInitializer
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public static void Initialize()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(MongoDB.Bson.BsonType.String),
                new IgnoreIfNullConvention(true)
            };

            ConventionRegistry.Register("AiVideoStudioConventions", pack, t => true);

            if (!BsonClassMap.IsClassMapRegistered(typeof(UploadSession)))
            {
                BsonClassMap.RegisterClassMap<UploadSession>(classMap =>
                {
                    classMap.AutoMap();
                    classMap.MapField("_completedChunks").SetElementName("completedChunks");
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Script)))
            {
                BsonClassMap.RegisterClassMap<Script>(classMap =>
                {
                    classMap.AutoMap();
                    classMap.MapField("_scenes").SetElementName("scenes");
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(Scene)))
            {
                BsonClassMap.RegisterClassMap<Scene>(classMap =>
                {
                    classMap.AutoMap();
                    classMap.MapField("_elements").SetElementName("elements");
                });
            }

            try
            {
                BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Utc));
            }
            catch (BsonSerializationException) { }

            _initialized = true;
        }
    }
}
