# ⏱ Jira Focus Timer (`JiraTimerManagement`)

Ferramenta desktop minimalista em **WPF (.NET Framework 4.8)** e **Web API (.NET 10)** desenvolvidas para otimizar o apontamento e lançamento de horas (*Worklog*) de desenvolvedores diretamente no **Jira Cloud**, reduzindo a necessidade de navegação manual em múltiplas abas do navegador e evitando perdas de registro durante interrupções (como ligações, chamados urgentes e cerimônias ágeis).

---

## 🎯 Problema e Solução Proposta

No fluxo diário de desenvolvimento e sustentação de software, os desenvolvedores frequentemente precisam interromper uma atividade codificada para atender ligações urgentes, chamados de suporte ou participar de cerimônias da Sprint (Daily, Planning, Refinamento, Retrospectiva). No modelo tradicional via navegador no Jira:
1. O apontamento de horas é esquecido ou feito de forma imprecisa no fim do dia.
2. Cards de **Cerimônias / Dailys / Chamadas** ou **Sustentação** muitas vezes não ficam atribuídos (`assignee`) a um desenvolvedor específico, exigindo buscas manuais repetitivas no Jira.
3. Abrir diversas abas do Jira gera distração e sobrecarga operacional.

### ✨ Principais Funcionalidades do **Jira Focus Timer**

- **Aplicativo Desktop Minimalista (WPF .NET 4.8 com Tema Grafite Escuro):**
  - Moldura customizada via `WindowChrome` (sem a borda padrão do sistema) e suporte a *Per-Monitor DPI Awareness* para nitidez em múltiplos monitores.
  - Modos **📌 Fixar no Topo (*Always on Top*)** e **🗕 Modo Mini-Widget Compacto**, ideais para acompanhar o tempo durante reuniões e ligações.
  - Link direto no código do card (ex.: `APP-142`) para abrir a *issue* no navegador com 1 clique.
- **Autenticação Jira Cloud + Filtro Multi-Projeto:**
  - Cada desenvolvedor autentica com a URL da sua organização (`https://sua-empresa.atlassian.net`), seu e-mail e seu *API Token* do Atlassian.
  - As credenciais ficam salvas localmente na máquina do usuário (`%APPDATA%\JiraTimerManagement\config.json`), fora do controle de versão do Git.
  - Conta também com **Modo Protótipo (Simulação)** para testar todas as funcionalidades instantaneamente sem precisar de credenciais reais.
- **Troca Inteligente com 1 Clique (*Auto-Sync* em Interrupções):**
  - Ao escolher uma tarefa e clicar em **▶ Iniciar**, o cronômetro inicia a contagem.
  - Se surgir uma interrupção ou ligação e o desenvolvedor clicar em **▶ Iniciar** na nova demanda:
    1. A contagem da tarefa anterior é **interrompida automaticamente**.
    2. O tempo decorrido e o **comentário digitado** na tarefa anterior são **sincronizados em background no Worklog do Jira** (arredondando contagens menores que 60 segundos para o mínimo exigido pelo Jira de `1m / 60s`).
    3. O cronômetro da nova tarefa **inicia imediatamente** do zero.
- **Visualização em 3 Abas Operacionais:**
  - **👤 Minhas Tarefas:** Lista os cards ativos (`statusCategory != Done`) atribuídos ao usuário logado (`assignee = currentUser()`).
  - **🤝 Cerimônias:** Localiza automaticamente cards recorrentes de *Reuniões*, *Planning*, *Refinamento* e *Suporte*, mesmo quando não atribuídos a um desenvolvedor específico.
  - **📋 Todos Ativos:** Exibe todos os cards ativos de Scrum e Sustentação do projeto selecionado, permitindo apontar horas em demandas compartilhadas pela equipe.
- **Lançamento de Horas Manuais (`+ Manual / 💬`) + Histórico em Tempo Real:**
  - Campo rápido de comentário (`💬 Comentário`) no banner do cronômetro ativo.
  - Botão **`+ Manual / 💬`** em cada card abrindo um modal com atalhos rápidos (`+15m`, `+30m`, `+45m`, `+1h`, `+1h 30m`, `+2h`) ou digitação livre (ex.: `1h 20m`, `45`) + descrição da atividade.
  - Painel inferior com o histórico auditável dos últimos apontamentos sincronizados na sessão.

---

## 🏗️ Arquitetura do Projeto

