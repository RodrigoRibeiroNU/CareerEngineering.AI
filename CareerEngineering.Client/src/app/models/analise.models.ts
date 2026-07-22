/** Item leve da Sidebar (sem textos pesados). */
export interface AnaliseListItem {
  id: string;
  titulo: string;
  dataCriacao: string;
  modeloLLM: string;
}

export interface MensagemHistorico {
  id: string;
  role: 'system' | 'user' | 'assistant' | string;
  conteudo: string;
  dataEnvio: string;
}

export interface AnaliseDetail {
  id: string;
  titulo: string;
  descricaoVaga: string;
  textoCurriculo: string;
  modeloLLM: string;
  dataCriacao: string;
  mensagens: MensagemHistorico[];
}

/** Mensagem exibida na UI (pode ser local/streaming). */
export interface ChatMessageView {
  id: string;
  role: 'user' | 'assistant' | 'system';
  conteudo: string;
  streaming?: boolean;
}
