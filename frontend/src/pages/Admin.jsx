// src/pages/Admin.jsx
import React, { useEffect, useState } from "react";
import { api } from "../api";

export default function Admin() {
  const [intJoin,   setIntJoin]   = useState([]);
  const [extJoin,   setExtJoin]   = useState([]);
  const [groupJoin, setGroupJoin] = useState([]);
  const [having,    setHaving]    = useState([]);
  const [nested,    setNested]    = useState([]);

  useEffect(() => {
    api.get("/report/internal-join")
      .then(r => setIntJoin(r.data))
      .catch(console.error);

    api.get("/report/external-join")
      .then(r => setExtJoin(r.data))
      .catch(console.error);

    api.get("/report/group-join")
      .then(r => setGroupJoin(r.data))
      .catch(console.error);

    api.get("/report/group-having")
      .then(r => setHaving(r.data))
      .catch(console.error);

    api.get("/report/nested")
      .then(r => setNested(r.data))
      .catch(console.error);
  }, []);

  function renderTable(data) {
    if (!data || data.length === 0) {
      return <p className="text-gray-500 italic">Sem dados para exibir.</p>;
    }

    // Colunas inferidas da primeira linha
    const cols = Object.keys(data[0]);

    return (
      <table className="min-w-full border mb-6">
        <thead className="bg-gray-100">
          <tr>
            {cols.map(c => (
              <th key={c} className="p-2 border text-left">
                {c}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((row, i) => (
            <tr key={i} className={i % 2 === 0 ? "bg-white" : "bg-gray-50"}>
              {cols.map(c => (
                <td key={c} className="p-2 border">
                  {row[c] ?? "-"}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    );
  }

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Painel Administrativo</h1>

      <section>
        <h2 className="text-xl font-semibold mb-2">1. Junção Interna</h2>
        {renderTable(intJoin)}
      </section>

      <section>
        <h2 className="text-xl font-semibold mb-2">2. Junção Externa</h2>
        {renderTable(extJoin)}
      </section>

      <section>
        <h2 className="text-xl font-semibold mb-2">3. Agrupamento por Sala</h2>
        {renderTable(groupJoin)}
      </section>

      <section>
        <h2 className="text-xl font-semibold mb-2">4. Salas com &gt;2 Mensagens</h2>
        {renderTable(having)}
      </section>

      <section>
        <h2 className="text-xl font-semibold mb-2">5. Usuários Acima da Média</h2>
        {renderTable(nested)}
      </section>
    </div>
  );
}
