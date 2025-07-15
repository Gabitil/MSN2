using Microsoft.OpenApi.Models;
using Oracle.ManagedDataAccess.Client;

const string userId = "ECLBDIT215";
const string password = "SenhadoBD58132!";
const string dataSource = "bdec2_high";

var connStr = $"User Id={userId};Password={password};Data Source={dataSource};";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000") // endereço do React
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configuração explícita do Oracle
string baseDirectory = Directory.GetCurrentDirectory();
string walletPath = Path.Combine(baseDirectory, "oracle_wallet");

// Configuração correta para o Oracle Managed Driver
OracleConfiguration.TnsAdmin = walletPath;
OracleConfiguration.WalletLocation = walletPath;  // Importante para wallets

// Adicionar suporte ao Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Oracle API", 
        Version = "v1",
        Description = "API de conexão com Oracle Database" 
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowFrontend"); // 👈 isso vem antes de UseAuthorization ou MapEndpoints

// ... seus outros middlewares
app.UseAuthorization();

// Configurar Swagger mesmo em produção para testes
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Oracle API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Endpoint de teste
app.MapGet("/test-db", async () =>
{


    

    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        // Testar consulta simples
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT SYSDATE FROM DUAL";
        var result = await cmd.ExecuteScalarAsync();

        // Tratar possível valor nulo
        if (result is null)
        {
            return Results.Ok(new
            {
                status = "⚠️ Conexão OK mas consulta retornou nulo",
                wallet_path = walletPath
            });
        }

        var serverDate = (DateTime)result;
        return Results.Ok(new
        {
            status = "✅ Conexão bem-sucedida!",
            server_time = serverDate.ToString("yyyy-MM-dd HH:mm:ss"),
            wallet_path = walletPath
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha na conexão Oracle",
                              statusCode: 500);
    }
})
.WithName("TestDatabaseConnection")
.WithOpenApi();

app.MapGet("/getusuarios", async () =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        // Consulta para obter usuários
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM USUARIO"; // Ajuste conforme sua tabela

        var usuarios = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            usuarios.Add(new
            {
                Id = reader.GetInt32(0), // Ajuste conforme sua tabela
                Nome = reader.GetString(1) // Ajuste conforme sua tabela
            });
        }

        return Results.Ok(usuarios);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter usuários",
                              statusCode: 500);
    }
})
.WithName("GetUsuarios")
.WithSummary("Consulta todos os usuários")
.WithOpenApi();


app.MapGet("/getusuario", async (string termo) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();

        if (int.TryParse(termo, out int id))
        {
            // Se o termo for um número, buscar por ID
            cmd.CommandText = "SELECT * FROM USUARIO WHERE ID_USUARIO = :Termo";
        }
        else
        {
            // Caso contrário, buscar por nome ou email
            cmd.CommandText = "SELECT * FROM USUARIO WHERE NOME = :Termo OR EMAIL = :Termo";
        }


        cmd.Parameters.Add(new OracleParameter("Termo", termo));

        var usuario = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            usuario.Add(new
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1)
            });
        }

        if (usuario.Count == 0)
        {
            return Results.NotFound(new { message = "Usuário não encontrado" });
        }

        return Results.Ok(usuario);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter usuário",
                              statusCode: 500);
    }

})
.WithName("GetUsuario")
.WithSummary("Consulta um usuário por ID, nome ou email")
.WithOpenApi();

app.MapPost("/addusuario", async (string nome, string email, string senha) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO USUARIO (NOME, EMAIL, Senha) VALUES (:Nome, :Email, :Senha)";
        cmd.Parameters.Add(new OracleParameter("Nome", nome));
        cmd.Parameters.Add(new OracleParameter("Email", email));
        cmd.Parameters.Add(new OracleParameter("Senha", senha));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Usuário adicionado com sucesso!" });
        }
        else
        {
            return Results.BadRequest(new { message = "Nenhum usuário foi adicionado." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao adicionar usuário",
                              statusCode: 500);
    }
})
.WithName("AddUsuario")
.WithSummary("Adiciona um novo usuário")
.WithOpenApi();

