import React, { useState } from "react";
import { useNavigate }    from "react-router-dom";
import { api }            from "../api";

export default function NovaSala({ user }) {
  const [nome, setNome]     = useState("");
  const [tipo, setTipo]     = useState("publica");
  const [erro, setErro]     = useState("");
  const navigate            = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!nome.trim()) {
      setErro("Digite um nome para a sala.");
      return;
    }

    try {
      await api.post("/addsala", null, {
        params: {
          nome,
          tipo,
          criador: user.id
        }
      });
      // redireciona direto para o chat da nova sala
      // (supondo que a API atribui ID incremental, você pode buscar a sala criada ou simplesmente voltar à lista)
      navigate("/");
    } catch (err) {
      console.error("Falha ao criar sala", err);
      setErro("Não foi possível criar a sala. Tente novamente.");
    }
  };

  return (
    <div className="max-w-sm mx-auto mt-20 p-6 border rounded-xl shadow-xl">
      <button
        className="mb-4 text-blue-600 hover:underline"
        onClick={() => navigate(-1)}
      >
        ← Voltar
      </button>

      <h2 className="text-2xl font-bold mb-4">Criar Nova Sala</h2>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block mb-1 font-semibold">Nome da Sala</label>
          <input
            type="text"
            value={nome}
            onChange={e => setNome(e.target.value)}
            className="w-full border p-2 rounded"
            placeholder="Ex: Chat Geral"
          />
        </div>

        <div>
          <label className="block mb-1 font-semibold">Tipo</label>
          <select
            value={tipo}
            onChange={e => setTipo(e.target.value)}
            className="w-full border p-2 rounded"
          >
            <option value="publica">Pública</option>
            <option value="privada">Privada</option>
          </select>
        </div>

        {erro && <p className="text-red-500">{erro}</p>}

        <button
          type="submit"
          className="w-full bg-green-600 text-white p-2 rounded hover:bg-green-700"
        >
          Criar Sala
        </button>
      </form>
    </div>
  );
}
