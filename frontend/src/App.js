import React, { useState, useEffect } from "react";
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import axios from "axios";

const api = axios.create({ baseURL: "http://localhost:5287" });

function Login({ onLogin }) {
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [erro, setErro] = useState(null);

  const handleLogin = async () => {
    try {
      const res = await api.post("/login", null, {
        params: { email, senha },
      });
      console.log("Resposta do login:", res.data);
      onLogin(res.data);
    } catch (err) {
      setErro("Email ou senha incorretos");
    }
  };

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

function Home({ user }) {
  const [conversas, setConversas] = useState([]);
  const [salas, setSalas] = useState([]);
  const [mensagens, setMensagens] = useState([]);

  useEffect(() => {
    async function fetchConversas() {
      const res = await api.get("/getmensagemenviadas", { params: { id: user.Id } });
      const unicos = [...new Set(res.data.map((m) => m.IdDestinatario))];
      setConversas(unicos);
    }

    async function fetchSalas() {
      const res = await api.get("/getsalas");
      const todas = res.data;
      const participacoes = await api.get("/getmensagemrecebidas", { params: { id: user.Id } });
      const idsSalas = participacoes.data
        .filter((m) => m.Tipo === "SALA")
        .map((m) => m.IdSala);
      const minhas = todas.filter((s) => idsSalas.includes(s.Id));
      const outras = todas.filter((s) => s.Tipo === "publica" && !idsSalas.includes(s.Id));
      setSalas([ { titulo: "Minhas Salas", itens: minhas }, { titulo: "Salas Públicas", itens: outras } ]);
    }

    fetchConversas();
    fetchSalas();
  }, [user]);

  return (
    <div className="p-6">
      <h1 className="text-xl font-bold">Bem-vindo, {user.Nome}!</h1>
      <div className="grid grid-cols-2 gap-6 mt-6">
        <div>
          <h2 className="text-lg font-semibold mb-2">Conversas</h2>
          <ul className="space-y-2">
            {conversas.map((id) => (
              <li key={id} className="border p-2 rounded hover:bg-gray-100 cursor-pointer">
                Usuário #{id}
              </li>
            ))}
          </ul>
        </div>
        <div>
          {salas.map((grupo) => (
            <div key={grupo.titulo} className="mb-4">
              <h2 className="text-lg font-semibold mb-2">{grupo.titulo}</h2>
              <ul className="space-y-2">
                {grupo.itens.map((sala) => (
                  <li
                    key={sala.Id}
                    className="border p-2 rounded hover:bg-gray-100 cursor-pointer"
                  >
                    {sala.Nome} ({sala.Tipo})
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function App() {
  const [usuario, setUsuario] = useState(null);

  return (
    <Router>
      <Routes>
        <Route
          path="/"
          element={usuario ? <Home user={usuario} /> : <Login onLogin={setUsuario} />}
        />
        {/* Aqui você poderá colocar mais rotas como /adm, /sala/:id, /usuario/:id */}
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </Router>
  );
}

export default App;