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

// ============================
// Teste de Conexão ao Banco
// Endpoint GET /test-db
// Verifica a conexão com o Oracle Database e retorna status, data do servidor e caminho do wallet.
// ============================
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

// ============================
// Listar Usuários
// Endpoint GET /getusuarios
// Retorna todos os registros da tabela USUARIO.
// ============================
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

// ============================
// Consultar Usuário
// Endpoint GET /getusuario?termo=...
// Busca usuário por ID, nome ou email conforme o parâmetro informado.
// ============================
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

// ============================
// Adicionar Usuário
// Endpoint POST /addusuario
// Insere um novo registro na tabela USUARIO com nome, email e senha fornecidos.
// ============================
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

// ============================
// Atualizar Usuário
// Endpoint PUT /updateusuario
// Atualiza nome, email e senha de um usuário existente por ID.
// ============================
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

// ============================
// Deletar Usuário
// Endpoint DELETE /deleteusuario
// Remove o usuário identificado pelo ID informado.
// ============================
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

// ============================
// Listar Salas
// Endpoint GET /getsalas
// Retorna todos os registros da tabela SALA.
// ============================
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
                IdSala = reader.GetInt32(0), // Ajuste conforme sua tabela
                Nome = reader.GetString(1), // Ajuste conforme sua tabela
                Tipo = reader.GetString(2), // Ajuste conforme sua tabela
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