A solução ([`JiraTimerManagement.slnx`](file:///c:/dev/dotnet/JiraTimerManagement/JiraTimerManagement.slnx)) é composta por dois projetos:

```text
JiraTimerManagement/
├── JiraTimerManagement.csproj      # Aplicativo Desktop Minimalista (WPF .NET Framework 4.8)
│   ├── TelaPrincipal.xaml          # Interface gráfica em tema escuro (WindowChrome, Abas, Banner e Modal)
│   ├── TelaPrincipal.xaml.cs       # Lógica de UI, cronômetro ativo, troca 1-clique e busca com debounce
│   ├── JiraService.cs              # Cliente REST API v3 do Jira + persistência local + Modo Protótipo
│   ├── app.manifest                # Configuração Per-Monitor V2 DPI Awareness
│   └── Program.cs                  # Ponto de entrada da aplicação Desktop WPF
│
└── JiraTimerApi/                   # Backend / Web API (.NET 10 + Scalar OpenAPI)
    ├── Program.cs                  # Endpoints REST (/api/me, /api/projetos, /api/tarefas, /api/cerimonias, /api/ativos, /worklog)
    ├── JiraClientService.cs        # Serviço de integração HTTP com Jira Cloud REST API v3 (ADF Worklog)
    └── appsettings.json            # Configurações base da API (sobrescritas via User Secrets / Development / Headers)
```

---

## 🔌 Integração com a API do Jira Cloud (REST API v3)

A comunicação utiliza **HTTP Basic Auth** codificado em Base64 (`email:apiToken`) sobre HTTPS (TLS 1.2+):

| Funcionalidade | Método e Endpoint Jira Cloud | Descrição |
| :--- | :--- | :--- |
| **Validação de Conta** | `GET /rest/api/3/myself` | Valida o token do desenvolvedor e obtém `displayName` e `accountId`. |
| **Listagem de Projetos** | `GET /rest/api/3/project/search` | Carrega os projetos visíveis para seleção de time no app. |
| **Minhas Tarefas** | `POST /rest/api/3/search/jql` | Executa JQL `assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC`. |
| **Cerimônias** | `POST /rest/api/3/search/jql` | Busca cards de cerimônias (`Reuniões`, `Refinamento`, `Planning`, `Suporte`) ativos mesmo sem `assignee`. |
| **Todos os Cards Ativos** | `POST /rest/api/3/search/jql` | Executa JQL `statusCategory != Done ORDER BY updated DESC` filtrando opcionalmente pelo projeto. |
| **Registro de Horas** | `POST /rest/api/3/issue/{key}/worklog` | Envia `timeSpentSeconds` (mínimo `60s`) e comentário estruturado em *Atlassian Document Format (ADF)*. |

---

## ⚙️ Configuração Local e Uso Corporativo (Sem Expor Credenciais no Git)

A aplicação foi desenhada para que você possa utilizá-la no seu ambiente de trabalho diário mantendo o código-fonte 100% limpo no GitHub:

### 1. Como gerar seu API Token no Jira Cloud
1. Acesse [https://id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens).
2. Clique em **Criar token de API** (*Create API token*), informe um nome (ex.: `Jira Focus Timer`) e copie o token gerado.

### 2. Configurando no Aplicativo Desktop (`JiraTimerManagement`)
1. Abra o aplicativo e clique no botão **`⚙ Conta`** no canto superior direito.
2. Desmarque a opção **Modo Protótipo (Dados Simulados)** para conectar ao Jira real.
3. Preencha:
   - **URL da Organização:** `https://sua-empresa.atlassian.net`
   - **E-mail do Desenvolvedor:** `seu.email@dominio.com`
   - **API Token:** *(cole o token gerado na Atlassian)*
4. Clique em **Salvar Credenciais e Carregar Projetos**.
5. Os dados são gravados exclusivamente na sua máquina em:
   ```text
   %APPDATA%\JiraTimerManagement\config.json
   ```
   *(Esse diretório fica fora do repositório Git, garantindo que suas credenciais e URL corporativa nunca sejam versionadas e permanecendo intactos mesmo após `git pull` ou `git push`).*

### 3. Configurando na Web API (`JiraTimerApi`)
Para rodar a Web API localmente com suas credenciais corporativas sem alterar o [`appsettings.json`](file:///c:/dev/dotnet/JiraTimerManagement/JiraTimerApi/appsettings.json) versionado:
- Defina no arquivo `JiraTimerApi/appsettings.Development.json` *(já ignorado no [`.gitignore`](file:///c:/dev/dotnet/JiraTimerManagement/.gitignore))*, **ou**
- Envie via cabeçalhos HTTP nas requisições:
  - `X-Jira-Url`: `https://sua-empresa.atlassian.net`
  - `X-Jira-Email`: `seu.email@dominio.com`
  - `X-Jira-Token`: `seu-api-token`

---

## 🚀 Como Compilar e Executar

### 1. Aplicativo Desktop (`JiraTimerManagement`)
```powershell
dotnet build JiraTimerManagement.csproj
.\bin\Debug\net48\JiraTimerManagement.exe
```

### 2. Web API (`JiraTimerApi`)
```powershell
dotnet run --project JiraTimerApi/JiraTimerApi.csproj
```
No ambiente de desenvolvimento (`Development`), a documentação interativa **Scalar OpenAPI** fica disponível em `/scalar/v1`.
