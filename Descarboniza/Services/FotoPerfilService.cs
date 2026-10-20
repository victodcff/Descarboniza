using Descarboniza.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace Descarboniza.Services
{
    /// <summary>
    /// Guarda a foto de perfil de cada usuário no próprio banco (PostgreSQL).
    /// O disco do Render grátis é apagado a cada deploy, então arquivo em pasta não serve.
    /// A tabela é criada sozinha na primeira execução (não precisa de migration).
    /// </summary>
    public class FotoPerfilService
    {
        private readonly DescarbonizaContext _context;

        public FotoPerfilService(DescarbonizaContext context)
        {
            _context = context;
        }

        public async Task GarantirTabelaAsync()
        {
            // Ao excluir o usuário, a foto some junto (ON DELETE CASCADE).
            const string sql = @"
CREATE TABLE IF NOT EXISTS ""FotosPerfil"" (
    ""UsuarioId""    text NOT NULL PRIMARY KEY
                     REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE,
    ""Dados""        bytea NOT NULL,
    ""AtualizadaEm"" timestamp with time zone NOT NULL DEFAULT now()
);";

            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        public async Task<byte[]?> ObterAsync(string usuarioId)
        {
            var conexao = _context.Database.GetDbConnection();
            var abriu = false;

            if (conexao.State != ConnectionState.Open)
            {
                await conexao.OpenAsync();
                abriu = true;
            }

            try
            {
                await using var comando = conexao.CreateCommand();
                comando.CommandText = @"SELECT ""Dados"" FROM ""FotosPerfil"" WHERE ""UsuarioId"" = @id";

                var parametro = comando.CreateParameter();
                parametro.ParameterName = "id";
                parametro.Value = usuarioId;
                comando.Parameters.Add(parametro);

                var resultado = await comando.ExecuteScalarAsync();

                return resultado as byte[];
            }
            finally
            {
                if (abriu)
                    await conexao.CloseAsync();
            }
        }

        public async Task SalvarAsync(string usuarioId, byte[] dados)
        {
            const string sql = @"
INSERT INTO ""FotosPerfil"" (""UsuarioId"", ""Dados"", ""AtualizadaEm"")
VALUES (@id, @dados, now())
ON CONFLICT (""UsuarioId"")
DO UPDATE SET ""Dados"" = EXCLUDED.""Dados"", ""AtualizadaEm"" = now();";

            await _context.Database.ExecuteSqlRawAsync(
                sql,
                new NpgsqlParameter("id", usuarioId),
                new NpgsqlParameter("dados", NpgsqlDbType.Bytea) { Value = dados });
        }

        public async Task RemoverAsync(string usuarioId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""FotosPerfil"" WHERE ""UsuarioId"" = @id",
                new NpgsqlParameter("id", usuarioId));
        }
    }
}