// ============================
// Consultar Sala
// Endpoint GET /getsala?termo=...
// Busca sala por ID ou nome conforme o parâmetro.
// ============================
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
            cmd.CommandText = @"SELECT 
                                    ID_SALA,
                                    NOME,
                                    TIPO,
                                    CRIADOR
                                FROM SALA 
                                WHERE ID_SALA = :Termo";
        }
        else
        {
            // Caso contrário, buscar por nome
            cmd.CommandText = @"SELECT 
                                    ID_SALA,
                                    NOME,
                                    TIPO,
                                    CRIADOR
                                FROM SALA 
                                WHERE NOME = :Termo";
        }

        cmd.Parameters.Add(new OracleParameter("Termo", termo));

        var sala = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sala.Add(new
            {
                IdSala = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Tipo = reader.GetString(2),
                Criador = reader.GetInt32(3) // Assumindo que CRIADOR é um ID de usuário
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

// ============================
// Adicionar Sala
// Endpoint POST /addsala
// Insere um novo registro na tabela SALA com nome, tipo e criador.
// ============================
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

// ============================
// Atualizar Sala
// Endpoint PUT /updatesala
// Atualiza nome, tipo ou criador de uma sala existente por ID.
// ============================
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

// ============================
// Deletar Sala
// Endpoint DELETE /deletesala
// Remove a sala identificada pelo ID informado.
// ============================
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

// ============================
// Listar Mensagens (Sala e Usuário)
// Endpoint GET /getmensagens
// Retorna todas as mensagens de salas e mensagens diretas entre usuários.
// ============================
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
            cmdSala.CommandText = @"
            SELECT 
                ID_MENSAGEM,
                ID_SALA, 
                ID_REMETENTE, 
                MENSAGEM, 
                DATA_ENVIO 
            FROM MENSAGEM_SALA";

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
            cmdUsuario.CommandText = @"
            SELECT 
                MU.ID_MENSAGEM,
                MU.ID_REMETENTE,
                MU.ID_DESTINATARIO,
                D.NOME AS NOME_DESTINATARIO,
                MU.MENSAGEM,
                MU.DATA_ENVIO
            FROM MENSAGEM_USUARIO MU
            JOIN USUARIO D ON MU.ID_DESTINATARIO = D.ID_USUARIO";

            await using var reader = await cmdUsuario.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                todasMensagens.MensagensUsuario.Add(new
                {
                    IdMensagem = reader.GetInt32(0),
                    IdRemetente = reader.GetInt32(1),
                    IdDestinatario = reader.GetInt32(2),
                    NomeDestinatario = reader.GetString(3),
                    Mensagem = reader.GetString(4),
                    DataEnvio = reader.GetDateTime(5),
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

app.MapGet("/getmensagenssala", async (int idSala)=>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        var mensagensSala = new List<object>();

        // Consulta MENSAGEM_SALA por ID da sala
        await using (var cmdSala = conn.CreateCommand())
        {
            cmdSala.CommandText = @"
            SELECT 
                MS.ID_MENSAGEM,
                MS.ID_SALA,
                MS.ID_REMETENTE,
                U.NOME       AS NOME_REMETENTE,
                MS.MENSAGEM,
                MS.DATA_ENVIO
            FROM MENSAGEM_SALA MS
            JOIN USUARIO U 
                ON MS.ID_REMETENTE = U.ID_USUARIO
            WHERE MS.ID_SALA = :Id";

            cmdSala.Parameters.Add(new OracleParameter("IdSala", idSala));

            await using var readerSala = await cmdSala.ExecuteReaderAsync();
            while (await readerSala.ReadAsync())
            {
                mensagensSala.Add(new
                {
                    IdMensagem = readerSala.GetInt32(0),
                    IdSala = readerSala.GetInt32(1),
                    IdRemetente = readerSala.GetInt32(2),
                    NomeRemetente = readerSala.GetString(3),
                    Mensagem = readerSala.GetString(4),
                    DataEnvio = readerSala.GetDateTime(5),
                    Tipo = "SALA"
                });
            }
        }

        if (mensagensSala.Count == 0)
        {
            return Results.NotFound(new { message = "Nenhuma mensagem encontrada para essa sala." });
        }

        return Results.Ok(mensagensSala);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter mensagens da sala",
                              statusCode: 500);
    }
})
.WithName("GetMensagensSala")
.WithSummary("Obtém mensagens de uma sala específica")
.WithOpenApi();

// ============================
// Mensagens Enviadas
// Endpoint GET /getmensagemenviadas?id=...
// Retorna todas as mensagens enviadas por um usuário em salas e diretas.
// ============================
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
            cmdUsuario.CommandText = cmdUsuario.CommandText = @"
            SELECT 
                MU.ID_MENSAGEM,
                MU.ID_REMETENTE,
                MU.ID_DESTINATARIO,
                D.NOME AS NOME_DESTINATARIO,
                MU.MENSAGEM,
                MU.DATA_ENVIO
            FROM MENSAGEM_USUARIO MU
            JOIN USUARIO D ON MU.ID_DESTINATARIO = D.ID_USUARIO
            WHERE MU.ID_REMETENTE = :Id";
            cmdUsuario.Parameters.Add(new OracleParameter("Id", id));

            await using var readerUsuario = await cmdUsuario.ExecuteReaderAsync();
            while (await readerUsuario.ReadAsync())
            {
                mensagens.Add(new
                {
                    IdMensagem = readerUsuario.GetInt32(0),
                    IdRemetente = readerUsuario.GetInt32(1),
                    IdDestinatario = readerUsuario.GetInt32(2),
                    NomeDestinatario = readerUsuario.GetString(3),
                    Mensagem = readerUsuario.GetString(4),
                    DataEnvio = readerUsuario.GetDateTime(5),
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

// ============================
// Mensagens Recebidas
// Endpoint GET /getmensagemrecebidas?id=...
// Retorna todas as mensagens recebidas por um usuário de salas que participa e diretas.
// ============================
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

// ============================
// Mensagens por Sala
// Endpoint GET /getmensagensporid?id=...
// Consulta mensagens de uma sala ou mensagens diretas de/para um usuário específico.
// ============================
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

// ============================
// Enviar Mensagem em Sala
// Endpoint POST /addmensagemsala
// Insere nova mensagem na tabela MENSAGEM_SALA.
// ============================
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

// ============================
// Registrar Visualização em Sala
// Endpoint POST /addvisualizacaosala
// Insere registro de visualização de mensagem por usuário.
// ============================
app.MapPost("/addvisualizacaosala", async (int idMensagem, int idUsuario) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO VISUALIZACAO_SALA (ID_MENSAGEM, ID_USUARIO, DATA_VISUALIZACAO) VALUES (:IdMensagem, :IdUsuario, SYSDATE)";
        cmd.Parameters.Add(new OracleParameter("IdMensagem", idMensagem));
        cmd.Parameters.Add(new OracleParameter("IdUsuario", idUsuario));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Visualização registrada com sucesso!" });
        }
        else
        {
            return Results.BadRequest(new { message = "Nenhuma visualização foi registrada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao registrar visualização",
                              statusCode: 500);
    }
})
.WithName("AddVisualizacaoSala")
.WithSummary("Registra a visualização de uma mensagem em uma sala por um usuário")
.WithOpenApi();

// ============================
// Obter Visualizações de Sala
// Endpoint GET /getvisualizacaosala? idMensagem=...
// Retorna lista de usuários que visualizaram a mensagem e data de visualização.
// ============================
app.MapGet("/getvisualizacaosala", async (int idMensagem) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ID_USUARIO, DATA_VISUALIZACAO FROM VISUALIZACAO_SALA WHERE ID_MENSAGEM = :IdMensagem";
        cmd.Parameters.Add(new OracleParameter("IdMensagem", idMensagem));

        var visualizacoes = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            visualizacoes.Add(new
            {
                IdUsuario = reader.GetInt32(0),
                DataVisualizacao = reader.GetDateTime(1)
            });
        }

        if (visualizacoes.Count == 0)
        {
            return Results.NotFound(new { message = "Nenhuma visualização encontrada para esta mensagem." });
        }

        return Results.Ok(visualizacoes);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter visualizações",
                              statusCode: 500);
    }
})
.WithName("GetVisualizacaoSala")
.WithSummary("Obtém todas as visualizações de uma mensagem em uma sala")
.WithOpenApi();

