# 🚀 CareerEngineering.AI

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet.svg)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-18+-red.svg)](https://angular.dev/)
[![AI-Powered](https://img.shields.io/badge/AI--Powered-Semantic%20Kernel-blue.svg)](https://learn.microsoft.com/en-us/semantic-kernel/)

O **CareerEngineering.AI** é uma plataforma inteligente e assistente conversacional de alta performance projetado para atuar como um mentor especialista em carreiras e posicionamento de mercado para profissionais de TI. O sistema analisa competências, objetivos de senioridade e pretensões salariais para desenhar rotas de estudos personalizadas e estratégias de marketing pessoal.

---

## 🏗️ Arquitetura do Sistema e Engenharia

A plataforma foi desenhada seguindo os mais rigorosos padrões arquiteturais corporativos, focando no desacoplamento entre o motor de interface do usuário, as regras de negócio e os serviços de orquestração de Inteligência Artificial.

### 🖥️ Backend Principal (.NET 10)
* **Orquestração de GenAI:** Integração com os modelos de linguagem (LLMs) através do SDK oficial da Microsoft ou Semantic Kernel, encapsulando as chamadas e tratando as respostas de forma assíncrona.
* **Prompt Engineering Isolation:** Camada dedicada para armazenamento e injeção dinâmica de System Prompts baseados no contexto do usuário (ex: regras ocultas que obrigam a IA a agir como um Tech Recruiter sênior de elite).
* **Gestão de Contexto e Histórico:** Implementação de um serviço de memória para manter o contexto reativo do chat, garantindo que o agente se recorde das respostas e informações enviadas anteriormente na mesma sessão.
* **Validação Rigorosa:** Uso de bibliotecas de validação para filtrar as mensagens do usuário e mitigar riscos de Injeção de Prompts (Prompt Injection).

### 🎨 Frontend Reativo (Angular & RxJS)
* **Modern Reactivity (Angular Signals):** Controle fino e reativo de estados de carregamento, interações do usuário e gatilhos visuais da interface sem sobrecarga de renderização.
* **Efeito Streaming (RxJS):** Motor de processamento assíncrono projetado para ler fluxos de dados de texto em tempo real (Server-Sent Events / WebSockets via SignalR), gerando a experiência visual de "máquina de escrever" do texto à medida que a IA responde.
* **Exportação Analítica:** Funcionalidade nativa na UI para exportar os roteiros e planos de carreira gerados pela IA em formato Markdown estruturado ou documentos PDF formatados.

---

## 🗺️ Roadmap de Desenvolvimento (Milestones)

O projeto está sendo construído de forma modular e incremental, dividido em quatro fases estratégicas:

### Fase 1: Fundação do Motor & Interface Chat (Em Desenvolvimento)
- [ ] Criação do layout base em Angular utilizando Standalone Components e submissão reativa de inputs.
- [ ] Estruturação da API em .NET 10 com suporte a WebAPI RESTful padrão.
- [ ] Integração básica e segura de chamadas com provedores de LLM.

### Fase 2: Streaming de Dados & Experiência Fluida
- [ ] Refatoração da comunicação para WebSockets (ASP.NET Core SignalR) para fornecer streaming de texto caractere por caractere no frontend.
- [ ] Implementação do serviço de gestão de histórico de chat na memória do backend.

### Fase 3: Engenharia de Prompts Avançada & Análise de Arquivos
- [ ] Criação do módulo de Upload de Currículos (extração de texto de arquivos .pdf/.docx).
- [ ] Implementação de prompts dinâmicos estruturados para gerar mapas de lacunas (Gap Analysis) baseados em vagas reais de mercado.

### Fase 4: Persistência & Governança Executiva
- [ ] Integração de persistência em banco de dados (PostgreSQL/SQL Server) via Entity Framework Core para salvar históricos de usuários.
- [ ] Implementação de autenticação segura via JSON Web Tokens (JWT).

---

## 🚀 Como Executar Localmente (Ambiente de Desenvolvimento)

### Pré-requisitos
* SDK do .NET 10.0 instalado.
* Node.js (Versão LTS recomendada).
* Angular CLI instalado globalmente.
* Uma chave de API válida para o provedor de IA configurado ou Ollama rodando localmente.

### ⚙️ Executando a API (.NET)
1. Navegue até a pasta do servidor: `cd CareerEngineering.Api`
2. Restaure os pacotes: `dotnet restore`
3. Configure a sua chave de API nas variáveis de ambiente ou appsettings.json.
4. Execute o servidor: `dotnet run`

### 💻 Executando o Cliente (Angular)
1. Navegue até a pasta do cliente: `cd CareerEngineering.Client`
2. Instale as dependências: `npm install`
3. Inicie a aplicação com Hot Reload: `ng serve`
4. Abra o navegador em: `http://localhost:4200`

---

## 👤 Autor

Desenvolvido por **Rodrigo Ribeiro**. 
Especialista em unir arquitetura robusta no backend com interfaces dinâmicas no frontend. Se você quer conversar sobre integração de IA em sistemas reais, Engenharia de Prompts ou desenvolvimento Full Stack, conecte-se comigo:

* **LinkedIn:** [Acessar meu Perfil Profissional](https://linkedin.com/in/rodrigo-ribeiro-developer)
* **E-mail:** rodrigong@gmail.com