app.MapPut("/updateusuario", async (int id, string nome, string email, string senha) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE USUARIO SET NOME = :Nome, EMAIL = :Email, Senha = :Senha WHERE ID_USUARIO = :Id";
        cmd.Parameters.Add(new OracleParameter("Nome", OracleDbType.Varchar2)).Value = nome ?? (object)DBNull.Value;
        cmd.Parameters.Add(new OracleParameter("Email", OracleDbType.Varchar2)).Value = email ?? (object)DBNull.Value;
        cmd.Parameters.Add(new OracleParameter("Senha", OracleDbType.Varchar2)).Value = senha ?? (object)DBNull.Value;
        cmd.Parameters.Add(new OracleParameter("Id", OracleDbType.Int32)).Value = id;


        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Usuário atualizado com sucesso!" });
        }
        else
        {
            return Results.NotFound(new { message = "Usuário não encontrado." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao atualizar usuário",
                              statusCode: 500);
    }
})
.WithName("UpdateUsuario")
.WithSummary("Atualiza um usuário existente")
.WithOpenApi();

app.MapDelete("/deleteusuario", async (int id) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM USUARIO WHERE ID_USUARIO = :Id";
        cmd.Parameters.Add(new OracleParameter("Id", id));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Usuário deletado com sucesso!" });
        }
        else
        {
            return Results.NotFound(new { message = "Usuário não encontrado." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao deletar usuário",
                              statusCode: 500);
    }
})
.WithName("DeleteUsuario")
.WithSummary("Deleta um usuário existente")
.WithOpenApi();

app.MapGet("/getsalas", async () =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        // Consulta para obter salas
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM SALA"; // Ajuste conforme sua tabela

        var salas = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            salas.Add(new
            {
                Id = reader.GetInt32(0), // Ajuste conforme sua tabela
                Nome = reader.GetString(1) // Ajuste conforme sua tabela
            });
        }

        return Results.Ok(salas);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter salas",
                              statusCode: 500);
    }
})
.WithName("GetSalas")
.WithSummary("Consulta todas as salas")
.WithOpenApi();

app.MapGet("/getsala", async (string termo) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();

        if (int.TryParse(termo, out int id))
        {
            // Se o termo for um número, buscar por ID
            cmd.CommandText = "SELECT * FROM SALA WHERE ID_SALA = :Termo";
        }
        else
        {
            // Caso contrário, buscar por nome
            cmd.CommandText = "SELECT * FROM SALA WHERE NOME = :Termo";
        }

        cmd.Parameters.Add(new OracleParameter("Termo", termo));

        var sala = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sala.Add(new
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1)
            });
        }

        if (sala.Count == 0)
        {
            return Results.NotFound(new { message = "Sala não encontrada" });
        }

        return Results.Ok(sala);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter sala",
                              statusCode: 500);
    }
})
.WithName("GetSala")
.WithSummary("Consulta uma sala por ID ou nome")
.WithOpenApi();

app.MapPost("/addsala", async (string nome, string tipo, int criador) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO SALA (NOME, TIPO, CRIADOR) VALUES (:Nome, :Tipo, :Criador)";
        cmd.Parameters.Add(new OracleParameter("Nome", nome));
        cmd.Parameters.Add(new OracleParameter("Tipo", tipo));
        cmd.Parameters.Add(new OracleParameter("Criador", criador));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Sala adicionada com sucesso!" });
        }
        else
        {
            return Results.BadRequest(new { message = "Nenhuma sala foi adicionada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao adicionar sala",
                              statusCode: 500);
    }
})
.WithName("AddSala")
.WithSummary("Adiciona uma nova sala")
.WithOpenApi();

