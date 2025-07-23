// src/components/ConversationList.jsx
import React from "react";




export default function ConversationList({ items }) {
  return (
    <div>
      {/* Título com o contador */}
      <h2 className="text-lg font-semibold mb-2">
        Conversas ({items.length})
      </h2>

      <ul className="space-y-2">
        {items.length === 0 && (
          <li className="text-gray-500">Você ainda não conversou com ninguém.</li>
        )}

        {items.map((c) => (
          <li
            key={c.idContato}
            className="flex justify-between border p-2 rounded hover:bg-gray-100 cursor-pointer"
          >
            <span >{c.nomeContato} </span>
            {c.qtdeNaoLidas > 0 && (
              <span className="text-red-600 font-medium">
                ({c.qtdeNaoLidas}) não lidas
              </span>
            )}
          </li>
        ))}
      </ul>
    </div>
  );
}
