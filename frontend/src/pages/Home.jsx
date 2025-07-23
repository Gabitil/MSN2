import { useEffect, useState } from "react";
import { api } from "../api";
import { useNavigate } from "react-router-dom";
import ConversationList from "../components/ConversationList";
import SalaList from "../components/SalaList";
import { Link } from "react-router-dom";



export default function Home({ user }) {
  const [conversas, setConversas] = useState([]);
  const [salas, setSalas] = useState([]);
  const navigate = useNavigate();
  const [joinRoom, setJoinRoom] = useState(null); // sala que o usuário quer entrar
  const [showModal, setShowModal] = useState(false);

  useEffect(() => {
    if (!user?.id) return;

  // Conversas (todas + nao-lidas)
  api.get("/conversas", { params: { idUsuario: user.id } })
     .then(res => setConversas(res.data));

// Salas (minhas e públicas não participadas)
api.get("/salas-usuario", { params: { idUsuario: user.id } })
   .then(res => {
     const minhas = res.data.filter(s => s.categoria === "minha");
     const outras = res.data.filter(s => s.categoria === "publica_nao_participo");
     setSalas([
       { titulo: "Minhas Salas", itens: minhas },
       { titulo: "Salas Públicas", itens: outras },
     ]);
   });
    }, [user]);

  return (
    <div>
      <h1>Bem-vindo, {user.nome}</h1>

            {/* Se for admin, mostra o link */}
        {user.privilegios === "admin" && (
            <div className="mt-2">
            <Link
                to="/admin"
                className="inline-block bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700"
            >
                Painel Admin
            </Link>
            </div>
        )}

      <section className="mt-6">
        <h2 className="text-lg font-semibold mb-2">
          Conversas ({conversas.length})
        </h2>
        <ul className="space-y-2">
          {conversas.map((c) => (
            <li
              key={c.idContato}
              role= "button"
              tabIndex={0}
              className="flex justify-between border p-2 rounded hover:bg-gray-100 cursor-pointer"
              onClick={() => navigate(`/conversa/${c.idContato}`)}
            >
              <span>{c.nomeContato}</span>
             {c.qtdeNaoLidas > 0 && (
               <span className="text-red-600 font-medium">
                 {c.qtdeNaoLidas} não lidas
               </span>
             )}
           </li>
         ))}
        </ul>
      </section>

      {/* Seções de Salas */}
      {salas.map((group) => (
        <section key={group.titulo} className="mt-6">
          <h2 className="text-lg font-semibold mb-2">
            {group.titulo} ({group.itens.length})
          </h2>
          <ul className="space-y-2">
            {group.itens.map((sala) => (
                <li
                    key={sala.idSala}
                    role="button"
                    tabIndex={0}
                    className="flex justify-between border p-2 rounded hover:bg-gray-100 cursor-pointer"
                    onClick={() => {
                        setJoinRoom(sala);
                        setShowModal(true);
                    }}
                >
                    <span>{sala.nomeSala}</span>
                    {sala.qtdeNaoVistas > 0 && (
                        <span className="text-red-600 font-medium">
                            {sala.qtdeNaoVistas} não lidas
                        </span>
                    )}
                </li>
            ))}
          </ul>
        </section>
      ))}
      
      {showModal && joinRoom && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
            <div className="bg-white p-6 rounded shadow-lg max-w-sm">
            <h3 className="text-lg font-semibold mb-4">
                Entrar na sala “{joinRoom.nome}”?
            </h3>
            <div className="flex justify-end space-x-2">
                <button
                className="px-4 py-2 rounded border"
                onClick={() => {
                    setShowModal(false);
                    setJoinRoom(null);
                }}
                >
                Cancelar
                </button>
                <button
                className="px-4 py-2 rounded bg-blue-600 text-white"
                onClick={async () => {
                    // 1) chama o endpoint de participação
                    await api.post("/addparticipacao", null, {
                    params: {
                        idSala: joinRoom.idSala,
                        idUsuario: user.id
                    }
                    });
                    // 2) fecha o modal e navega pro chat da sala
                    setShowModal(false);
                    navigate(`/sala/${joinRoom.idSala}`);
                }}
                >
                Entrar
                </button>
            </div>
            </div>
        </div>
        )}

        <button onClick={() => navigate("/nova-conversa")} className="mb-4 p-2 bg-blue-500 text-white rounded">
          Nova Conversa
        </button>

        <button onClick={() => navigate("/nova-sala")} className="mb-4 p-2 bg-green-500 text-white rounded">
           + Nova Sala
        </button>
    </div>
  );
}
