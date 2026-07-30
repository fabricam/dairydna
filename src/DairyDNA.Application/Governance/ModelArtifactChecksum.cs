using System.Security.Cryptography;
using System.Text;

namespace DairyDNA.Application.Governance;

/// <summary>
/// Computes a stable, reproducible checksum for a trained model artifact from its recorded
/// training metadata. The same algorithm/dataset/seed/hyperparameters/metrics combination always
/// yields the same checksum, which lets the registry detect duplicate registrations.
/// </summary>
public static class ModelArtifactChecksum
{
    public static string Compute(
        string algorithm,
        string datasetVersion,
        string featureSchemaVersion,
        int randomSeed,
        string hyperparametersJson,
        string metricsJson)
    {
        var stable = $"{algorithm}|{datasetVersion}|{featureSchemaVersion}|{randomSeed}|{hyperparametersJson}|{metricsJson}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(stable));
        return Convert.ToHexStringLower(bytes);
    }
}
