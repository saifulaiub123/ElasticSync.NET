using Nest;
using ElasticSync.Models;
using System.Threading.Tasks;
using System;

namespace ElasticSync.Services;

public class ElasticIndexProvisioner
{
    private readonly ElasticClient _client;
    private readonly ElasticSyncOptions _options;

    public ElasticIndexProvisioner(ElasticClient client, ElasticSyncOptions options)
    {
        _client = client;
        _options = options;
    }

    public async Task EnsureIndicesExistAsync()
    {
        foreach (var entity in _options.Entities)
        {
            var indexName = entity.IndexVersion is not null ? $"{entity.IndexName}-{entity.IndexVersion}" : entity.IndexName;

            var result = await _client.Indices.ExistsAsync(indexName);
            if (!result.ApiCall.Success)
                throw new Exception(result.ApiCall.OriginalException.ToString());

            if (!result.Exists)
                Console.WriteLine($"Elastic Search Index - {indexName} - not exist");
            
            
            //var createResponse = await _client.Indices.CreateAsync(indexName, c =>
            //{
            //    var map = c.Map<object>(m =>
            //    {
            //        if (entity.CustomMapping != null)
            //        {
            //            var desc = new TypeMappingDescriptor<object>();
            //            entity.CustomMapping(desc); // apply config
            //            return desc; // return mapping
            //        }
            //        else
            //        {
            //            return m.AutoMap(entity.EntityType);
            //        }
            //    });

            //    return map;
            //});

            //if (!createResponse.IsValid)
            //    throw new Exception($"Failed to create index {indexName}: {createResponse.DebugInformation}");

            //if (entity.IndexVersion != null)
            //{
            //    await _client.Indices.PutAliasAsync(indexName, baseIndex);
            //}
        }
    }
}