using System.Collections.Generic;
using Shared.Model;

namespace Indexer;
    public interface IDatabase
    {
        //Get all words with key as the value, and the value as the id 
        Dictionary<string, int> GetAllWords();

        // Get word frequencies - returns dictionary with word name as key and occurrence count as value
        // The list is ordered by occurrence count (descending)
        List<(string word, int id, int count)> GetWordFrequencies();

        // Return the number of documents indexed in the database
        int DocumentCounts { get; }

        void InsertDocument(BEDocument doc);

        // Insert a word in the database with id = [id] and value = [value]
        void InsertWord(int id, string value);

        void InsertAllWords(Dictionary<string, int> words);

        void InsertAllOcc(int docId, ISet<int> wordIds);
    }
