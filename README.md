[![tests](https://github.com/Proxyspyk/Heimdall/actions/workflows/tests.yml/badge.svg)](https://github.com/Proxyspyk/Heimdall/actions/workflows/tests.yml)
[![license: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![dotnet](https://img.shields.io/badge/dotnet-10.0-blue)](Heimdall.csproj)
[![architecture](https://img.shields.io/badge/architecture-CQRS%20--%20Single--File-brightgreen)](Abstractions/)

# Heimdall

**O Guardião Inabalável.** Na mitologia nórdica, Heimdall é o vigilante supremo possuidor de visão e audição infalíveis, capaz de enxergar a centenas de léguas de dia ou de noite. Esse é o espírito da ferramenta: manter uma vigilância implacável sobre os componentes críticos do seu sistema Linux, identificando superfícies de ataque e vulnerabilidades antes que sejam exploradas.

Scanner desenvolvido em **C# (.NET 10)** sob a arquitetura **CQRS (sem banco de dados)**. Detecta componentes críticos de um sistema Linux (kernel, glibc, sudo, systemd, polkit, openssl, docker, podman, snap, etc.) e cruza as versões instaladas com o **NVD** (CVEs + CVSS) e o **EPSS** (probabilidade real de exploração), gerando um relatório de risco priorizado.

Diferente de ferramentas como LinPEAS/LinEnum (que enumeram possíveis vetores de escalada de privilégio), este projeto foca em **correlacionar versões instaladas com vulnerabilidades conhecidas e sua probabilidade real de exploração**, para ajudar a priorizar o que corrigir primeiro.

> ⚠️ **Uso defensivo.** Esta ferramenta é somente leitura: não executa exploits nem explora vulnerabilidades. Ela detecta versões e consulta bases públicas de CVE. Use apenas em sistemas que você tem autorização para auditar.

---

## Como funciona (Arquitetura CQRS)

```
┌─────────────────────────┐     ┌─────────────────────────┐     ┌────────────────────────┐     ┌────────────────────────┐
│  SystemInfoCollector    │ --> │  CorrelateRiskHandler   │ --> │    ReportPresenter     │ --> │   Saída Terminal /     │
│ (dpkg, rpm, binários...)│     │ (NVD + EPSS + CQRS Query│     │ (ANSI Terminal / JSON) │     │     Relatório JSON     │
└─────────────────────────┘     └─────────────────────────┘     └────────────────────────┘     └────────────────────────┘
```

1. **`Abstractions/`** — Interfaces genéricas da arquitetura CQRS (`IQuery`, `IQueryHandler`, `ICommand`, `ICommandHandler`).
2. **`Infrastructure/Collectors/SystemInfoCollector.cs`** — Coleta local passiva e segura (kernel via `RuntimeInformation`, distro via `/etc/os-release`, versões via `dpkg-query`, `rpm` ou `--version` dos binários).
3. **`Infrastructure/HttpClients/NvdApiClient.cs`** — Consulta a [API pública do NVD 2.0](https://nvd.nist.gov/developers/vulnerabilities) por palavra-chave (nome do componente) com suporte a paginação e throttling.
4. **`Infrastructure/HttpClients/EpssApiClient.cs`** — Consulta em lotes a [API EPSS do FIRST.org](https://www.first.org/epss/api) para estimar a probabilidade de exploração real nos próximos 30 dias.
5. **`Domain/Services/` (`VersionMatcher`, `RiskCalculator`)** — Filtra CVEs cuja descrição menciona a versão instalada (reduz falsos positivos) e calcula um `risk_score` (0–100) combinando CVSS + EPSS + indício de exploit público.
6. **`Application/Queries/CorrelateRisk/`** — Orquestrador do CQRS que processa a busca e correlaciona as fontes.
7. **`Application/Commands/GenerateReport/ReportPresenter.cs`** — Imprime o relatório colorido no terminal e, opcionalmente, exporta JSON estruturado (útil para CI/CD ou SIEM).

---

## Instalação e Guia de Deploy

O Heimdall é empacotado como um **único arquivo binário autossuficiente (Single File)**. Isso significa que o servidor de destino **não precisa ter o .NET instalado**, nem Python, nem compiladores ou dependências externas.

### ETAPA 1: Na sua máquina (Build e Geração do Executável)

1. Clonar o repositório Git:
   ```bash
   git clone https://github.com/Proxyspyk/Heimdall.git
   cd Heimdall
   ```

2. Gerar o executável único autossuficiente:
   ```bash
   ./build.sh
   ```
   > O script compilará o projeto e gerará o arquivo binário em **`./dist/heimdall`**.

3. *(Opcional)* Instalar o comando `heimdall` globalmente na sua máquina local:
   ```bash
   mkdir -p ~/.local/bin
   ln -sf $(pwd)/dist/heimdall ~/.local/bin/heimdall
   ```

---

### ETAPA 2: Transferência para o Servidor Alvo

Envie o arquivo binário gerado na pasta `dist/` para o servidor Linux que deseja auditar usando `scp` ou `sftp`:

```bash
scp dist/heimdall usuario@ip-do-servidor:/tmp/
```

---

### ETAPA 3: No Servidor Alvo (Execução da Auditoria)

1. Conecte no servidor via SSH:
   ```bash
   ssh usuario@ip-do-servidor
   ```

2. Dê permissão de execução ao binário:
   ```bash
   chmod +x /tmp/heimdall
   ```

3. Execute a auditoria:
   ```bash
   /tmp/heimdall scan --json /tmp/relatorio_servidor.json
   ```

---

### ETAPA 4: Coleta do Relatório e Limpeza

1. Baixe o relatório gerado de volta para a sua máquina (no seu terminal local):
   ```bash
   scp usuario@ip-do-servidor:/tmp/relatorio_servidor.json ./
   ```

2. Remova o Heimdall do servidor de destino:
   ```bash
   rm /tmp/heimdall /tmp/relatorio_servidor.json
   ```

---

## Uso

```bash
# Scan simples (exibe relatório visual no terminal)
heimdall scan

# Salva também um relatório em formato JSON
heimdall scan --json relatorio.json

# Desativa o filtro de versão (mais resultados, mais ruído)
heimdall scan --no-version-filter

# Omitir o banner de abertura
heimdall scan --no-banner

# Usa uma API key do NVD (aumenta o rate limit de 5 para 50 req/30s)
# Gratuita em https://nvd.nist.gov/developers/request-an-api-key
heimdall scan --api-key SUA_KEY
# ou via variável de ambiente: export NVD_API_KEY=SUA_KEY
```

---

### Exemplo de Saída no Terminal

```text
Heimdall — Linux CVE Auditor
Scan em 2026-08-15T03:47:01Z

[+] Sistema
    Distro : Ubuntu 24.04
    Kernel : Linux 6.6.137+ #1 SMP PREEMPT_DYNAMIC
    Arch   : x64

[+] Componentes detectados
    glibc      2.39-0ubuntu8.4      (dpkg)
    sudo       1.9.15p5-3ubuntu5    (dpkg)
    systemd    255.4-1ubuntu8.5     (dpkg)
    openssl    3.0.13-0ubuntu3.5    (dpkg)
    openssh    1:9.6p1-3ubuntu13.8  (dpkg)

[+] Possíveis vulnerabilidades (1)

CVE-2024-6387  risco: 87.4/100
    Componente : openssh 1:9.6p1-3ubuntu13.8
    CVSS       : 8.1 (HIGH)
    EPSS       : 93.0%
    Exploit    : ✔ indício de exploit público
    Descrição  : A signal handler race condition was found in OpenSSH's server (sshd)...
```

---

## Limitações Conhecidas (Leia antes de confiar no resultado)

- O matching é feito por **palavra-chave + heurística de versão na descrição da CVE**, não por CPE 2.3 exato. Isso significa que pode haver **falsos positivos e falsos negativos**. Trate o relatório como uma lista de priorização, não como confirmação definitiva.
- A API do NVD tem rate limit sem API key (5 req/30s), então scans com muitos componentes podem demorar. Use `--api-key` ou `NVD_API_KEY` para acelerar.
- "Indício de exploit público" é uma heurística baseada nas referências do próprio NVD.

---

## Roadmap / Idéias para Contribuir

- [ ] Matching por CPE 2.3 real (usar o dicionário oficial de CPEs)
- [ ] Integração com ExploitDB (mirror CSV) e busca de PoCs no GitHub
- [ ] Exportar relatório em formato HTML interativo
- [ ] Suporte a compilação 100% Native AOT (via `PublishAot`)
- [ ] Suporte a mais distros/gerenciadores de pacote (apk, pacman)

---

## Testes

Para rodar a suíte de testes unitários xUnit em C#:

```bash
dotnet test Tests/Heimdall.Tests.csproj
```

---

## Licença

MIT — veja [LICENSE](LICENSE).

## Autor

**Gabriel Knobbe da Silveira** ([@Proxyspyk](https://github.com/Proxyspyk))  
Hacker ético focado em Bug Bounty, Pentest e Red Team.

[LinkedIn](https://www.linkedin.com/in/gabriel-knobbe-da-silveira-628620362/)
