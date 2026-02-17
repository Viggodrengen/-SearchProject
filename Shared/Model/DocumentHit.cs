using System.Collections.Generic;

namespace Shared.Model;

public class DocumentHit
{
    public BEDocument? Document { get; set; }

    public int NoOfHits { get; set; }

    public List<string> Missing { get; set; } = new();
}
