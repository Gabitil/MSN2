// src/pages/NovaConversa.jsx
import React, { useState } from "react";
import { api } from "../api";
import { useNavigate } from "react-router-dom";

export default function NovaConversa({ user }) {
  const [email, setEmail]       = useState("");
  const [mensagem, setMensagem] = useState("");
  const [erro, setErro]         = useState("");
  const [sucesso, setSucesso]   = useState("");
  const navigate                = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErro("");
    setSucesso("");

    try {
      // 1) Busca usuário pelo email
      const resUser = await api.get("/getusuario", {
        params: { termo: email }
      });
      if (!resUser.data.length) {
        setErro("Usuário não encontrado.");
        return;
      }
      console.log("Usuário encontrado:", resUser.data[0]);
      const destinatario = resUser.data[0].id;

      // 2) Envia mensagem direto
      await api.post("/addmensagemusuario", null, {
        params: {
          idRemetente: user.id,
          idDestinatario: destinatario,
          mensagem: mensagem
        }
      });

      setSucesso("Mensagem enviada com sucesso!");
      // opcional: volta pra home após 1s
      setTimeout(() => navigate("/"), 1000);
    } catch (err) {
      console.error(err);
      setErro("Falha ao enviar a mensagem. Tente novamente.");
    }
  };

  return (
    <div className="max-w-md mx-auto mt-12 p-6 border rounded-xl shadow-xl">
      <h2 className="text-2xl font-semibold mb-4">Nova Conversa</h2>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block mb-1">E-mail do usuário:</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full border p-2 rounded"
            placeholder="usuario@exemplo.com"
            required
          />
        </div>

        <div>
          <label className="block mb-1">Mensagem:</label>
          <textarea
            value={mensagem}
            onChange={(e) => setMensagem(e.target.value)}
            className="w-full border p-2 rounded"
            rows={4}
            placeholder="Digite sua mensagem..."
            required
          />
        </div>

        {erro && <p className="text-red-600">{erro}</p>}
        {sucesso && <p className="text-green-600">{sucesso}</p>}

        <div className="flex justify-between">
          <button
            type="button"
            className="px-4 py-2 border rounded hover:bg-gray-100"
            onClick={() => navigate(-1)}
          >
            Cancelar
          </button>
          <button
            type="submit"
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
          >
            Enviar
          </button>
        </div>
      </form>
    </div>
  );
}
