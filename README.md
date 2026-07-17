# 🚀 CareerEngineering.AI

[![.NET 10](https://img.shields.io/badge/.NET-10.0-blueviolet.svg)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-22.0-red.svg)](https://angular.dev/)
[![Ollama](https://img.shields.io/badge/Ollama-Local_AI-black.svg?logo=ollama)](https://ollama.com/)
[![Semantic Kernel](https://img.shields.io/badge/AI--Powered-Semantic%20Kernel-blue.svg)](https://learn.microsoft.com/en-us/semantic-kernel/)
[![Auth0](https://img.shields.io/badge/Auth-Auth0-orange.svg)](https://auth0.com/)

O **CareerEngineering.AI** é um ecossistema inteligente de alta performance projetado para realizar análise automatizada e minuciosa de aderência profissional (Gap Analysis). O sistema confronta os requisitos técnicos de uma vaga de TI contra o perfil de um candidato, mapeando de forma precisa lacunas de competências divididas em Ferramentas, Metodologias e Certificações.

**Diferencial Arquitetural:** O motor de inteligência opera em duplo estágio por meio de modelos *Open-Source* rodando **100% localmente via Ollama**, garantindo privacidade total dos dados sensíveis do currículo e custo zero de inferência. A entrega do resultado é feita em tempo real através de uma arquitetura orientada a eventos e streaming de tokens.

---

## 🏗️ Arquitetura e Engenharia de Software Realizada

O sistema está estruturado em um modelo desacoplado, utilizando tecnologias modernas de comunicação assíncrona, resistência relacional e controle rigoroso de ciclo de vida de tokens de IA.

### 🖥️ Backend (.NET 10 & SignalR)
* **Orquestração GenAI Local (Semantic Kernel):** Consumo do modelo local via SDK oficial da Microsoft, permitindo isolamento de prompts e controle fino de hiperparâmetros.
* **Pipeline de Engenharia de Prompt em Duplo Estágio (Generator-Refiner):** 1. **Estágio 1 (Extrator Bruto):** Avaliação analítica fria a nível de dicionário. Se o input do currículo for completamente desalinhado com a vaga, o pipeline dispara um gatilho de saída antecipada (`DIVERGENTE`), poupando hardware.
  2. **Estágio 2 (Refinador Consultivo):** Transforma a lista seca em um feedback amigável estruturado rigidamente em 3 seções Markdown.
* **Filtros Defensivos & Guilhotina de Tokens:** Implementação no `CareerChatHub.cs` utilizando penalidades de presença/frequência (`PresencePenalty`/`FrequencyPenalty`) e uma trava lógica via C# que monitora o stream e corta a conexão imediatamente após a seção de Certificações, eliminando repetições ou saudações redundantes do modelo.
* **Resiliência de Hardware:** Controle de timeout assíncrono limitando a requisição do primeiro estágio a **45 segundos** via `CancellationToken`, evitando travamentos por estrangulamento de memória RAM/VRAM.
* **Streaming por WebSockets:** Centralização do fluxo produtivo no Hub SignalR (`/careerChatHub`), transmitindo pedaços de texto em tempo real com suporte a credenciais e CORS restrito a ambiente local seguro (`http://localhost:4200`).
* **Persistência de Auditoria (Write-Only):** Conclusão do stream dispara o salvamento em banco relacional (SQL Server) através do Entity Framework Core, registrando a entidade `Usuario` e a respectiva `Analise` vinculada (ignorado no caso de saída por divergência).
* **Documentação de API:** Configuração nativa de OpenAPI utilizando a interface moderna do **Scalar** disponível em ambiente de desenvolvimento.

### 🎨 Frontend Reativo (Angular 22)
* **Controle de Estado Moderno (Angular Signals):** Gestão reativa de estados de tela (Gatilhos de carregamento como *"Conectando ao mentor e analisando..."*, exibição de perfis e fluxos de erro).
* **Streaming Consumer (RxJS):** Ingestão contínua do fluxo de tokens do SignalR com efeito de digitação em tempo real.
* **Interface Restrita e Autenticada:** Fluxo estruturado contendo uma **Landing Page pública** e uma **Rota Protegida (`/analise`)** blindada por um `AuthGuard` nativo do Auth0.
* **Gestão de Perfil na UI:** Componente de dashboard integrado com o Auth0 para exibição de avatar do usuário logado, dropdown de controle e gatilho de logout.
* **Design Visual:** Interface construída com Tailwind CSS 4, adotando um tema escuro customizado baseado na paleta *emerald*.

---

## 🛠️ Status do Roadmap de Desenvolvimento

O projeto encontra-se em estágio avançado de maturidade de sua fundação crítica, possuindo algumas features legadas e módulos futuros bem definidos:

- [x] **Infraestrutura Básica de IA:** Configuração do Ollama local e homologação do modelo estável **Qwen 2.5 14B Instruct (Q4_K_M)**.
- [x] **Fundação de Interface:** Landing page pública, Dashboard de análise em Angular 22 e componentização reativa via Signals.
- [x] **Segurança:** Integração completa com **Auth0** para autenticação JWT e repasse de *access token* via query string na conexão do SignalR.
- [x] **Pipeline de Análise Inteligente:** Mecanismo *Generator-Refiner* em C#, controle de timeout de hardware, saída rápida para dados divergentes e guilhotina sintática de strings.
- [x] **Persistência do Resultado:** Modelagem de dados EF Core (SQL Server) salvando relatórios pós-streaming de forma automática.
- [⚠️] **Endpoints REST Paralelos:** O controlador `CareerMentorController` disponibiliza o endpoint síncrono `evaluate-gap` (sem streaming, Temp=0). Atualmente, ele funciona como um recurso isolado/legado e **não é consumido** pela aplicação Angular (`career-mentor.ts` desativado), operando sem validação de política de autorização externa.
- [ ] **Fase 3 (Upload Automático):** Implementação do parser físico para extração de textos diretamente de arquivos `.pdf` ou `.docx` (Pendente).
- [ ] **Fase 4 (Leitura de Históricos):** Criação de endpoints de consulta na API e componentes visuais de listagem na UI para ler os relatórios persistidos no banco de dados (Atualmente a escrita é consolidada, mas a leitura é inexistente na interface).
- [ ] **Fase 5 (Expansão Conversacional):** Evolução do fluxo de análise *One-Shot* atual para um assistente de chat multi-turno contínuo com histórico mantido em sessão (Visão de Produto).

---

## 🚀 Requisitos e Como Executar Localmente

Diferente de aplicações web tradicionais, o ecossistema local exige a orquestração e configuração prévia de três pilares de infraestrutura.

### 📋 Pré-requisitos Obrigatórios
1. **Ollama:** Servidor de IA local instalado e rodando.
2. **SQL Server:** Instância ativa de banco de dados relacional.
3. **Auth0 Account:** Um Tenant/Application configurado no Auth0 para prover as chaves de validação JWT e as configurações do cliente front-end.

---

### 🏃 Compartilhamento de Passos para Inicialização

#### 1. Preparando o Cérebro de IA (Ollama)
Com o Ollama ativo em segundo plano na porta padrão 11434, baixe o modelo homologado executando o comando a seguir no terminal:

ollama run qwen2.5:14b

(Nota de Hardware: O modelo exige cerca de 9.2 GB em disco. Em máquinas com GPUs de 6GB VRAM como a GTX 1060, o Ollama fará o transbordo automático de parte dos parâmetros para a memória RAM de 32GB, gerando uma taxa estável porém contida de tokens por segundo devido ao limite do barramento).

#### 2. Configurando as Variáveis e Executando a API (.NET)
1. Certifique-se de que as chaves do seu provedor Auth0 e a string de conexão do seu SQL Server estejam devidamente preenchidas no arquivo `appsettings.Development.json` na pasta `CareerEngineering.Api`.
2. Abra o terminal na raiz do projeto da API e aplique as migrações do Entity Framework para estruturar o banco de dados:
   dotnet ef database update
3. Execute o servidor de backend:
   dotnet run
4. A documentação interativa dos endpoints (Scalar) ficará disponível automaticamente pelo navegador através do mapeamento de desenvolvimento do ambiente .NET.

#### 3. Inicializando o Painel Cliente (Angular)
1. Navegue até o diretório do cliente: cd CareerEngineering.Client
2. Instale os pacotes de dependências do ecossistema:
   npm install
3. Inicie o servidor de desenvolvimento do Angular com Hot Reload:
   ng serve
4. Acesse o sistema através do endereço local padrão: http://localhost:4200

---

## 👤 Autor

Desenvolvido por **Rodrigo Ribeiro**.  
Especialista em unir arquitetura robusta no backend com interfaces dinâmicas no frontend. Se você quer conversar sobre integração de IA em sistemas reais, Engenharia de Prompts ou desenvolvimento Full Stack, conecte-se comigo:

* **LinkedIn:** [Acessar meu Perfil Profissional](https://linkedin.com/in/rodrigo-ribeiro-developer)
* **E-mail:** rodrigong@gmail.com