// ============================
// Atualizar Visualização de Usuário
// Endpoint PUT /updatevisualizacaousuario
// Atualiza o status de visualização de uma mensagem entre usuários.
// ============================

app.MapPut("/updatevisualizacaousuario", async (int idMensagem, int idUsuario) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE MENSAGEM_USUARIO SET VISUALIZADA = 1 WHERE ID_MENSAGEM = :IdMensagem AND ID_DESTINATARIO = :IdUsuario";
        cmd.Parameters.Add(new OracleParameter("IdMensagem", idMensagem));
        cmd.Parameters.Add(new OracleParameter("IdUsuario", idUsuario));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();

        if (rowsAffected > 0)
        {
            return Results.Ok(new { message = "Visualização atualizada com sucesso!" });
        }
        else
        {
            return Results.NotFound(new { message = "Visualização não encontrada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao atualizar visualização",
                              statusCode: 500);
    }
})
.WithName("UpdateVisualizacaoUsuario")
.WithSummary("Atualiza a visualização de uma mensagem entre usuários")
.WithOpenApi();

// ============================
// Obter Visualização de Usuário
// Endpoint GET /getvisualizacaousuario?idMensagem=...&idUsuario=...
// Retorna o status de visualização de uma mensagem entre usuários.
// ============================

app.MapGet("/getvisualizacaousuario", async (int idMensagem, int idUsuario) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT VISUALIZADA FROM MENSAGEM_USUARIO WHERE ID_MENSAGEM = :IdMensagem AND ID_DESTINATARIO = :IdUsuario";
        cmd.Parameters.Add(new OracleParameter("IdMensagem", idMensagem));
        cmd.Parameters.Add(new OracleParameter("IdUsuario", idUsuario));

        var visualizacao = await cmd.ExecuteScalarAsync();

        if (visualizacao != null)
        {
            return Results.Ok(new { Visualizada = visualizacao });
        }
        else
        {
            return Results.NotFound(new { message = "Visualização não encontrada." });
        }
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message + "\n" + ex.StackTrace,
                              title: "❌ Falha ao obter visualização",
                              statusCode: 500);
    }
})
.WithName("GetVisualizacaoUsuario")
.WithSummary("Obtém o status de visualização de uma mensagem entre usuários")
.WithOpenApi();

// ============================
// Número de Mensagens Não Lidas Entre Usuários
// Endpoint GET /getnummensagensnaolidasentreusuarios?idUsuario=...
// Retorna o número de mensagens não lidas entre usuários, agrupadas por remetente.
// ============================

