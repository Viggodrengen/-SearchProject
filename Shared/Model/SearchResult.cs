using System;
using System.Collections.Generic;

namespace Shared.Model;

public class SearchResult
{
    public string[] Query { get; set; } = Array.Empty<string>();

    public int NoOfHits { get; set; }

    public List<DocumentHit> DocumentHits { get; set; } = new();

    public List<string> Ignored { get; set; } = new();

    public TimeSpan TimeUsed { get; set; }
    
}
