
double[] boidSpeeds = { 1.0, 1.5, 2.0, 2.5 };

Console.WriteLine("=== Boid Speed Array ===");
Console.WriteLine($"Initial array length: {boidSpeeds.Length}");

CollectionHelpers.PrintSpeeds(boidSpeeds);

// Modify array elements by index.
for (int index = 0; index < boidSpeeds.Length; index++)
{
    boidSpeeds[index] += 0.5;
}

Console.WriteLine("\nUpdated speeds:");
CollectionHelpers.PrintSpeeds(boidSpeeds);

// A list is appropriate because identifiers can be added or removed.
Console.WriteLine("\n=== Boid Identifier List ===");

List<string> boidIds = new()
{
    "boid-001",
    "boid-002",
    "boid-003"
};

boidIds.Add("boid-004");
boidIds.Remove("boid-002");

Console.WriteLine($"Final list count: {boidIds.Count}");

foreach (string boidId in boidIds)
{
    Console.WriteLine(boidId);
}

CollectionHelpers.ReportIdentifierExists(boidIds, "boid-003");
CollectionHelpers.ReportIdentifierExists(boidIds, "boid-002");

// A dictionary is appropriate for looking up a speed by boid identifier.
Console.WriteLine("\n=== Boid Speed Dictionary ===");

Dictionary<string, double> speedById = new()
{
    ["boid-001"] = 1.5,
    ["boid-003"] = 2.5,
    ["boid-004"] = 3.0
};

string requestedId = "boid-003";

if (speedById.TryGetValue(requestedId, out double speed))
{
    Console.WriteLine($"{requestedId} speed: {speed:F1}");
}
else
{
    Console.WriteLine($"No speed found for {requestedId}.");
}

string missingId = "boid-999";

if (speedById.TryGetValue(missingId, out double missingSpeed))
{
    Console.WriteLine($"{missingId} speed: {missingSpeed:F1}");
}
else
{
    Console.WriteLine($"No speed found for {missingId}.");
}

// A set is appropriate for tracking unique identifiers.
Console.WriteLine("\n=== Unique Boid Identifiers ===");

HashSet<string> uniqueBoidIds = new()
{
    "boid-001",
    "boid-003"
};

bool firstAdd = uniqueBoidIds.Add("boid-004");
bool duplicateAdd = uniqueBoidIds.Add("boid-001");

Console.WriteLine($"New identifier added: {firstAdd}");
Console.WriteLine($"Duplicate identifier added: {duplicateAdd}");
Console.WriteLine($"Unique identifier count: {uniqueBoidIds.Count}");

static class CollectionHelpers
{
    public static void PrintSpeeds(double[] speeds)
    {
        for (int index = 0; index < speeds.Length; index++)
        {
            Console.WriteLine($"Speed {index}: {speeds[index]:F1}");
        }
    }

    public static void ReportIdentifierExists(
        List<string> identifiers,
        string requestedId)
    {
        bool exists = identifiers.Contains(requestedId);

        Console.WriteLine(
            $"List contains {requestedId}: {exists}");
    }
}
