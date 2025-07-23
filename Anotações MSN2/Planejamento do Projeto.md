# 📁 Projeto: MSN2 – Sistema de Chat Online

## 🧠 Modo de usar:

Marque cada item com - [x] quando concluir

Se quiser, adicione tags como #feito, #andamento, #importante

---

## 🟣 Etapa 1 – Documentação e Planejamento Inicial

- [x] Definir nome do projeto (`msn2`)
- [x] Definir tema e registrar no AVA
- [x] Escrever universo de discurso do banco (30 a 60 linhas)
- [x] Criar MER no Dia
- [x] Explicar o DER no PDF final

---

## 🟡 Etapa 2 – Banco de Dados Relacional

- [x] Criar tabelas no Oracle
- [x] Verificar integridade das relações com `FOREIGN KEY`
- [ ] Criar *views* úteis (opcional)
- [x] Criar **5 triggers**:
  - [x] Impedir mensagens em salas privadas sem participação
  - [x] Atualizar `data_entrada` ao entrar em sala
  - [x] Registrar `data_bloqueio` ao bloquear usuário
  - [x] Impedir envio de mensagem por usuários bloqueados
  - [x] Trigger de auditoria (login ou envio)
- [x] Povoar o banco (mínimo 5 registros por tabela)

---

## 🔵 Etapa 3 – Backend (.NET API)

### ⚙️ Configuração Inicial

- [x] Criar estrutura do projeto .NET
- [x] Configurar conexão com banco Oracle
- [x] Testar conexão com Oracle (Dapper / EF / ADO.NET)
- [ ] Criar modelos (`/Models`)
- [ ] Criar DTOs (`/DTOs`)

### 🔁 Endpoints CRUD

#### Usuário
- [x] GET todos
- [x] GET por id
- [x] POST (criar)
- [x] PUT (editar)
- [x] DELETE (remover)

#### Sala
- [x] GET todos
- [x] GET por id
- [x] POST
- [x] PUT
- [x] DELETE

#### Mensagem
- [x] GET todas
- [x] GET por mensagens eviadas
- [x] GET por mensagens recebidas
- [x] POST nova mensagem

#### Participação
- [x] POST entrar em sala
- [x] DELETE sair da sala

#### Bloqueio
- [x] POST bloquear
- [x] DELETE desbloquear

### 📊 Relatórios SQL

- [ ] Junção interna (ex: usuários + mensagens)
- [ ] Junção externa (ex: salas + participantes)
- [ ] `GROUP BY` (ex: mensagens por sala)
- [ ] `HAVING` (ex: salas com + de 10 mensagens)
- [ ] Subconsulta aninhada (ex: usuários acima da média de mensagens)

---

## 🔴 Etapa 4 – Front-End (React)

### ⚙️ Configuração

- [x] Criar projeto React
- [x] Instalar dependências (Axios, React Router)
- [x] Criar `.env` com URL da API
- [x] Configurar rotas

### 👤 Telas de Usuário

- [x] Login/Cadastro
- [ ] Lista de salas
- [ ] Criar nova sala
- [ ] Entrar/sair de sala
- [ ] Chat da sala (mensagens)
- [ ] Histórico da conversa

### 🔐 (Opcional) Telas de Administração

- [ ] Tela para bloquear/desbloquear usuários
- [ ] Visualização de logs (se tiver auditoria)

---

## 🟢 Etapa 5 – Finalização

- [ ] Revisar banco e exportar comandos SQL
- [ ] Capturar prints das saídas (banco e sistema)
- [ ] Escrever PDF explicando todas as etapas
- [ ] Gravar vídeo (5 a 10 min)
- [ ] Subir vídeo no YouTube ou similar
- [ ] Incluir link do vídeo no PDF
- [ ] Enviar PDF no AVA até **15/07/2025 às 23h59**

---

> ✅ Dica: Use tags como `#feito`, `#emprogresso`, `#bloqueado` se quiser controlar o progresso no Obsidian com busca e filtros!



## O que falta fazer

- **Usuário**

- [x] Botão nos nomes dos usuários e salas, que vai mandar o usuário para a conversa com essas salas e usuários.
- [x] Botão para começar uma nova mensagem (usuário tem que inserir o e-mail da pessoa que ele quer conversar);
- [x] Botão para o usuário criar uma sala.
- [ ] botão para usuário adicionar membro em uma sala.
- [x] Adicionar a notificação com a quantidade de mensagens não lidas;
	- [x] Criar as endpoints que fazem essa visualização

- **Admin**

- [ ] Botão que vai redirecionar ele para um Lugar para ver relatórios;