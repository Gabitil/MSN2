// src/components/SalaList.jsx
import React from "react";

export default function SalaList({ groups }) {
  return (
    <div>
      {groups.map(({ titulo, itens }) => (
        <div key={titulo} className="mb-6">
          {/* Exibe o título + quantidade */}
          <h2 className="text-lg font-semibold mb-2">
            {titulo} ({itens.length})
          </h2>
          <ul className="space-y-2">
            {itens.map((s) => (
              <li
                key={s.idSala}
                className="flex justify-between border p-2 rounded hover:bg-gray-100 cursor-pointer"
              >
                <span>
                  {s.nomeSala} <em className="text-gray-500">({s.tipoSala})</em>
                </span>
                {s.qtdeNaoVistas > 0 && (
                  <span className="text-red-600 font-medium">
                    ({s.qtdeNaoVistas}) não lidas
                  </span>
                )}
              </li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
}