app.MapPut("/updatesala", async (int id, string nome, string tipo, int criador) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE SALA SET NOME = :Nome, TIPO = :Tipo, CRIADOR = :Criador WHERE ID_SALA = :Id";
        cmd.Parameters.Add(new OracleParameter("Nome", OracleDbType.Varchar2)).Value = nome ?? (object)DBNull.Value;
        cmd.Parameters.Add(new OracleParameter("Tipo", OracleDbType.Varchar2)).Value = tipo ?? (object)DBNull.Value;
        cmd.Parameters.Add(new OracleParameter("Criador", OracleDbType.Int32)).Value = criador;
        cmd.Parameters.Add(new OracleParameter("Id", OracleDbType.Int32)).Value = id;

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Sala atualizada com sucesso!" });
        }
        else
        {
            return Results.NotFound(new { message = "Sala não encontrada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao atualizar sala",
                              statusCode: 500);
    }
})
.WithName("UpdateSala")
.WithSummary("Atualiza uma sala existente")
.WithOpenApi();

app.MapDelete("/deletesala", async (int id) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SALA WHERE ID_SALA = :Id";
        cmd.Parameters.Add(new OracleParameter("Id", id));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Sala deletada com sucesso!" });
        }
        else
        {
            return Results.NotFound(new { message = "Sala não encontrada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao deletar sala",
                              statusCode: 500);
    }
})
.WithName("DeleteSala")
.WithSummary("Deleta uma sala existente")
.WithOpenApi();

app.MapGet("/getmensagens", async () =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        var todasMensagens = new
        {
            MensagensSala = new List<object>(),
            MensagensUsuario = new List<object>()
        };

        // ==== Consulta 1: MENSAGEM_SALA ====
        await using (var cmdSala = conn.CreateCommand())
        {
            cmdSala.CommandText = "SELECT ID_MENSAGEM, ID_SALA, ID_REMETENTE, MENSAGEM, DATA_ENVIO FROM MENSAGEM_SALA";

            await using var reader = await cmdSala.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                todasMensagens.MensagensSala.Add(new
                {
                    IdMensagem = reader.GetInt32(0),
                    IdSala = reader.GetInt32(1),
                    IdRemetente = reader.GetInt32(2),
                    Mensagem = reader.GetString(3),
                    DataEnvio = reader.GetDateTime(4),
                    Tipo = "SALA"
                });
            }
        }

        // ==== Consulta 2: MENSAGEM_USUARIO ====
        await using (var cmdUsuario = conn.CreateCommand())
        {
            cmdUsuario.CommandText = "SELECT ID_MENSAGEM, ID_REMETENTE, ID_DESTINATARIO, MENSAGEM, DATA_ENVIO FROM MENSAGEM_USUARIO";

            await using var reader = await cmdUsuario.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                todasMensagens.MensagensUsuario.Add(new
                {
                    IdMensagem = reader.GetInt32(0),
                    IdRemetente = reader.GetInt32(1),
                    IdDestinatario = reader.GetInt32(2),
                    Mensagem = reader.GetString(3),
                    DataEnvio = reader.GetDateTime(4),
                    Tipo = "USUARIO"
                });
            }
        }

        return Results.Ok(todasMensagens);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter mensagens",
                              statusCode: 500);
    }
})
.WithName("GetMensagens")
.WithSummary("Consulta todas as mensagens (sala e usuário)")
.WithOpenApi();

