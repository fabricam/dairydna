namespace DairyDNA.DataGenerator;

/// <summary>Continental US placement anchors for synthetic network entities.</summary>
public static class UsGeography
{
    public sealed record StateAnchor(string StateCode, string Name, decimal Latitude, decimal Longitude);

    /// <summary>Dairy / processing / demand anchors across the contiguous US.</summary>
    public static IReadOnlyList<StateAnchor> Anchors { get; } =
    [
        new("WI", "Wisconsin", 43.78m, -88.79m),
        new("CA", "California", 36.78m, -119.42m),
        new("ID", "Idaho", 44.07m, -114.74m),
        new("NY", "New York", 42.17m, -74.95m),
        new("PA", "Pennsylvania", 40.59m, -77.21m),
        new("TX", "Texas", 31.97m, -99.90m),
        new("WA", "Washington", 47.40m, -121.49m),
        new("MN", "Minnesota", 46.73m, -94.69m),
        new("MI", "Michigan", 43.33m, -84.54m),
        new("OH", "Ohio", 40.39m, -82.76m),
        new("IL", "Illinois", 40.35m, -88.99m),
        new("IA", "Iowa", 42.01m, -93.21m),
        new("NM", "New Mexico", 34.52m, -105.87m),
        new("VT", "Vermont", 44.06m, -72.71m),
        new("OR", "Oregon", 43.80m, -120.55m),
        new("CO", "Colorado", 39.06m, -105.31m),
        new("GA", "Georgia", 33.04m, -83.64m),
        new("FL", "Florida", 27.77m, -81.69m),
        new("AZ", "Arizona", 33.73m, -111.43m),
        new("KS", "Kansas", 38.53m, -96.73m)
    ];

    public static (string StateCode, decimal Latitude, decimal Longitude) Place(int index, Random rng, decimal jitterDegrees = 0.35m)
    {
        var anchor = Anchors[index % Anchors.Count];
        var lat = anchor.Latitude + (decimal)(rng.NextDouble() * 2 - 1) * jitterDegrees;
        var lon = anchor.Longitude + (decimal)(rng.NextDouble() * 2 - 1) * jitterDegrees;
        return (anchor.StateCode, lat, lon);
    }
}