app.MapGet("/getnummensagensnaolidasentreusuarios", async (int idUsuario) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
                MU.ID_REMETENTE        AS ID_REMETENTE,
                U.NOME                 AS NOME_REMETENTE,
                COUNT(*)               AS QTDE_NAO_LIDAS
            FROM MENSAGEM_USUARIO MU
            JOIN USUARIO U
              ON MU.ID_REMETENTE = U.ID_USUARIO
            WHERE
              MU.ID_DESTINATARIO = :IdUsuario
              AND MU.VISUALIZADA  = 0
            GROUP BY
              MU.ID_REMETENTE, U.NOME
            ORDER BY
              QTDE_NAO_LIDAS DESC";

        cmd.Parameters.Add(new OracleParameter("IdUsuario", idUsuario));

        var lista = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new
            {
                IdRemetente   = reader.GetInt32(0),
                NomeRemetente = reader.GetString(1),
                QtdeNaoLidas  = reader.GetInt32(2)
            });
        }

        if (lista.Count == 0)
            return Results.NotFound(new { message = "Nenhuma mensagem não lida encontrada para este usuário." });

        // <— não esqueça esse return!
        return Results.Ok(lista);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message + "\n" + ex.StackTrace,
            title: "❌ Falha ao obter número de mensagens não lidas",
            statusCode: 500
        );
    }
})
.WithName("GetNumMensagensNaoLidasEntreUsuarios")
.WithSummary("Obtém o número de mensagens não lidas entre usuários")
.WithOpenApi();


// ============================
// Número de Mensagens Não Lidas Entre Salas
// Endpoint GET /getnummensagensnaolidassalas?idSala=...
// Retorna o número de mensagens não lidas em uma sala, agrupadas por remetente
// ============================

app.MapGet("/getnummensagensnaolidassalas", async (int idUsuario) =>
{
    try
    {
        await using var conn = new OracleConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                MS.ID_SALA,
                S.NOME        AS NOME_SALA,
                COUNT(*)      AS NAO_VISTAS
            FROM 
                MENSAGEM_SALA MS
                JOIN SALA S 
                    ON MS.ID_SALA = S.ID_SALA
                JOIN PARTICIPACAO_SALA PS 
                    ON MS.ID_SALA = PS.ID_SALA 
                AND PS.ID_USUARIO = :idUsuario
                LEFT JOIN VISUALIZACAO_SALA VS 
                    ON MS.ID_MENSAGEM = VS.ID_MENSAGEM 
                AND VS.ID_USUARIO = :idUsuario
            WHERE 
                VS.ID_MENSAGEM IS NULL
            GROUP BY 
                MS.ID_SALA,
                S.NOME
            ORDER BY
                NAO_VISTAS DESC";

        cmd.Parameters.Add(new OracleParameter("IdUsuario", idUsuario));

        var lista = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new
            {
                IdSala = reader.GetInt32(0),
                NomeSala = reader.GetString(1),
                QtdeNaoLidas = reader.GetInt32(2)
            });
        }
        if (lista.Count == 0)
            return Results.NotFound(new { message = "Nenhuma mensagem não lida encontrada para esta sala." });
        return Results.Ok(lista);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message + "\n" + ex.StackTrace,
                title: "❌ Falha ao obter número de mensagens não lidas",
                statusCode: 500
            );
        }
})
.WithName("GetNumMensagensNaoLidasEntreSalas")
.WithSummary("Obtém o número de mensagens não lidas entre salas")
.WithOpenApi();

// ============================
// Enviar Mensagem Direta
// Endpoint POST /addmensagemusuario
// Insere nova mensagem na tabela MENSAGEM_USUARIO.
// ============================
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

// ============================
// Adicionar Participação em Sala
// Endpoint POST /addparticipacao
// Insere registro de participação de usuário em sala.
// ============================
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

// ============================
// Remover Participação em Sala
// Endpoint DELETE /removeparticipacao
// Remove registro de participação de usuário em sala.
// ============================
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

// ============================
// Login de Usuário
// Endpoint POST /login
// Valida credenciais e retorna dados do usuário autenticado.
// ============================
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

// ============================
// Bloquear Usuário
// Endpoint POST /bloqueiarusuario
// Insere registro de bloqueio entre dois usuários.
// ============================
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

// ============================
// Desbloquear Usuário
// Endpoint DELETE /desbloquearusuario
// Remove registro de bloqueio entre dois usuários.
// ============================
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


