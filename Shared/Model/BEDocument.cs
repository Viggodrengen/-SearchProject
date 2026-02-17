using System;
namespace Shared.Model;
    public class BEDocument
    {
        public int Id { get; set; }

        public string Url { get; set; } = string.Empty;

        public DateTime IdxTime { get; set; }

        public DateTime CreationTime { get; set; }

    }
