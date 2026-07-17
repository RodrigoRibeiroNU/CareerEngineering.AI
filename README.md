# 🚀 CareerEngineering.AI

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet.svg)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-18+-red.svg)](https://angular.dev/)
[![Ollama](https://img.shields.io/badge/Ollama-Local_AI-black.svg?logo=ollama)](https://ollama.com/)
[![AI-Powered](https://img.shields.io/badge/AI--Powered-Semantic%20Kernel-blue.svg)](https://learn.microsoft.com/en-us/semantic-kernel/)

O **CareerEngineering.AI** é uma plataforma inteligente e assistente conversacional de alta performance projetado para atuar como um mentor especialista em carreiras e posicionamento de mercado para profissionais de TI. O sistema analisa competências, objetivos de senioridade e pretensões salariais para desenhar rotas de estudos personalizadas e estratégias de marketing pessoal.

**Diferencial Arquitetural:** O núcleo de Inteligência Artificial é alimentado por modelos de linguagem *Open-Source* rodando **100% localmente via Ollama**, garantindo privacidade total dos dados dos usuários e custo zero de inferência de tokens.

---

## 🏗️ Arquitetura do Sistema e Engenharia

A plataforma foi desenhada seguindo os mais rigorosos padrões arquiteturais corporativos, focando no desacoplamento entre o motor de interface do usuário, as regras de negócio e os serviços de orquestração de Inteligência Artificial.

### 🖥️ Backend Principal (.NET 10 & SignalR)
* **Orquestração GenAI Local (Ollama):** Integração com o servidor local do Ollama utilizando o SDK oficial da Microsoft (Semantic Kernel), consumindo modelos de alta precisão cognitiva de forma transparente.
* **Engenharia de Prompt em Duplo Estágio (Generator-Refiner):** Arquitetura que divide a carga de trabalho em duas etapas: um extrator de dados frio focado na validação rígida de dados e um refinador consultivo responsável pela formatação final da mentoria.
* **Filtros Decisórios Interceptadores:** Camada lógica desenvolvida em C# que analisa os tokens do streaming em tempo real, aplicando travas de segurança (*CancellationToken* com Timeout) e guilhotinas sintáticas para barrar alucinações de layout ou redundâncias do modelo.
* **Comunicação em Tempo Real:** Uso do ASP.NET Core SignalR para streaming assíncrono de tokens caractere por caractere, minimizando a percepção de latência do modelo local.

### 🎨 Frontend Reativo (Angular & RxJS)
* **Modern Reactivity (Angular Signals):** Controle fino e reativo de estados de carregamento (*"Conectando ao mentor..."*), interações do usuário e gatilhos visuais da interface sem sobrecarga de renderização.
* **Efeito Streaming (RxJS + SignalR Client):** Motor de processamento projetado para ler os fluxos de dados enviados pelo backend em tempo real, gerando a experiência visual fluida de digitação na tela.
* **Layout Adaptativo Resiliente:** View com dimensionamento otimizado (ex: `h-[72vh]`) configurada para evitar quebras visuais e rolagens involuntárias durante respostas extensas de análises de perfil.

---

## 🖥️ Especificações de Hardware para Execução Local

Este projeto foi desenhado como um estudo de capacidade técnica para rodar LLMs (Large Language Models) de forma 100% local. Devido à escala do modelo de linguagem homologado, a arquitetura exige o compartilhamento estratégico de recursos entre CPU, GPU e memória RAM.

### Requisitos Mínimos (Homologado: Qwen 2.5 14B Instruct Q4_K_M)
* **Processador (CPU):** AMD Ryzen 5 3600X / Intel Core i5 (9ª geração) ou superior.
* **Memória RAM:** 32 GB DDR4 (Mínimo de 3000 MHz recomendado para evitar gargalos de barramento durante o transbordo).
* **Placa de Vídeo (GPU):** NVIDIA GeForce GTX 1060 6GB (ou qualquer GPU com suporte a CUDA e mínimo de 6GB VRAM).
* **Armazenamento:** Mínimo de 15 GB livres em SSD (A execução em HDD torna a latência de leitura inicial inviável).

> **Nota de Arquitetura:** O modelo de 14B (~9.2 GB em disco) excede os 6GB de VRAM físicos da placa de vídeo de desenvolvimento. O ecossistema realiza o transbordo (*offloading*) dos parâmetros excedentes para a memória RAM. Como consequência, a taxa de geração (*Tokens por Segundo*) opera sob restrição de velocidade do barramento da placa-mãe, priorizando precisão analítica e obediência estrita às regras de formatação em detrimento de velocidade bruta.

---

## 🗺️ Roadmap de Desenvolvimento (Milestones)

O projeto é construído de forma modular e incremental. Graças ao avanço da arquitetura de backend e filtros defensivos, as fundições principais foram consolidadas:

### Fase 0: Infraestrutura de IA Local (Ollama)
- [x] Instalação e configuração do servidor Ollama no ambiente de desenvolvimento.
- [x] Testes de *benchmarking*, validação de alucinações sintáticas e homologação do modelo (Migração de Llama 3.1 8B para **Qwen 2.5 14B**).

### Fase 1: Fundação do Motor & Interface Chat
- [x] Criação do layout base em Angular utilizando Standalone Components e submissão reativa de inputs.
- [x] Estruturação da API em .NET 10 com suporte a WebAPI RESTful padrão.
- [x] Integração do backend em C# (Semantic Kernel) com a API REST local do Ollama.

### Fase 2: Streaming de Dados & Experiência Fluida
- [x] Refatoração da comunicação para WebSockets utilizando **ASP.NET Core SignalR** para fornecer streaming de texto em tempo real no frontend.
- [x] Implementação do serviço de gestão de histórico de chat na memória do backend.

### Fase 3: Engenharia de Prompts Avançada & Análise de Arquivos
- [x] Criação do pipeline de análise comparativa (Vaga vs. Currículo).
- [x] Implementação do padrão de prompt em dois estágios (*Generator-Refiner*) com tratamento automático para perfis divergentes.
- [x] Implementação de travas lógicas e interceptadores de fechamento de fluxo em C# para eliminação de redundâncias de escrita.
- [ ] Implementação física do módulo de Upload de Arquivos (extração de texto de arquivos `.pdf`/`.docx`).

### Fase 4: Persistência & Governança Executiva
- [x] Integração de persistência em banco de dados (SQL Server) via Entity Framework Core para salvar históricos e dados de análise.
- [x] Implementação de autenticação segura e controle de contexto de usuário.

---

## 🚀 Como Executar Localmente (Ambiente de Desenvolvimento)

### Pré-requisitos
* SDK do [.NET 10.0](https://dotnet.microsoft.com/) instalado.
* [Node.js](https://nodejs.org/) (Versão LTS recomendada).
* [Angular CLI](https://angular.dev/) instalado globalmente.

### 🧠 1. Configurando o Servidor de IA (Ollama)
1. Faça o download e instale o [Ollama](https://ollama.com/).
2. Abra seu terminal e baixe a versão homologada de 14B da família Qwen:
   ollama run qwen2.5:14b
3. Mantenha o serviço do Ollama rodando em segundo plano (porta padrão `11434`).

### ⚙️ 2. Executando a API (.NET)
1. Navegue até a pasta do servidor: `cd CareerEngineering.Api`
2. Restaure os pacotes: `dotnet restore`
3. Execute as migrações do banco de dados (EF Core) se necessário.
4. Execute o servidor: `dotnet run`

### 💻 3. Executando o Cliente (Angular)
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