app.MapGet("/conversas", async (int idUsuario) =>
{
    await using var conn = new OracleConnection(connStr);
    await conn.OpenAsync();

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
WITH contatos AS (
    -- todo mundo com quem ele já conversou (enviou ou recebeu)
    SELECT DISTINCT ID_DESTINATARIO AS contato_id
    FROM MENSAGEM_USUARIO
    WHERE ID_REMETENTE = :idUsuario
  UNION
    SELECT DISTINCT ID_REMETENTE    AS contato_id
    FROM MENSAGEM_USUARIO
    WHERE ID_DESTINATARIO = :idUsuario
),
nao_lidas AS (
    -- conta quantas não lidas cada remetente deixou para ele
    SELECT ID_REMETENTE AS contato_id, COUNT(*) AS qtde
    FROM MENSAGEM_USUARIO
    WHERE ID_DESTINATARIO = :idUsuario
      AND VISUALIZADA      = 0
    GROUP BY ID_REMETENTE
)
SELECT
    c.contato_id             AS IdContato,
    u.NOME                  AS NomeContato,
    COALESCE(n.qtde, 0)     AS QtdeNaoLidas
FROM contatos c
JOIN USUARIO u 
  ON u.ID_USUARIO = c.contato_id
LEFT JOIN nao_lidas n
  ON n.contato_id = c.contato_id
ORDER BY NomeContato";

    cmd.Parameters.Add(new OracleParameter("idUsuario", idUsuario));

    var lista = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        lista.Add(new
        {
            IdContato = reader.GetInt32(0),
            NomeContato = reader.GetString(1),
            QtdeNaoLidas = reader.GetInt32(2)
        });
    }

    return Results.Ok(lista);
})
.WithName("GetConversasComNaoLidas")
.WithSummary("Todas as conversas de um usuário, com contagem de não-lidas (0 se todas lidas)")
.WithOpenApi();

app.MapGet("/salas-usuario", async (int idUsuario) =>
{
    await using var conn = new OracleConnection(connStr);
    await conn.OpenAsync();

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
WITH minhas AS (
    -- Salas em que ele participa
    SELECT S.ID_SALA, S.NOME, S.TIPO
    FROM SALA S
    JOIN PARTICIPACAO_SALA PS
      ON PS.ID_SALA    = S.ID_SALA
     AND PS.ID_USUARIO = :idUsuario
),
publicas_nao AS (
    -- Salas PUBLICAS em que ele NÃO participa
    SELECT S.ID_SALA, S.NOME, S.TIPO
    FROM SALA S
    WHERE S.TIPO = 'publica'
      AND NOT EXISTS (
        SELECT 1 FROM PARTICIPACAO_SALA PS
         WHERE PS.ID_SALA = S.ID_SALA
           AND PS.ID_USUARIO = :idUsuario
      )
),
nao_vistas AS (
    -- Quantas mensagens não vistas em cada sala
    SELECT MS.ID_SALA, COUNT(*) AS qtde
    FROM MENSAGEM_SALA MS
    JOIN PARTICIPACAO_SALA PS
      ON PS.ID_SALA    = MS.ID_SALA
     AND PS.ID_USUARIO = :idUsuario
    LEFT JOIN VISUALIZACAO_SALA VS
      ON VS.ID_MENSAGEM = MS.ID_MENSAGEM
     AND VS.ID_USUARIO  = :idUsuario
    WHERE VS.ID_MENSAGEM IS NULL
    GROUP BY MS.ID_SALA
)
SELECT
  m.ID_SALA    AS IdSala,
  m.NOME       AS NomeSala,
  m.TIPO       AS TipoSala,
  COALESCE(n.qtde, 0) AS QtdeNaoVistas,
  'minha'      AS Categoria
FROM minhas m
LEFT JOIN nao_vistas n
  ON n.ID_SALA = m.ID_SALA

UNION ALL

SELECT
  p.ID_SALA    AS IdSala,
  p.NOME       AS NomeSala,
  p.TIPO       AS TipoSala,
  0            AS QtdeNaoVistas,
  'publica_nao_participo' AS Categoria
FROM publicas_nao p

ORDER BY Categoria, NomeSala";

    cmd.Parameters.Add(new OracleParameter("idUsuario", idUsuario));

    var lista = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        lista.Add(new
        {
            IdSala = reader.GetInt32(0),
            NomeSala = reader.GetString(1),
            TipoSala = reader.GetString(2),
            QtdeNaoVistas = reader.GetInt32(3),
            Categoria = reader.GetString(4)  // "minha" ou "publica_nao_participo"
        });
    }

    return Results.Ok(lista);
})
.WithName("GetSalasUsuario")
.WithSummary("Salas que o usuário participa (com não-vistas) e públicas que não participa")
.WithOpenApi();