app.MapGet("/getmensagemenviadas", async (int id) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        var mensagens = new List<object>();

        // ====== MENSAGEM_SALA ======
        await using (var cmdSala = conn.CreateCommand())
        {
            cmdSala.CommandText = @"
                SELECT ID_MENSAGEM, ID_SALA, ID_REMETENTE, MENSAGEM, DATA_ENVIO
                FROM MENSAGEM_SALA
                WHERE ID_REMETENTE = :Id";
            cmdSala.Parameters.Add(new OracleParameter("Id", id));

            await using var readerSala = await cmdSala.ExecuteReaderAsync();
            while (await readerSala.ReadAsync())
            {
                mensagens.Add(new
                {
                    IdMensagem = readerSala.GetInt32(0),
                    IdSala = readerSala.GetInt32(1),
                    IdRemetente = readerSala.GetInt32(2),
                    Mensagem = readerSala.GetString(3),
                    DataEnvio = readerSala.GetDateTime(4),
                    Tipo = "SALA"
                });
            }
        }

        // ====== MENSAGEM_USUARIO ======
        await using (var cmdUsuario = conn.CreateCommand())
        {
            cmdUsuario.CommandText = @"
                SELECT ID_MENSAGEM, ID_REMETENTE, ID_DESTINATARIO, MENSAGEM, DATA_ENVIO
                FROM MENSAGEM_USUARIO
                WHERE ID_REMETENTE = :Id";
            cmdUsuario.Parameters.Add(new OracleParameter("Id", id));

            await using var readerUsuario = await cmdUsuario.ExecuteReaderAsync();
            while (await readerUsuario.ReadAsync())
            {
                mensagens.Add(new
                {
                    IdMensagem = readerUsuario.GetInt32(0),
                    IdRemetente = readerUsuario.GetInt32(1),
                    IdDestinatario = readerUsuario.GetInt32(2),
                    Mensagem = readerUsuario.GetString(3),
                    DataEnvio = readerUsuario.GetDateTime(4),
                    Tipo = "USUARIO"
                });
            }
        }

        if (mensagens.Count == 0)
        {
            return Results.NotFound(new { message = "Nenhuma mensagem enviada encontrada para esse usuário." });
        }

        return Results.Ok(mensagens);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter mensagens enviadas",
                              statusCode: 500);
    }
})
.WithName("GetMensagensEnviadas")
.WithSummary("Obtém todas as mensagens enviadas por um usuário (sala e usuário)")
.WithOpenApi();

app.MapGet("/getmensagemrecebidas", async (int id) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        var mensagens = new List<object>();

        // ====== MENSAGEM_SALA - onde o usuário participa ======
        await using (var cmdSala = conn.CreateCommand())
        {
            cmdSala.CommandText = @"
                SELECT ID_MENSAGEM, ID_SALA, ID_REMETENTE, MENSAGEM, DATA_ENVIO
                FROM MENSAGEM_SALA
                WHERE ID_SALA IN (
                    SELECT ID_SALA
                    FROM PARTICIPACAO_SALA
                    WHERE ID_USUARIO = :Id
                )";
            cmdSala.Parameters.Add(new OracleParameter("Id", id));

            await using var readerSala = await cmdSala.ExecuteReaderAsync();
            while (await readerSala.ReadAsync())
            {
                mensagens.Add(new
                {
                    IdMensagem = readerSala.GetInt32(0),
                    IdSala = readerSala.GetInt32(1),
                    IdRemetente = readerSala.GetInt32(2),
                    Mensagem = readerSala.GetString(3),
                    DataEnvio = readerSala.GetDateTime(4),
                    Tipo = "SALA"
                });
            }
        }

        // ====== MENSAGEM_USUARIO - mensagens diretas ======
        await using (var cmdUsuario = conn.CreateCommand())
        {
            cmdUsuario.CommandText = @"
                SELECT ID_MENSAGEM, ID_REMETENTE, ID_DESTINATARIO, MENSAGEM, DATA_ENVIO
                FROM MENSAGEM_USUARIO
                WHERE ID_DESTINATARIO = :Id";
            cmdUsuario.Parameters.Add(new OracleParameter("Id", id));

            await using var readerUsuario = await cmdUsuario.ExecuteReaderAsync();
            while (await readerUsuario.ReadAsync())
            {
                mensagens.Add(new
                {
                    IdMensagem = readerUsuario.GetInt32(0),
                    IdRemetente = readerUsuario.GetInt32(1),
                    IdDestinatario = readerUsuario.GetInt32(2),
                    Mensagem = readerUsuario.GetString(3),
                    DataEnvio = readerUsuario.GetDateTime(4),
                    Tipo = "USUARIO"
                });
            }
        }

        if (mensagens.Count == 0)
        {
            return Results.NotFound(new { message = "Nenhuma mensagem recebida encontrada para esse usuário." });
        }

        return Results.Ok(mensagens);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter mensagens recebidas",
                              statusCode: 500);
    }
})
.WithName("GetMensagensRecebidas")
.WithSummary("Obtém todas as mensagens recebidas por um usuário (de salas que ele participa e mensagens diretas)")
.WithOpenApi();

