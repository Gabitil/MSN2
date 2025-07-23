// src/pages/Conversa.jsx
import React, { useEffect, useState, useRef } from "react";
import { useParams, useNavigate }            from "react-router-dom";
import { api }                                from "../api";

export default function Conversa({ user }) {
  const { contatoId }        = useParams();
  const navigate             = useNavigate();
  const [contatoNome, setContatoNome] = useState("");
  const [mensagens, setMensagens] = useState([]);
  const [texto, setTexto]         = useState("");
  const bottomRef                = useRef(null);

  useEffect(() => {
    async function fetchContato() {
      try {
        const res = await api.get("/getusuario", {
          params: { termo: contatoId }
        });
        if (res.data.length > 0) {
          // seu endpoint retorna um array de objetos { Id, Nome, ... }
          setContatoNome(res.data[0].Nome || res.data[0].nome);
        } else {
          setContatoNome("Desconhecido");
        }
      } catch {
        setContatoNome("Erro ao obter nome");
      }
    }

    fetchContato();
  }, [contatoId]);

  useEffect(() => {
    if (!user?.id) return;

    async function carregar() {
      try {
        // 1) mensagens que eu enviei a ele
        const { data: enviadas } = await api.get("/getmensagemenviadas", {
          params: { id: user.id }
        });
        // 2) mensagens que ele me enviou
        const { data: recebidas } = await api.get("/getmensagemrecebidas", {
          params: { id: user.id }
        });

        // filtra só as diretas (tipo USUARIO) e só entre eu e ele
        const diretasEnviadas = enviadas
          .filter(m => m.tipo === "USUARIO" && m.idDestinatario === +contatoId);
        const diretasRecebidas = recebidas
          .filter(m => m.tipo === "USUARIO" && m.idRemetente === +contatoId);

        const todas = [...diretasEnviadas, ...diretasRecebidas];
        // ordenar por data
        todas.sort((a, b) => new Date(a.dataEnvio) - new Date(b.dataEnvio));

        setMensagens(todas);
        // rola pro fim
        bottomRef.current?.scrollIntoView({ behavior: "smooth" });
      } catch(err) {
        console.error("Falha ao carregar conversa", err);
      }
    }

    carregar();
  }, [user, contatoId]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!texto.trim()) return;

    try {
      await api.post("/addmensagemusuario", null, {
        params: {
          idRemetente: user.id,
          idDestinatario: contatoId,
          mensagem: texto
        }
      });
      // otimista: adiciona já na tela
      setMensagens(prev => [
        ...prev,
        {
          idMensagem: Date.now(),
          idRemetente: user.id,
          idDestinatario: +contatoId,
          mensagem: texto,
          dataEnvio: new Date().toISOString(),
          tipo: "USUARIO"
        }
      ]);
      setTexto("");
      bottomRef.current?.scrollIntoView({ behavior: "smooth" });
    } catch(err) {
      console.error("Falha ao enviar mensagem", err);
      alert("Erro ao enviar, tente novamente.");
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
        <h2 className="text-2xl font-semibold mb-4">
          Conversa com {contatoNome}
        </h2>   

    <div className="h-80 overflow-y-auto border p-2 mb-4">
    {mensagens.map((m) => {
        const isOwn = m.idRemetente === user.id;
        return (
        <div
            key={m.idMensagem}
            className={`flex mb-2 ${isOwn ? "justify-end" : "justify-start"}`}
        >
            <div
            className={`
                max-w-[70%] p-2 rounded
                ${isOwn
                ? "bg-blue-100 text-right"     // suas mensagens azuis, texto alinhado à direita
                : "bg-gray-200 text-left"}      // do outro cinza, texto alinhado à esquerda
            `}
            >
            <div>{m.Mensagem || m.mensagem}</div>
            <div className="text-xs text-gray-500 mt-1">
                {new Date(m.DataEnvio || m.dataEnvio).toLocaleString()}
            </div>
            </div>
        </div>
        );
    })}
    <div ref={bottomRef} />
    </div>


      <form onSubmit={handleSubmit} className="mt-4 flex gap-2">
        <input
          type="text"
          value={texto}
          onChange={e => setTexto(e.target.value)}
          placeholder="Digite sua mensagem..."
          className="flex-1 border p-2 rounded"
        />
        <button
          type="submit"
          className="bg-blue-600 text-white px-4 rounded hover:bg-blue-700"
        >
          Enviar
        </button>
      </form>
    </div>
  );
}
