using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;

namespace Indexer;
    public class App
    {
        public void Run()
        {
            Console.WriteLine("Indexer stores words with original casing.");
            Console.WriteLine("Re-run indexing after this update to enable true case-sensitive search.\n");

            IDatabase db = GetDatabase();
            Crawler crawler = new Crawler(db);

            var root = new DirectoryInfo(Config.FOLDER);

            DateTime start = DateTime.Now;

            crawler.IndexFilesIn(root, new List<string> { ".txt"});        

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            var wordFrequencies = db.GetWordFrequencies();

            Console.WriteLine($"Indexed {db.DocumentCounts} documents");
            Console.WriteLine($"Number of different words: {wordFrequencies.Count}");
            
            // Calculate total word occurrences
            long totalOccurrences = 0;
            foreach (var item in wordFrequencies)
            {
                totalOccurrences += item.count;
            }
            Console.WriteLine($"Total word occurrences indexed: {totalOccurrences}");
            
            Console.Write("How many words do you want to see? ");
            string input = Console.ReadLine();
            if (int.TryParse(input, out int count))
            {
                count = Math.Min(count, wordFrequencies.Count);
                Console.WriteLine($"\nShowing top {count} words by frequency:");
                foreach (var item in wordFrequencies.Take(count))
                {
                    Console.WriteLine($"<{item.word}, {item.id}> - {item.count}");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. No words displayed.");
            }
        }

        private IDatabase GetDatabase()
        {
            Console.Write("Use SQLite (1) or Postgres (2) database?");
            string input = Console.ReadLine();
            if (input.Equals("1"))
                return new DatabaseSqlite();
            else if (input.Equals("2"))
                return new DatabasePostgres();
            Console.WriteLine("Wrong input - try again...");
            return GetDatabase();
        }
    }