app.MapGet("/getmensagensporid", async (int id) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        var mensagens = new List<object>();

        // Consultar MENSAGEM_SALA por ID
        await using (var cmdSala = conn.CreateCommand())
        {
            cmdSala.CommandText = "SELECT ID_MENSAGEM, ID_SALA, ID_REMETENTE, MENSAGEM, DATA_ENVIO FROM MENSAGEM_SALA WHERE ID_SALA = :Id";
            cmdSala.Parameters.Add(new OracleParameter("Id", id));

            await using var readerSala = await cmdSala.ExecuteReaderAsync();
            while (await readerSala.ReadAsync())
            {
                mensagens.Add(new
                {
                    IdMensagem = readerSala.GetInt32(0),
                    IdSala = readerSala.GetInt32(1),
                    IdRemetente = readerSala.GetInt32(2),
                    Mensagem = readerSala.GetString(3),
                    DataEnvio = readerSala.GetDateTime(4),
                    Tipo = "SALA"
                });
            }
        }

        // Consultar MENSAGEM_USUARIO por ID
        await using (var cmdUsuario = conn.CreateCommand())
        {
            cmdUsuario.CommandText = "SELECT ID_MENSAGEM, ID_REMETENTE, ID_DESTINATARIO, MENSAGEM, DATA_ENVIO FROM MENSAGEM_USUARIO WHERE ID_DESTINATARIO = :Id";
            cmdUsuario.Parameters.Add(new OracleParameter("Id", id));

            await using var readerUsuario = await cmdUsuario.ExecuteReaderAsync();
            while (await readerUsuario.ReadAsync())
            {
                mensagens.Add(new
                {
                    IdMensagem = readerUsuario.GetInt32(0),
                    IdRemetente = readerUsuario.GetInt32(1),
                    IdDestinatario = readerUsuario.GetInt32(2),
                    Mensagem = readerUsuario.GetString(3),
                    DataEnvio = readerUsuario.GetDateTime(4),
                    Tipo = "USUARIO"
                });
            }
        }

        if (mensagens.Count == 0)
        {
            return Results.NotFound(new { message = "Mensagem não encontrada." });
        }

        return Results.Ok(mensagens);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter mensagem por ID",
                              statusCode: 500);
    }
})
.WithName("GetMensagensPorId")
.WithSummary("Obtém mensagens por ID (sala e usuário)")
.WithOpenApi();

app.MapPost("/addmensagemsala", async (int idSala, int idRemetente, string mensagem) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO MENSAGEM_SALA (ID_SALA, ID_REMETENTE, MENSAGEM, DATA_ENVIO) VALUES (:IdSala, :IdRemetente, :Mensagem, SYSDATE)";
        cmd.Parameters.Add(new OracleParameter("IdSala", idSala));
        cmd.Parameters.Add(new OracleParameter("IdRemetente", idRemetente));
        cmd.Parameters.Add(new OracleParameter("Mensagem", mensagem));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Mensagem enviada com sucesso!" });
        }
        else
        {
            return Results.BadRequest(new { message = "Nenhuma mensagem foi enviada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao enviar mensagem",
                              statusCode: 500);
    }
})
.WithName("AddMensagemSala")
.WithSummary("Adiciona uma nova mensagem em uma sala")
.WithOpenApi();

app.MapPost("/addmensagemusuario", async (int idRemetente, int idDestinatario, string mensagem) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO MENSAGEM_USUARIO (ID_REMETENTE, ID_DESTINATARIO, MENSAGEM, DATA_ENVIO) VALUES (:IdRemetente, :IdDestinatario, :Mensagem, SYSDATE)";
        cmd.Parameters.Add(new OracleParameter("IdRemetente", idRemetente));
        cmd.Parameters.Add(new OracleParameter("IdDestinatario", idDestinatario));
        cmd.Parameters.Add(new OracleParameter("Mensagem", mensagem));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Mensagem enviada com sucesso!" });
        }
        else
        {
            return Results.BadRequest(new { message = "Nenhuma mensagem foi enviada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao enviar mensagem",
                              statusCode: 500);
    }
})
.WithName("AddMensagemUsuario")
.WithSummary("Adiciona uma nova mensagem direta entre usuários")
.WithOpenApi();

