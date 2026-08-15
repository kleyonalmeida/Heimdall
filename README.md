[![license: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![dotnet](https://img.shields.io/badge/dotnet-10.0%20(Native%20AOT)-blue)](Heimdall.csproj)
[![architecture](https://img.shields.io/badge/architecture-CQRS%20--%20Single--File-brightgreen)](Abstractions/)
[![size](https://img.shields.io/badge/binary%20size-~20MB-success)]()

# Heimdall

**O Guardião Inabalável.** Na mitologia nórdica, Heimdall é o vigilante supremo possuidor de visão e audição infalíveis, capaz de enxergar a centenas de léguas de dia ou de noite. Esse é o espírito da ferramenta: manter uma vigilância implacável sobre os componentes críticos do seu sistema Linux, identificando superfícies de ataque e vulnerabilidades antes que sejam exploradas.

Heimdall é um **auditor e scanner de vulnerabilidades defensivo** desenvolvido em **C# (.NET 10)** compilado como **Native AOT**. Ele detecta componentes críticos de um sistema Linux (kernel, glibc, sudo, systemd, polkit, openssl, docker, podman, etc.) e correlaciona as versões instaladas com o **NVD** (CVEs + CVSS) e a **API EPSS** (probabilidade real de exploração nos próximos 30 dias), gerando um relatório de risco priorizado.

---

> [!IMPORTANT]
> **O Grande Destaque: Execução Limpa & Zero Footprint**  
> O Heimdall é empacotado como um **único arquivo binário autossuficiente (Native AOT)** de aproximadamente **20 MB**.  
> - **Zero Dependências:** O servidor auditado **não precisa ter o .NET instalado**, nem Python, nem compiladores ou bibliotecas externas.
> - **Sem Sujeira no Servidor:** Você pode copiar o executável para o diretório `/tmp`, rodar a auditoria em milissegundos e remover o arquivo em seguida (`rm /tmp/heimdall`), sem deixar qualquer rastro ou alteração no sistema auditado.

---

## 🎯 Propósito da Ferramenta & Como ela Ajuda

Diferente de ferramentas como LinPEAS ou LinEnum (que focam na enumeração de vetores para escalada local de privilégios), o Heimdall foi concebido para **gestão de vulnerabilidades baseada em risco real**:

1. **Priorização Inteligente de Patches:** Em vez de exibir centenas de CVEs genéricas, o Heimdall cruza dados do CVSS com o índice **EPSS (Exploit Prediction Scoring System)** e indícios de exploits públicos. Isso ajuda administradores de sistemas, engenheiros DevSecOps e analistas de segurança a responderem primeiro às vulnerabilidades que possuem **maior chance real de serem exploradas**.
2. **Auditoria Ágil e Não Intrusiva:** Permite auditar servidores de produção, ambientes críticos e contêineres sem instalar pacotes ou alterar o estado do sistema.
3. **Integração com CI/CD e SIEM:** Gera relatórios estruturados em JSON para integração automatizada em pipelines de deploy e monitoramento contínuo.

> [!NOTE]
> ⚠️ **Uso defensivo.** Esta ferramenta é estritamente de auditoria e somente leitura: não executa exploits nem altera configurações de sistema. Use apenas em sistemas que você possui autorização explícita para auditar.

---

## 🏗️ Arquitetura de Software (CQRS sem Banco de Dados)

O projeto foi construído utilizando os princípios da arquitetura **CQRS (Command Query Responsibility Segregation)** e **Clean Architecture**, dispensando a necessidade de bancos de dados ou persistência pesada em disco:

```
┌─────────────────────────┐     ┌─────────────────────────┐     ┌────────────────────────┐     ┌────────────────────────┐
│  SystemInfoCollector    │ --> │  CorrelateRiskHandler   │ --> │    ReportPresenter     │ --> │   Saída Terminal /     │
│ (dpkg, rpm, binários...)│     │ (NVD + EPSS + CQRS Query│     │ (ANSI Terminal / JSON) │     │     Relatório JSON     │
└─────────────────────────┘     └─────────────────────────┘     └────────────────────────┘     └────────────────────────┘
```

### Discriminativo dos Componentes

- **`Abstractions/`**: Interfaces genéricas e desacopladas da arquitetura CQRS (`IQuery`, `IQueryHandler`, `ICommand`, `ICommandHandler`).
- **`Domain/`**: Camada pura com as regras de negócio de filtragem de versão (`VersionMatcher`) e cálculo do Score de Risco (`RiskCalculator`). Totalmente isolada de detalhes de infraestrutura.
- **`Infrastructure/`**:
  - `Collectors/SystemInfoCollector.cs`: Coleta passiva de componentes do Linux via `/etc/os-release`, `dpkg-query`, `rpm` ou `--version`.
  - `HttpClients/NvdApiClient.cs`: Consulta resiliente à API do NVD 2.0 com suporte a throttling e paginação.
  - `HttpClients/EpssApiClient.cs`: Consulta em lotes (batching) ao FIRST.org para obter a probabilidade EPSS.
- **`Application/`**: Orquestração dos fluxos de busca e correlação de risco (`CorrelateRiskHandler`).
- **`Presentation/`**: Apresentação de relatórios coloridos em terminal ANSI (`ReportPresenter`) ou exportação estruturada em JSON.

### 💡 Vantagens desta Arquitetura

- **Alta Testabilidade:** Lógicas de domínio (como cálculo de risco e correspondência de versões) possuem 100% de cobertura de testes unitários sem necessidade de simular bancos de dados.
- **Manutenibilidade e Extensibilidade:** Adicionar suporte a novos gerenciadores de pacotes (`apk`, `pacman`) ou novos provedores de inteligência de ameaças requer apenas implementar uma nova interface sem alterar a lógica existente.
- **Baixo Consumo de Recursos:** Fluxo de dados unidirecional e assíncrono em memória, ideal para execução rápida em ambientes restritos.

---

## ⚡ Por que Native AOT? (Compilação & Benefícios Técnicos)

Ao compilar o Heimdall com **Native AOT (`PublishAot=true`)**, o código C# é traduzido diretamente para **código de máquina nativo (ELF de 64 bits)** durante o build, resultando em um binário compacto de **~20 MB**.

### Benefícios Técnicos em Destaque

#### 1. 🚀 Eficiência Extrema e Imagens Docker Minúsculas
Sem a necessidade de incluir o SDK ou Runtime do .NET (~200MB+), a aplicação inicia em **poucos milissegundos** e consome o mínimo de memória RAM. Para auditorias em contêineres Docker ou Kubernetes, é possível gerar imagens minúsculas (na casa dos 20–30 MB) perfeitas para testes e provisionamento rápido.

#### 2. 🛡️ Segurança e Dificuldade de Engenharia Reversa
Em aplicações .NET tradicionais, o código é compilado para IL (Intermediate Language) dentro de DLLs, facilitando a descompilação quase perfeita do código-fonte através de ferramentas como `dnSpy` ou `dotPeek`.  
Com **Native AOT**, o processo de compilação realiza um *trimming* agressivo de metadados e gera código de máquina nativo. Fazer engenharia reversa no Heimdall exige análise de baixo nível com ferramentas estruturais densas como **Ghidra** ou **IDA Pro**, aumentando consideravelmente a resiliência e reduzindo a superfície de exposição da lógica interna do programa quando deixado no servidor.

#### 3. 🧩 Design Limpo sem Reflection Dinâmico (Source Generators)
O Native AOT proíbe a geração dinâmica de código em tempo de execução (como `System.Reflection.Emit`). Para superar essa limitação mantendo a máxima velocidade na serialização e deserialização de dados, o Heimdall adota **Source Generators** (`System.Text.Json` com `JsonSerializerContext`). Toda a amarração de tipos é resolvida em tempo de compilação estática, eliminando falhas de reflection em runtime e otimizando o uso de CPU.

---

## 🚀 Guia de Instalação, Deploy e Execução

### Passos de Build e Auditoria (Fluxo Limpo)

#### ETAPA 1: Build do Executável (Na sua máquina local)
```bash
git clone https://github.com/Proxyspyk/Heimdall.git
cd Heimdall
./build.sh
```
> O executável Native AOT será gerado em **`./dist/heimdall`** (~20 MB).

#### ETAPA 2: Transferência para o Servidor Alvo
Envie o binário para a pasta temporária do servidor via `scp`:
```bash
scp dist/heimdall usuario@ip-do-servidor:/tmp/
```

#### ETAPA 3: Execução da Auditoria
No servidor remoto, conceda permissão de execução e rode o scan:
```bash
ssh usuario@ip-do-servidor
chmod +x /tmp/heimdall
/tmp/heimdall scan --json /tmp/relatorio_servidor.json
```

#### ETAPA 4: Coleta do Relatório e Limpeza (Zero Footprint)
Baixe o relatório para sua máquina local e remova os arquivos do servidor remoto:
```bash
# Na sua máquina local:
scp usuario@ip-do-servidor:/tmp/relatorio_servidor.json ./

# No servidor remoto (limpeza total):
rm /tmp/heimdall /tmp/relatorio_servidor.json
```

---

## 💻 Uso da Linha de Comando (CLI)

```bash
# Scan simples (relatório visual colorido no terminal)
heimdall scan

# Salva o relatório em formato JSON estruturado
heimdall scan --json relatorio.json

# Desativa o filtro de versão (exibe mais resultados com heurística ampla)
heimdall scan --no-version-filter

# Oculta o banner inicial
heimdall scan --no-banner

# Utiliza uma chave da API do NVD (aumenta o limite de requisições de 5 para 50 req/30s)
heimdall scan --api-key SUA_API_KEY
# ou via variável de ambiente:
export NVD_API_KEY=SUA_API_KEY
```

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

## ⚠️ Limitações Conhecidas

- **Correspondência de Versão:** A correlação é feita por **palavra-chave + heurística de versão na descrição da CVE**, e não por CPE 2.3 estrito. Trate os resultados como uma lista de priorização para análise de risco.
- **Taxa de Requisições da API NVD:** Sem chave de API (`NVD_API_KEY`), o NVD limita as requisições a 5 a cada 30 segundos. Para análises mais rápidas, utilize uma chave gratuita obtida no site do NVD.
- **Heurística de Exploits:** A indicação de "exploit público" é baseada nas referências e dados cadastrados no próprio NVD.

---

## 🧪 Testes

Para executar a suíte de testes unitários automatizados:

```bash
dotnet test Tests/Heimdall.Tests.csproj
```

---

## 📄 Licença

Distribuído sob a licença **MIT**. Veja [LICENSE](LICENSE) para mais detalhes.

---

## 👤 Autoria e Créditos
- **Manutenção & Melhorias:** [Kleyon Almeida](https://github.com/kleyonalmeida)  
  *Melhorias de arquitetura CQRS, compilação Native AOT, otimização de performance e documentação.*

- **Autor do Projeto Original:** [Gabriel Knobbe da Silveira](https://github.com/Proxyspyk) ([@Proxyspyk](https://github.com/Proxyspyk))  
  *Criador da versão inicial do projeto nomeado como Argus.*
