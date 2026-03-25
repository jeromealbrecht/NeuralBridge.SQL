using Npgsql;
using NeuralBridge.SQL.Services;

namespace NeuralBridge.SQL.Infrastructure;

public class Repository
{
    private readonly string _connectionString;

    public Repository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ─── SpectralSignatures ───────────────────────────────────────────────────

    public async Task<int> SaveSpectralSignatureAsync(string name, string sourceUrl, SpectralResult result)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO SpectralSignatures (name, source_url, low_energy, mid_energy, high_energy, air_energy, high_peak_hz, fft_data_json)
            VALUES (@name, @sourceUrl, @low, @mid, @high, @air, @peakHz, @fft::jsonb)
            RETURNING id;
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("name",      name);
        cmd.Parameters.AddWithValue("sourceUrl", sourceUrl);
        cmd.Parameters.AddWithValue("low",       (double)result.LowEnergy);
        cmd.Parameters.AddWithValue("mid",       (double)result.MidEnergy);
        cmd.Parameters.AddWithValue("high",      (double)result.HighEnergy);
        cmd.Parameters.AddWithValue("air",       (double)result.AirEnergy);
        cmd.Parameters.AddWithValue("peakHz",    (double)result.HighPeakHz);
        cmd.Parameters.AddWithValue("fft",       result.FftDataJson);

        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<IReadOnlyList<string>> GetSpectralSignaturesAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, source_url, high_energy, created_at FROM SpectralSignatures ORDER BY created_at DESC;", conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        var lines = new List<string>();

        while (await reader.ReadAsync())
            lines.Add($"[{reader["id"]}] {reader["name"]} | high={reader["high_energy"]:F6} | {reader["source_url"]} | {reader["created_at"]}");

        return lines;
    }

    // ─── UserProcessings ──────────────────────────────────────────────────────

    public async Task<int> SaveUserProcessingAsync(
        string userId, string inputFilename, int referenceId, float appliedReduction, bool success)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = """
            INSERT INTO UserProcessings (user_id, input_filename, reference_id, applied_reduction, success)
            VALUES (@userId, @filename, @refId, @reduction, @success)
            RETURNING id;
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("userId",    userId);
        cmd.Parameters.AddWithValue("filename",  inputFilename);
        cmd.Parameters.AddWithValue("refId",     referenceId);
        cmd.Parameters.AddWithValue("reduction", (double)appliedReduction);
        cmd.Parameters.AddWithValue("success",   success);

        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    // ─── Utilitaires ──────────────────────────────────────────────────────────

    public async Task<long> GetTableCountAsync(string tableName)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName}", conn);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}
