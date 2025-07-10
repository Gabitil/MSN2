using Oracle.ManagedDataAccess.Client;
using Microsoft.AspNetCore.Mvc; 


string walletPath = Path.Combine(AppContext.BaseDirectory, "Wallet_BDEC2");
OracleConfiguration.TnsAdmin = walletPath;
OracleConfiguration.WalletLocation = walletPath;

// Importa as funcionalidades de controle de rotas e model binding do ASP.NET Core

// Cria o builder da aplicação, carregando configurações, serviços e dependências
var builder = WebApplication.CreateBuilder(args);

// Configura o serviço de CORS (Cross-Origin Resource Sharing)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        // Permite que o frontend (React em localhost:3000) faça requisições a esta API
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()   // Autoriza qualquer cabeçalho HTTP
              .AllowAnyMethod();  // Autoriza qualquer método HTTP (GET, POST, etc.)
    });
});

// Adiciona serviços para gerar documentação OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Constrói a aplicação com as configurações acima
var app = builder.Build();

// Ativa o CORS usando a política configurada
app.UseCors("PermitirFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();      // Gera o JSON do Swagger
    app.UseSwaggerUI();    // Fornece a interface web para visualizar e testar a API
}

// Redireciona todas as requisições HTTP para HTTPS
app.UseHttpsRedirection();

using (var conn = new OracleConnection(Configuration["ConnectionStrings:OracleDb"]))
{
    conn.Open();
    var cmd = new OracleCommand("SELECT * FROM USUARIO", conn);
    using (var reader = cmd.ExecuteReader())
    {
        while (reader.Read())
        {
            Console.WriteLine(reader.GetString(0));
        }
    }
}


