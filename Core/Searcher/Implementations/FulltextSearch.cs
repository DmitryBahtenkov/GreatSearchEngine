﻿using Core.Analyzer;
using Core.Models;
using Core.Searcher.Interfaces;
using Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Searcher.Implementations
{
    public class FullTextSearch : ISearch
    {

        private readonly IndexingOperations _indexingOperations;
        private readonly GetOperations _getOperations;
        private readonly Analyzer.Analyzer _analyzer;

        public FullTextSearch()
        {
            _indexingOperations = new IndexingOperations();
            _analyzer = new Analyzer.Analyzer(new Tokenizer(), new Normalizer());
            _getOperations = new GetOperations();
        }

        public async Task<List<DocumentModel>> ExecuteSearch(IndexModel indexModel, BaseSearchModel searchModel)
        {
            var all = new List<List<Guid>>();
            var ids = new List<Guid>();
            var tokens = await _analyzer.Anal(searchModel.Term);
            var docs = await _getOperations.GetDocuments(indexModel);
            var data = await _indexingOperations.GetIndexesAllKeys(indexModel, searchModel.Key);

            foreach (var dict in data)
            {
                if (dict.Keys.Intersect(tokens).Count() == tokens.Count)
                    all.AddRange(dict.Values);
            }

            if (all.Count == 1)
            {
                ids = all[0];
            }
            else if (all.Count == 2)
            {
                ids.AddRange(all[0].Intersect(all[1]));
            }
            else
            {
                for (var i = 0; i < all.Count - 1; i++)
                {
                    var current = all[i];
                    var intersect = current.Intersect(all[i + 1]).ToList();
                    foreach (var item in intersect.Where(item => !ids.Contains(item)))
                    {
                        ids.Add(item);
                    }
                }
            }

            var result = (from doc in docs from id in ids where doc.Id == id select doc).Distinct().ToList();
            return result;
        }
    }
}