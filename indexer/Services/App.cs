using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StackExchange.Redis;
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
            if (!root.Exists)
            {
                throw new DirectoryNotFoundException($"Indexer folder does not exist: {root.FullName}");
            }

            DateTime start = DateTime.Now;

            crawler.IndexFilesIn(root, Config.EXTENSIONS);

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            var wordFrequencies = db.GetWordFrequencies();

            Console.WriteLine($"Indexed {db.DocumentCounts} documents");
            Console.WriteLine($"Number of different words: {wordFrequencies.Count}");
            ClearRedisCacheIfConfigured();
            
            // Calculate total word occurrences
            long totalOccurrences = 0;
            foreach (var item in wordFrequencies)
            {
                totalOccurrences += item.count;
            }
            Console.WriteLine($"Total word occurrences indexed: {totalOccurrences}");
            
            var count = Config.TOP_WORDS;
            if (!Config.NON_INTERACTIVE)
            {
                Console.Write("How many words do you want to see? ");
                var input = Console.ReadLine();
                int.TryParse(input, out count);
            }

            if (count > 0)
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
                Console.WriteLine("No top words requested.");
            }
        }

        private IDatabase GetDatabase()
        {
            if (Config.DATABASE.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return new DatabaseSqlite();
            }

            if (Config.DATABASE.Equals("postgres", StringComparison.OrdinalIgnoreCase))
            {
                return new DatabasePostgres();
            }

            Console.Write("Use SQLite (1) or Postgres (2) database?");
            string input = Console.ReadLine() ?? string.Empty;
            if (input.Equals("1"))
                return new DatabaseSqlite();
            else if (input.Equals("2"))
                return new DatabasePostgres();
            Console.WriteLine("Wrong input - try again...");
            return GetDatabase();
        }

        private void ClearRedisCacheIfConfigured()
        {
            if (string.IsNullOrWhiteSpace(Config.REDIS_CONNECTION_STRING))
            {
                Console.WriteLine("No Redis connection string configured. Cache invalidation skipped.");
                return;
            }

            try
            {
                using var redis = ConnectionMultiplexer.Connect(Config.REDIS_CONNECTION_STRING);
                var endpoints = redis.GetEndPoints();
                if (endpoints.Length == 0)
                {
                    Console.WriteLine("Redis has no endpoints. Cache invalidation skipped.");
                    return;
                }

                foreach (var endpoint in endpoints)
                {
                    redis.GetServer(endpoint).FlushDatabase();
                }

                Console.WriteLine("Redis cache flushed after indexing.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis cache invalidation failed: {ex.Message}");
            }
        }
    }