// 1) Junção interna (mensagens de usuário com nome de remetente)
app.MapGet("/report/internal-join", async () =>
{
    await using var conn = new OracleConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT 
          MU.ID_MENSAGEM        AS IdMensagem,
          MU.MENSAGEM           AS Mensagem,
          U.NOME                AS Remetente
        FROM MENSAGEM_USUARIO MU
        JOIN USUARIO U
          ON MU.ID_REMETENTE = U.ID_USUARIO";
    var list = new List<object>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        list.Add(new {
            IdMensagem = rdr.GetInt32(0),
            Mensagem   = rdr.GetString(1),
            Remetente  = rdr.GetString(2)
        });
    }
    return Results.Ok(list);
});

// 2) Junção externa (todos os usuários e, se houver, suas mensagens)
app.MapGet("/report/external-join", async () =>
{
    await using var conn = new OracleConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT 
          U.ID_USUARIO       AS IdUsuario,
          U.NOME             AS NomeUsuario,
          MU.ID_MENSAGEM     AS IdMensagem,
          MU.MENSAGEM        AS Mensagem
        FROM USUARIO U
        LEFT JOIN MENSAGEM_USUARIO MU
          ON U.ID_USUARIO = MU.ID_REMETENTE";
    var list = new List<object>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        list.Add(new {
            IdUsuario  = rdr.GetInt32(0),
            Nome       = rdr.GetString(1),
            IdMensagem = rdr.IsDBNull(2) ? null : rdr.GetInt32(2) as int?,
            Mensagem   = rdr.IsDBNull(3) ? null : rdr.GetString(3)
        });
    }
    return Results.Ok(list);
});

// 3) Agrupamento (total de mensagens por sala)
app.MapGet("/report/group-join", async () =>
{
    await using var conn = new OracleConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT 
          MS.ID_SALA     AS IdSala,
          S.NOME         AS NomeSala,
          COUNT(*)       AS TotalMensagens
        FROM MENSAGEM_SALA MS
        JOIN SALA S
          ON MS.ID_SALA = S.ID_SALA
        GROUP BY MS.ID_SALA, S.NOME";
    var list = new List<object>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        list.Add(new {
            IdSala           = rdr.GetInt32(0),
            NomeSala         = rdr.GetString(1),
            TotalMensagens   = rdr.GetInt32(2)
        });
    }
    return Results.Ok(list);
});

// 4) HAVING (salas com mais de 2 mensagens)
app.MapGet("/report/group-having", async () =>
{
    await using var conn = new OracleConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT 
          MS.ID_SALA     AS IdSala,
          S.NOME         AS NomeSala,
          COUNT(*)       AS TotalMensagens
        FROM MENSAGEM_SALA MS
        JOIN SALA S
          ON MS.ID_SALA = S.ID_SALA
        GROUP BY MS.ID_SALA, S.NOME
        HAVING COUNT(*) > 2";
    var list = new List<object>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        list.Add(new {
            IdSala         = rdr.GetInt32(0),
            NomeSala       = rdr.GetString(1),
            TotalMensagens = rdr.GetInt32(2)
        });
    }
    return Results.Ok(list);
});

// 5) Consulta aninhada (usuários que enviaram mais que a média de mensagens)
app.MapGet("/report/nested", async () =>
{
    await using var conn = new OracleConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT U.ID_USUARIO    AS IdUsuario,
               U.NOME          AS NomeUsuario,
               T.TOTAL         AS TotalEnvios
        FROM USUARIO U
        JOIN (
          SELECT ID_REMETENTE AS Id,
                 COUNT(*)      AS TOTAL
          FROM MENSAGEM_USUARIO
          GROUP BY ID_REMETENTE
        ) T
          ON U.ID_USUARIO = T.Id
        WHERE T.TOTAL > (
          SELECT AVG(cnt) 
          FROM (
            SELECT COUNT(*) AS cnt
            FROM MENSAGEM_USUARIO
            GROUP BY ID_REMETENTE
          )
        )";
    var list = new List<object>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        list.Add(new {
            IdUsuario    = rdr.GetInt32(0),
            NomeUsuario  = rdr.GetString(1),
            TotalEnvios  = rdr.GetInt32(2)
        });
    }
    return Results.Ok(list);
});

app.Run();

