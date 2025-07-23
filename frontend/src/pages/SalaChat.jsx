// src/pages/SalaChat.jsx
import React, { useEffect, useState, useRef } from "react";
import { useParams, useNavigate }            from "react-router-dom";
import { api }                                from "../api";

export default function SalaChat({ user }) {
  const { salaId }                = useParams();
  const navigate                  = useNavigate();
  const [salaNome, setSalaNome]   = useState("");
  const [salaCriador, setSalaCriador] = useState(null);
  const [mensagensSala, setMensagensSala] = useState([]);
  const [texto, setTexto]         = useState("");
  const [showAddUserModal, setShowAddUserModal] = useState(false);
  const [emailNovoUsuario, setEmailNovoUsuario] = useState("");
  const [erroAdd, setErroAdd]                   = useState("");
  const bottomRef                 = useRef(null);
  

  // 1) Buscar dados da sala (nome + criador)
  useEffect(() => {
    async function fetchSala() {
      try {
        const res = await api.get("/getsala", { params: { termo: salaId } });
        if (res.data.length > 0) {
          // seu endpoint retorna [{ Id, Nome, ... }]
          setSalaNome(res.data[0].Nome || res.data[0].nome);
          setSalaCriador(res.data[0].Criador || res.data[0].criador);
        }
      } catch (err) {
        console.error("Erro ao buscar sala:", err);
      }
    }
    fetchSala();
  }, [salaId]);

  // 2) Carregar histórico da sala
  useEffect(() => {
    if (!user?.id) return;

    async function carregarSala() {
      try {
        // usa getmensagensporid e filtra só tipo SALA
        const { data } = await api.get("/getmensagenssala", {
          params: { idSala: salaId }
        });
        const salaMsgs = data
          .filter(m => m.tipo === "SALA")
          .sort((a, b) => new Date(a.dataEnvio) - new Date(b.dataEnvio));
        setMensagensSala(salaMsgs);
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
      } catch (err) {
        console.error("Falha ao carregar mensagens da sala:", err);
      }
    }
    carregarSala();
  }, [user, salaId]);

  // 3) Enviar nova mensagem na sala
  const handleSubmit = async e => {
    e.preventDefault();
    if (!texto.trim()) return;
    try {
      await api.post("/addmensagemsala", null, {
        params: {
          idSala: salaId,
          idRemetente: user.id,
          mensagem: texto
        }
      });
      // otimista: adicionar localmente
      setMensagensSala(prev => [
        ...prev,
        {
          IdMensagem: Date.now(),
          IdSala: +salaId,
          IdRemetente: user.id,
          Mensagem: texto,
          DataEnvio: new Date().toISOString(),
          Tipo: "SALA"
        }
      ]);
      setTexto("");
      bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    } catch (err) {
      console.error("Falha ao enviar mensagem na sala:", err);
      alert("Erro ao enviar, tente novamente.");
    }
  };

    // 4) Convidar novo usuário
  const handleAddUser = async () => {
    setErroAdd("");
    if (!emailNovoUsuario.trim()) {
      setErroAdd("Informe o email");
      return;
    }
    try {
      // 4.1) buscar ID pelo email
      const resUser = await api.get("/getusuario", {
        params: { termo: emailNovoUsuario }
      });
      if (!resUser.data.length) {
        setErroAdd("Usuário não encontrado");
        return;
      }
      const idUsuario = resUser.data[0].Id;

      // 4.2) chamar endpoint de participação
      await api.post("/addparticipacao", null, {
        params: { idSala: salaId, idUsuario }
      });

      // tudo certo: fecha modal
      setShowAddUserModal(false);
      setEmailNovoUsuario("");
    } catch (err) {
      console.error("Erro ao adicionar usuário:", err);
      setErroAdd("Falha ao adicionar");
    }
  };

  return (
    <div className="max-w-md mx-auto mt-6 p-4 border rounded">
      <button
        className="mb-4 text-blue-600 hover:underline"
        onClick={() => navigate(-1)}
      >
        ← Voltar
      </button>

      <h2 className="text-xl font-semibold mb-4">
        Sala: {salaNome}
      </h2>
      {/* Só o criador vê este botão */}
        {user.id === salaCriador && (
          <button
            className="bg-green-600 text-white px-3 py-1 rounded hover:bg-green-700 text-sm"
            onClick={() => setShowAddUserModal(true)}
          >
            Adicionar Usuário
          </button>
        )}
     
    <div className="h-80 overflow-y-auto border p-2 mb-4">
    {mensagensSala.map((m) => {
        const isOwn = m.idRemetente === user.id;  // ou m.IdRemetente, dependendo da sua resposta
        return (
        <div
            key={m.idMensagem}
            className={`flex mb-2 ${isOwn ? "justify-end" : "justify-start"}`}
        >
            <div
            className={`
                max-w-[70%] p-2 rounded
                ${isOwn ? "bg-blue-100 text-right" : "bg-gray-200 text-left"}
            `}
            >
            {/* 1) Nome do remetente acima da mensagem */}
            <div className="font-semibold text-sm mb-1">
                {m.nomeRemetente /* ou m.nomeRemetente */}
            </div>

            {/* 2) Texto da mensagem */}
            <div>{m.mensagem /* ou m.mensagem */}</div>

            {/* 3) Timestamp */}
            <div className="text-xs text-gray-500 mt-1">
                {new Date(m.dataEnvio || m.DataEnvio).toLocaleString()}
            </div>
            </div>
        </div>
        );
    })}
    <div ref={bottomRef} />
    </div>


      <form onSubmit={handleSubmit} className="flex gap-2">
        <input
          type="text"
          value={texto}
          onChange={e => setTexto(e.target.value)}
          placeholder="Digite sua mensagem na sala..."
          className="flex-1 border p-2 rounded"
        />
        <button
          type="submit"
          className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
        >
          Enviar
        </button>
      </form>
        {showAddUserModal && (
            <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
            <div className="bg-white p-6 rounded shadow-lg max-w-sm">
                <h3 className="text-lg font-semibold mb-4">Adicionar Usuário</h3>
                <input
                type="email"
                value={emailNovoUsuario}
                onChange={e => setEmailNovoUsuario(e.target.value)}
                placeholder="Email do usuário"
                className="w-full border p-2 rounded mb-2"
                />
                {erroAdd && <p className="text-red-500 mb-2">{erroAdd}</p>}
                <div className="flex justify-end space-x-2">
                <button
                    className="px-4 py-2 rounded border"
                    onClick={() => setShowAddUserModal(false)}
                >
                    Cancelar
                </button>
                <button
                    className="px-4 py-2 rounded bg-green-600 text-white"
                    onClick={handleAddUser}
                >
                    Adicionar
                </button>
                </div>
            </div>
            </div>
        )}
    </div>
  );
}