app.MapPost("/addparticipacao", async (int idSala, int idUsuario) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO PARTICIPACAO_SALA (ID_SALA, ID_USUARIO) VALUES (:IdSala, :IdUsuario)";
        cmd.Parameters.Add(new OracleParameter("IdSala", idSala));
        cmd.Parameters.Add(new OracleParameter("IdUsuario", idUsuario));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Participação adicionada com sucesso!" });
        }
        else
        {
            return Results.BadRequest(new { message = "Nenhuma participação foi adicionada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao adicionar participação",
                              statusCode: 500);
    }
})
.WithName("AddParticipacao")
.WithSummary("Adiciona um usuário a uma sala")
.WithOpenApi();

app.MapDelete("/removeparticipacao", async (int idSala, int idUsuario) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM PARTICIPACAO_SALA WHERE ID_SALA = :IdSala AND ID_USUARIO = :IdUsuario";
        cmd.Parameters.Add(new OracleParameter("IdSala", idSala));
        cmd.Parameters.Add(new OracleParameter("IdUsuario", idUsuario));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Participação removida com sucesso!" });
        }
        else
        {
            return Results.NotFound(new { message = "Participação não encontrada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao remover participação",
                              statusCode: 500);
    }
})
.WithName("RemoveParticipacao")
.WithSummary("Remove um usuário de uma sala")
.WithOpenApi();


app.MapPost("/login", async (string email, string senha) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM USUARIO WHERE EMAIL = :Email AND SENHA = :Senha";
        cmd.Parameters.Add(new OracleParameter("Email", email));
        cmd.Parameters.Add(new OracleParameter("Senha", senha));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return Results.Ok(new
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Email = reader.GetString(2),
                Privilegios = reader.GetString(5) // Supondo que o privilégio esteja na coluna 4
            });
        }
        else
        {
            return Results.Json(
            new { message = "Credenciais inválidas" },
            statusCode: 401
            );
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao realizar login",
                              statusCode: 500);
    }
})
.WithName("Login")
.WithSummary("Realiza o login de um usuário")
.WithOpenApi();

app.MapPost("/bloqueiarusuario", async (int idbloqueia, int idbloquado) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO BLOQUEIO(ID_USU_BLOQUEIA, ID_USU_BLOQUEADO) VALUES (:IdBloqueia, :IdBloqueado)";
        cmd.Parameters.Add(new OracleParameter("IdBloqueia", idbloqueia));
        cmd.Parameters.Add(new OracleParameter("IdBloqueado", idbloquado));
        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Usuário bloqueado com sucesso!" });
        }
        else
        {
            return Results.BadRequest(new { message = "Nenhum bloqueio foi realizado." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao bloquear usuário",
                              statusCode: 500);
    }
})
.WithName("BloquearUsuario")
.WithSummary("Bloqueia um usuário")
.WithOpenApi();

app.MapDelete("/desbloquearusuario", async (int idbloqueia, int idbloquado) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM BLOQUEIO WHERE ID_USU_BLOQUEIA = :IdBloqueia AND ID_USU_BLOQUEADO = :IdBloqueado";
        cmd.Parameters.Add(new OracleParameter("IdBloqueia", idbloqueia));
        cmd.Parameters.Add(new OracleParameter("IdBloqueado", idbloquado));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Usuário desbloqueado com sucesso!" });
        }
        else
        {
            return Results.NotFound(new { message = "Nenhum bloqueio encontrado." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao desbloquear usuário",
                              statusCode: 500);
    }
})
.WithName("DesbloquearUsuario")
.WithSummary("Desbloqueia um usuário")
.WithOpenApi();

app.Run();

