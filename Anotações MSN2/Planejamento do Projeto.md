# 📁 Projeto: MSN2 – Sistema de Chat Online

## 🧠 Modo de usar:

Marque cada item com - [x] quando concluir

Se quiser, adicione tags como #feito, #andamento, #importante

---

## 🟣 Etapa 1 – Documentação e Planejamento Inicial

- [x] Definir nome do projeto (`msn2`)
- [x] Definir tema e registrar no AVA
- [ ] Escrever universo de discurso do banco (30 a 60 linhas)
- [x] Criar MER no Dia
- [ ] Explicar o DER no PDF final

---

## 🟡 Etapa 2 – Banco de Dados Relacional

- [x] Criar tabelas no Oracle
- [ ] Verificar integridade das relações com `FOREIGN KEY`
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
- [ ] Configurar conexão com banco Oracle
- [ ] Testar conexão com Oracle (Dapper / EF / ADO.NET)
- [ ] Criar modelos (`/Models`)
- [ ] Criar DTOs (`/DTOs`)

### 🔁 Endpoints CRUD

#### Usuário
- [ ] GET todos
- [ ] GET por id
- [ ] POST (criar)
- [ ] PUT (editar)
- [ ] DELETE (remover)

#### Sala
- [ ] GET todos
- [ ] GET por id
- [ ] POST
- [ ] PUT
- [ ] DELETE

#### Mensagem
- [ ] GET todas
- [ ] GET por sala
- [ ] POST nova mensagem

#### Participação
- [ ] POST entrar em sala
- [ ] DELETE sair da sala

#### Bloqueio
- [ ] POST bloquear
- [ ] DELETE desbloquear

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
- [ ] Instalar dependências (Axios, React Router)
- [ ] Criar `.env` com URL da API
- [ ] Configurar rotas

### 👤 Telas de Usuário

- [ ] Login/Cadastro
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
