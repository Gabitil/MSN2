import React, { useState } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "./pages/Login";
import Home from "./pages/Home";
import Admin from "./pages/Admin";
import NovaConversa from './pages/NovaConversa';
import Conversa from './pages/Conversa';
import SalaChat from './pages/SalaChat';
import NovaSala from './pages/NovaSala'; // Importa a nova página de sala

function App() {
  const [user, setUser] = useState(null);

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={
          user ? <Home user={user}/> : <Login onLogin={setUser}/>
        }/>
        <Route path="/nova-conversa" element={
          user ? <NovaConversa user={user}/> : <Navigate to="/" replace/>
        }/>
        <Route path="/nova-sala" element={
          user ? <NovaSala user={user}/> : <Navigate to="/" replace/>
        }/>
        <Route path="/conversa/:contatoId" element={
          user ? <Conversa user={user}/> : <Navigate to="/" replace/>
        }/>
        <Route path="/sala/:salaId" element={
          user ? <SalaChat user={user}/> : <Navigate to="/" replace/>
        }/>
        {/* Rota de admin, só acessível se o usuário for admin */}
        <Route path="/admin" element={
          user?.privilegios === "admin"
            ? <Admin user={user}/>
            : <Navigate to="/" replace/>
        }/>
        <Route path="*" element={<Navigate to="/" replace/>}/>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
