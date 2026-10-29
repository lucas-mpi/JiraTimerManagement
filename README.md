# ⏱ Jira Focus Timer (`JiraTimerManagement`)

Ferramenta desktop minimalista e Web API desenvolvidas na plataforma **.NET** para otimizar o apontamento e lançamento de horas (*Worklog*) de desenvolvedores diretamente no **Jira Cloud**, reduzindo a necessidade de navegação manual em múltiplas abas do navegador e evitando perdas de registro durante interrupções (como ligações, atendimentos urgentes e cerimônias ágeis).

---

## 📋 Contexto Acadêmico e Organizacional

- **Instituição:** Centro Universitário Internacional UNINTER — Escola Superior Politécnica (ESP)
- **Curso:** Bacharelado em Ciência da Computação
- **Disciplina:** Atividades Extensionistas — Tecnologia Aplicada à Inclusão Digital
- **Aluno:** Lucas Mariani Pires (RU: 5197181)
- **Setor de Aplicação:** Setor interno de Tecnologia da Informação (Desenvolvimento e Suporte de Software) de cooperativa agroindustrial sediada em Medianeira - PR.
- **ODS Relacionado:** 09 — Indústria, Inovação e Infraestrutura.

---

## 🎯 Problema e Solução Proposta

No fluxo diário de desenvolvimento e suporte de software, os desenvolvedores frequentemente precisam interromper uma atividade codificada para atender ligações urgentes, chamados de suporte ou participar de cerimônias da Sprint (Daily, Planning, Refinamento, Retrospectiva). No modelo tradicional (via navegador no Jira):
1. O apontamento de horas é esquecido ou feito de forma imprecisa no fim do dia.
2. Cards de **Cerimônias / Dailys / Chamadas** normalmente não ficam atribuídos (`assignee`) a um desenvolvedor específico, exigindo buscas manuais repetitivas no Jira.
3. Abrir diversas abas do Jira gera distração e sobrecarga operacional.

### Como o **Jira Focus Timer** resolve isso:
- **Aplicativo Desktop Minimalista (Windows Forms .NET 4.8):** Fica acessível na área de trabalho (com opções de **📌 Fixar no Topo** e **🗕 Modo Mini-Widget Flutuante** + **Ícone na Bandeja do Sistema**), sem depender de abas de navegador.
- **Autenticação por Conta + Seleção de Projeto Multi-Time:** Cada desenvolvedor autentica com seu e-mail e *API Token* do Jira e pode filtrar o projeto do seu time (ex.: `NDSTRS`, `SUP`, `COOP`, ou todos os projetos).
- **Troca Inteligente com 1 Clique (Auto-Sync em Interrupções):**
  - Ao escolher uma tarefa e clicar em **▶ Iniciar**, o cronômetro inicia a contagem.
  - Se alguém ligar para resolver outra demanda e o desenvolvedor clicar em **▶ Iniciar** na nova tarefa:
    1. A contagem da tarefa anterior é **interrompida automaticamente**.
    2. O tempo decorrido e o **comentário digitado** na tarefa anterior são **sincronizados em background no Worklog do Jira** (arredondando contagens menores que 60 segundos para o mínimo exigido pelo Jira de `1m / 60s`).
    3. O cronômetro da nova tarefa **inicia imediatamente** do zero.
- **Aba "🤝 Cerimônias / ⭐ Fixadas" (Cards Não Atribuídos):**
  - Permite cronometrar ou lançar horas em cards de cerimônias (Daily, Planning, Reunião, Chamadas) que não estão atribuídos a um dev específico.
  - Permite **Fixar (`⭐`)** qualquer card da Sprint (ou adicionar pelo código via botão **`+ Fixar Card`**, ex.: `NDSTRS-100`) para acesso rápido diário.
- **Comentário no Worklog + Lançamento de Horas Manuais (`+ Manual / 💬`):**
  - **No Cronômetro Ativo:** Campo rápido de comentário (`💬 Comentário`) enviado automaticamente ao parar ou trocar de tarefa.
  - **Em Cada Card (`+ Manual / 💬`):** Abre um modal minimalista com atalhos rápidos (`+15m`, `+30m`, `+45m`, `+1h`, `+1h 30m`, `+2h`) ou digitação livre + descrição/comentário para lançar horas diretamente no Jira sem precisar rodar o cronômetro.

---

## 🏗️ Arquitetura do Projeto

A solução (`JiraTimerManagement.slnx`) é composta por dois projetos complementares:

```
JiraTimerManagement/
├── JiraTimerManagement.csproj      # Aplicativo Desktop Minimalista (Windows Forms .NET Framework 4.8)
│   ├── Form1.cs                    # Lógica de UI, abas (Minhas vs Cerimônias), cronômetro ativo e troca automática
│   ├── Form1.Designer.cs           # Layout minimalista em tema escuro (Flat UI)
│   ├── ManualWorklogDialog.cs      # Modais minimalistas de lançamento manual (+15m, +1h, etc.) e fixação de cards (⭐)
│   ├── JiraService.cs              # Cliente REST API v3 do Jira + persistência local + Modo Protótipo
│   └── Program.cs                  # Ponto de entrada da aplicação Desktop
│
└── JiraTimerApi/                   # Backend / Web API (.NET 10 + Scalar OpenAPI)
    ├── Program.cs                  # Endpoints REST (/api/me, /api/projetos, /api/tarefas, /api/cerimonias, /worklog)
    ├── JiraClientService.cs        # Serviço de integração HTTP com Jira Cloud REST API v3 (ADF Worklog)
    └── appsettings.json            # Configurações padrão de integração Jira
```

---

## 🔌 Integração com a API do Jira Cloud (REST API v3)

A autenticação utiliza **HTTP Basic Auth** codificado em Base64 (`email:apiToken`) sobre HTTPS (TLS 1.2+):

| Funcionalidade | Método e Endpoint Jira Cloud | Descrição |
| :--- | :--- | :--- |
| **Validação de Conta** | `GET /rest/api/3/myself` | Valida o token do desenvolvedor e obtém `displayName` e `accountId`. |
| **Listagem de Projetos** | `GET /rest/api/3/project/search` | Carrega os projetos visíveis para seleção de time no app. |
| **Minhas Tarefas** | `POST /rest/api/3/search/jql` | Executa JQL `assignee = currentUser() AND project = "..." ORDER BY updated DESC`. |
| **Cerimônias e Fixadas** | `POST /rest/api/3/search/jql` | Busca cards de cerimônias (Daily, Planning, Reunião, Chamada) e `key IN (...)` mesmo sem `assignee`. |
| **Registro de Horas** | `POST /rest/api/3/issue/{key}/worklog` | Envia `timeSpentSeconds` (mínimo `60s`) e comentário em *Atlassian Document Format (ADF)*. |

---

## 🌿 Estrutura de Branches Git

- `main`: Versão base contendo o aplicativo desktop minimalista, seleção de projetos, cronômetro com troca automática em 1 clique e API REST.
- `feat/cerimonias-e-lancamento-manual`: Evolução contendo suporte a cards de Cerimônias/Dailys não atribuídos, fixação de favoritos (`⭐`), comentários nos apontamentos e lançamento manual de horas (`+ Manual / 💬`).

---

## 🚀 Como Executar

### 1. Aplicativo Desktop (`JiraTimerManagement`)
```powershell
dotnet build JiraTimerManagement.csproj --no-restore
.\bin\Debug\JiraTimerManagement.exe
```

### 2. Web API (`JiraTimerApi`)
```powershell
dotnet run --project JiraTimerApi/JiraTimerApi.csproj
```
