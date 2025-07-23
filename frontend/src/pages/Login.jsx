import { useState } from "react";
import { api } from "../api";

export default function Login({ onLogin }) {
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [erro, setErro] = useState("");

  async function handleLogin() {
    try {
      const res = await api.post("/login", null, { params: { email, senha } });
      console.log("Resposta do login:", res.data);
      onLogin(res.data);    // salva o usuário no estado geral
    } catch (err) {
    console.error("Erro ao fazer login:", err);
      if (err.response && err.response.status === 401) {
        setErro("Email ou senha inválidos.");
      } else {
        setErro("Ocorreu um erro ao tentar fazer login. Tente novamente.");
      }
      onLogin(null);
      setEmail("");
      setSenha("");
    }
  }

 return (
    <div className="max-w-sm mx-auto mt-20 p-6 border rounded-xl shadow-xl">
      <h2 className="text-2xl font-bold mb-4">Login</h2>
      <input
        type="email"
        placeholder="Email"
        className="w-full p-2 mb-2 border rounded"
        onChange={(e) => setEmail(e.target.value)}
      />
      <input
        type="password"
        placeholder="Senha"
        className="w-full p-2 mb-2 border rounded"
        onChange={(e) => setSenha(e.target.value)}
      />
      <button
        onClick={handleLogin}
        className="w-full bg-blue-600 text-white p-2 rounded hover:bg-blue-700"
      >
        Entrar
      </button>
      {erro && <p className="text-red-500 mt-2">{erro}</p>}
    </div>
  );
}
