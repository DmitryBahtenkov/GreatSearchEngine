using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Models;
using Newtonsoft.Json.Linq;

namespace Core.Storage
{
    public class AddObjectToIndexCommand
    {
        private readonly GetIdsCommand _getIdsCommand;

        public AddObjectToIndexCommand()
        {
            _getIdsCommand = new GetIdsCommand();
        }

        public async Task Add(string dbName, string indexName, string raw)
        {
            var path = $"{AppDomain.CurrentDomain.BaseDirectory}data/{dbName}/{indexName}";
            var ids = await _getIdsCommand.GetIds(dbName, indexName);
            var id = 0;
            if (ids.Any())
            {
                id = ids.Count;
            }
            
            var doc = new DocumentModel
            {
                Id = id,
                Value = JObject.Parse(raw)
            };

            path += $"/{id}.json";
            
            var res = File.Create(path);
            res.Close();
            
            await WriteJsonFileCommand.WriteFile(path, doc);
        }
    }
}