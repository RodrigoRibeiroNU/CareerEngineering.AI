# 🚀 CareerEngineering.AI

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet.svg)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-22.0-red.svg)](https://angular.dev/)
[![Ollama](https://img.shields.io/badge/Ollama-Local_AI-black.svg?logo=ollama)](https://ollama.com/)
[![Semantic Kernel](https://img.shields.io/badge/AI--Powered-Semantic%20Kernel-blue.svg)](https://learn.microsoft.com/en-us/semantic-kernel/)
[![Auth0](https://img.shields.io/badge/Auth-Auth0-orange.svg)](https://auth0.com/)

O **CareerEngineering.AI** é um ecossistema inteligente de alta performance projetado para realizar análise automatizada e minuciosa de aderência profissional (Gap Analysis) combinada com um mentor de IA conversacional. O sistema confronta os requisitos técnicos de uma vaga de TI contra o perfil de um candidato, mapeando de forma precisa lacunas de competências divididas em Ferramentas, Metodologias e Certificações.

**Diferencial Arquitetural:** O motor de inteligência opera por meio de modelos *Open-Source* rodando **100% localmente via Ollama**, garantindo privacidade total dos dados sensíveis do currículo e custo zero de inferência. A entrega do resultado é feita em tempo real através de uma arquitetura orientada a eventos, streaming de tokens por WebSockets e persistência relacional completa de sessões.

---

## 🏗️ Arquitetura e Engenharia de Software Realizada

O sistema está estruturado em uma arquitetura desacoplada e robusta, suportando gerenciamento de sessões de chat, persistência em banco relacional e controle rigoroso de ciclo de vida de tokens de IA.

### 🖥️ Backend (.NET 10 & SignalR)
* **Orquestração GenAI Local (Semantic Kernel):** Consumo do modelo local via SDK oficial da Microsoft, permitindo isolamento de prompts e controle fino de hiperparâmetros.
* **Pipeline de Engenharia de Prompt em Duplo Estágio (Generator-Refiner):**
  1. **Estágio 1 (Extrator Bruto):** Avaliação analítica a nível de dicionário. Se o input do currículo for completamente desalinhado com a vaga, o pipeline dispara um gatilho de saída antecipada (`DIVERGENTE`), poupando hardware.
  2. **Estágio 2 (Refinador Consultivo):** Transforma a lista seca em um feedback estruturado rigidamente em 3 seções Markdown.
* **Streaming por WebSockets (SignalR):** Hub centralizado em `/careerChatHub` transmite tokens em tempo real (`StartAnalysis`). A autenticação do hub usa JWT via query string (`access_token`), com CORS restrito a `http://localhost:4200`.
* **Chat Multi-Turno & Defesas de Escopo:** Conversações continuadas na sessão ativa (`SendChatMessage`). Há um prompt âncora de personagem (Guardrails) reinjetado a cada turno; tentativas de fora de escopo/jailbreak são registradas em log (auditoria) e o modelo é orientado a recusar. `PresencePenalty`/`FrequencyPenalty` reduzem loops de repetição.
* **Atualização e Regeneração de Sessão:** `UpdateAnalysis` reexecuta o pipeline após editar vaga/currículo (preservando histórico e registrando aviso `system`). `RegenerateAnalysis` reprocessa análises órfãs sem mensagens.
* **Mecanismo de Sliding Window:** Para não estourar a memória (VRAM/RAM) do hardware local, o sistema mantém fixo o contexto âncora (Vaga/Currículo) e envia apenas as últimas 4 mensagens da sessão para o Ollama.
* **Gerenciamento Completo de Sessões (CRUD REST):** Endpoints HTTP protegidos via JWT fornecem listagem leve (`GET /api/analises`), detalhamento completo (`GET /api/analises/{id}`), renomeação de títulos (`PATCH .../title`) e deleção em cascata (`DELETE`).
* **Rotas do Sistema:** Endpoint `GET /api/System/active-model` expõe o `ModelId` configurado no Semantic Kernel (usado pelo badge da navbar).
* **Resiliência de Hardware:** Timeout assíncrono de **45 segundos** no primeiro estágio via `CancellationToken`, evitando travamentos por estrangulamento de RAM/VRAM. Guilhotina sintática corta o stream após a seção de Certificações.
* **Persistência Relacional Integrada (EF Core + SQL Server):** Mapeamento 1:N entre `Analises` (cabeçalho pesado de vaga/currículo) e `MensagensHistorico` (turnos do usuário e do assistente).
* **Documentação Técnica:** OpenAPI e Scalar expostos em ambiente de Development.

### 🎨 Frontend Reativo (Angular 22)
* **Controle de Estado Moderno (Angular Signals):** Gestão reativa de carregamento, listagem/mutação de sessões e consumo do stream SignalR.
* **Estrutura de Casca (Layout Shell):**
  * `NavbarComponent`: badge do modelo LLM ativo e dropdown de perfil/logout Auth0.
  * `SidebarComponent`: histórico cronológico com exclusão rápida e renomeação inline com update otimista.
* **Responsividade:** A Sidebar vira gaveteiro flutuante (overlay drawer) em telas menores (`md` para baixo), sem comprimir a área do chat.
* **Alternação Form vs Chat:** Painel central alterna entre formulário de preenchimento (`max-w-5xl`, alinhado à Landing) e janela de chat com streaming em tempo real.
* **Parser Client-Side de Documentos (Fase 6):** Extração de texto de `.pdf`, `.docx` e `.txt` no navegador via `DocumentParserService` (`pdfjs-dist` + `mammoth`), sem upload para o backend.
  * Componente `DocumentFieldComponent` nos cards **Descrição da Vaga** e **Seu Currículo**.
  * Barra de ferramentas: badge do arquivo importado → Colar → Importar (clipe) → Copiar → Limpar (lixeira).
  * Drag & drop com highlight visual, spinner durante a extração e toasts de erro/sucesso (`ToastService`).
  * Worker do PDF.js servido em `public/assets/pdf.worker.min.mjs`.
* **Atalhos do Chat Conversacional:** No textarea de follow-up, `Enter` envia a mensagem; `Ctrl+Enter` / `Shift+Enter` inserem nova linha. Dica de UX exibida abaixo do input.
* **Edição Contínua:** Reedição de vaga/currículo via `UpdateAnalysis`, registrando a transição como evento no chat sem limpar o histórico.
* **Segurança Baseada em Provedor:** Landing pública e rotas protegidas (`/analise` e `/analise/:id`) com `AuthGuard` Auth0. Interceptor HTTP injeta JWT nas requisições REST (`/api/*`); o SignalR usa `accessTokenFactory` na conexão do hub.
* **Configuração por Ambiente (Fase 8):** Frontend consome `environment.ts` / `environment.development.ts` (`apiUrl`, `hubUrl`, Auth0). Backend lê `Cors:AllowedOrigins` (REST + SignalR com `AllowCredentials`), `Ollama:*` e `ConnectionStrings` via appsettings ou variáveis de ambiente.

---

## 🛠️ Status do Roadmap de Desenvolvimento

O projeto encontra-se em estágio avançado de maturidade de sua fundação crítica, com módulos futuros bem definidos:

- [x] **Infraestrutura Básica de IA:** Configuração do Ollama local e homologação do modelo estável **Qwen 2.5 14B Instruct (Q4_K_M)**.
- [x] **Fundação de Interface:** Landing page pública, Dashboard de análise em Angular 22 e componentização reativa via Signals.
- [x] **Segurança:** Integração com **Auth0** (JWT no REST e no hub SignalR). No tenant de desenvolvimento, a aplicação opera com login social Google (configuração do Auth0, não do código).
- [x] **Pipeline de Análise Inteligente:** Mecanismo *Generator-Refiner* em C#, timeout de hardware, saída rápida para dados divergentes e guilhotina sintática de strings.
- [x] **Leitura e Gestão de Históricos (Fase 4):** Persistência no SQL Server exposta via API REST e listada dinamicamente na sidebar.
- [x] **Expansão Conversacional Multi-turno (Fase 5):** Chat contínuo com histórico estruturado, sliding window e guardrails de personagem no prompt.
- [x] **Fase 6 (Parser Físico de Documentos):** Extração client-side de `.pdf` / `.docx` / `.txt` (`DocumentParserService`), toolbar de importação/copiar/colar/limpar, drag & drop e feedback por toast.
- [x] **Atalhos de Teclado no Chat:** `Enter` para enviar; `Ctrl+Enter` / `Shift+Enter` para quebra de linha.
- [x] **Fase 7 (Renderização Rica):** Parser Markdown (`ngx-markdown` + `marked`) no chat do Dashboard para renderizar as seções do Refinador e respostas do assistente, com tipografia alinhada ao tema escuro e atualização reativa no streaming SignalR.
- [x] **Fase 8 (Configurações de Produção):** Environments Angular (`apiUrl` / `hubUrl` / Auth0) com `fileReplacements`; backend com `Cors:AllowedOrigins`, `Ollama:*` e `ConnectionStrings` sobrescrevíveis via `appsettings.*.json` ou variáveis de ambiente.
- [x] **Faxina de Código Depreciado:** Remoção do serviço obsoleto `CareerMentorService` e do componente órfão `AnalysisResultComponent`.

---

## 🚀 Requisitos e Como Executar Localmente

Diferente de aplicações web tradicionais, o ecossistema local exige a orquestração e configuração prévia de três pilares de infraestrutura.

### 📋 Pré-requisitos Obrigatórios
1. **Ollama:** Servidor de IA local instalado e rodando.
2. **SQL Server:** Instância ativa de banco de dados relacional.
3. **Auth0 Account:** Tenant/Application com Audience da API e, no ambiente atual de desenvolvimento, conexão **Google Social OAuth2** (demais conexões desativadas no dashboard do Auth0).

---

### 🏃 Compartilhamento de Passos para Inicialização

#### 1. Preparando o Cérebro de IA (Ollama)
Com o Ollama ativo em segundo plano na porta padrão 11434, baixe o modelo homologado executando o comando a seguir no terminal:

```bash
ollama run qwen2.5:14b
```

*(Nota de Hardware: O modelo exige cerca de 9.2 GB em disco. Em máquinas com GPUs de VRAM contida, o Ollama fará o transbordo automático de parte dos parâmetros para a memória RAM física, dividindo a carga de processamento).*

#### 2. Configurando as Variáveis e Executando a API (.NET)
1. Em desenvolvimento, preencha `CareerEngineering.Api/appsettings.Development.json` (`Auth0`, `Cors:AllowedOrigins`, `ConnectionStrings:DefaultConnection`, `Ollama`). Em produção, use `appsettings.Production.json` ou variáveis de ambiente (ex.: `ConnectionStrings__DefaultConnection`, `Cors__AllowedOrigins__0`, `Ollama__Endpoint`, `Auth0__Domain`, `Auth0__Audience`).
2. Abra o terminal na raiz do projeto da API e aplique as migrações do Entity Framework para estruturar o banco de dados:
   ```bash
   dotnet ef database update
   ```
3. Execute o servidor de backend:
   ```bash
   dotnet run
   ```

#### 3. Inicializando o Painel Cliente (Angular)
1. Navegue até o diretório do cliente:
   ```bash
   cd CareerEngineering.Client
   ```
2. Em desenvolvimento, `ng serve` aplica `environment.development.ts` (localhost:5019 + Auth0 de dev) via `fileReplacements`. Para produção, preencha `src/environments/environment.ts` e rode `ng build --configuration=production`.
3. Instale as dependências:
   ```bash
   npm install
   ```
4. Inicie o servidor de desenvolvimento com Hot Reload:
   ```bash
   ng serve
   ```
5. Acesse: http://localhost:4200

---

## 👤 Autor

Desenvolvido por **Rodrigo Ribeiro**.  
Software Developer especialista em construir arquiteturas robustas e escaláveis no backend integradas a interfaces modernas, limpas e altamente reativas no frontend.

* **LinkedIn:** [Acessar meu Perfil Profissional](https://linkedin.com/in/rodrigo-ribeiro-developer)
* **E-mail:** rodrigong@gmail.com
