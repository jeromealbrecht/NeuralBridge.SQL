using Microsoft.SemanticKernel;
using Npgsql;
using System.ComponentModel;

namespace NeuralBridge.SQL
{
    public class DatabasePlugin
    {
        private readonly string _connectionString = "NEON";

        [KernelFunction]
        [Description("Récupère le nombre total de lignes dans une table spécifique de la base Neon.")]
        public async Task<string> GetTableCount([Description("Le nom de la table")] string tableName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                string query = $"SELECT COUNT(*) FROM {tableName}";

                await using var cmd = new NpgsqlCommand(query, conn);

                var count = await cmd.ExecuteScalarAsync();

                return $"La table {tableName} contient {count} enregistrements sur Neon.";
            }
            catch (Exception ex)
            {
                return $"Erreur Neon : {ex.Message}";
            }
        }
    }
}