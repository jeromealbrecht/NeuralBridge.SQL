using Microsoft.SemanticKernel;
using Npgsql;
using System.ComponentModel;

namespace NeuralBridge.SQL
{
    public class DatabasePlugin
    {
        private readonly string _connectionString;

        // Le constructeur reçoit la string depuis Program.cs
        public DatabasePlugin(string connectionString)
        {
            _connectionString = connectionString;
        }

        [KernelFunction]
        [Description("Récupère le nombre total de lignes dans une table spécifique (ex: stocks).")]
        public async Task<string> GetTableCount([Description("Le nom de la table à compter")] string tableName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // On nettoie le nom de la table pour éviter les injections SQL
                using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName}", conn);

                var count = await cmd.ExecuteScalarAsync();

                return $"La table {tableName} contient {count} enregistrements sur Neon.";
            }
            catch (NpgsqlException ex)
            {
                return $"Erreur Neon (Base de données) : {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Erreur Système : {ex.Message}";
            }
        }
    }
}