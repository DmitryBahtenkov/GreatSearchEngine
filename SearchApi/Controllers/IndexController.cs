using System.Linq;
using System.Threading.Tasks;
using Core.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace SearchApi.Controllers
{
    [Route("Index")]
    [ApiController]
    public class IndexController : ControllerBase
    {
        private readonly GetDocumentsCommand _getDocumentsCommand;
        private readonly CreateDbCommand _createDbCommand;
        private readonly CreateIndexCommand _createIndexCommand;
        private readonly AddObjectToIndexCommand _addObjectToIndex;
        private readonly IndexingDocumentsCommand _indexingDocumentsCommand;

        public IndexController()
        {
            _getDocumentsCommand = new GetDocumentsCommand();
            _createDbCommand = new CreateDbCommand();
            _createIndexCommand = new CreateIndexCommand();
            _addObjectToIndex = new AddObjectToIndexCommand();
            _indexingDocumentsCommand = new IndexingDocumentsCommand();
        }
        [HttpGet]
        public async Task<IActionResult> GetDocuments(string dbname, string index)
        {
            var docs = await _getDocumentsCommand.Get(dbname, index);
            var result = docs
                .Select(x => new {Id = x.Id, Value = JsonConvert.SerializeObject(x.Value)});
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> CreateIndex([FromBody] object obj, string dbname, string index)
        {
            var str = obj.ToString();
            _createDbCommand.CreateDb(dbname);
            _createIndexCommand.CreateIndex(dbname,index);
            await _addObjectToIndex.Add(dbname, index, str);
            await _indexingDocumentsCommand.Indexing(dbname, index);
            return Ok();
        }
    }
}