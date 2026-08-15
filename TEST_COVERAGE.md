# Mapeamento e Cobertura de Testes — Python para C# (Heimdall)

Este documento descreve o mapeamento completo da suíte de testes legada em Python (`tests/`) para a nova suíte de testes em C# (.NET 10 xUnit) localizada em `Tests/`.

---

## 1. Mapeamento por Módulo

### 1.1 `test_collectors.py` ➔ `Tests/CollectorsTests.cs`
Coobre a extração de versões semânticas de comandos/pacotes e a coleta de dados do sistema local.

| Teste Python | Descrição / Comportamento Testado | Teste C# Equivalente | Status C# |
| :--- | :--- | :--- | :--- |
| `test_extract_version_simple` | Extrai versão simples de uma string (ex: `"sudo 1.9.15"` -> `"1.9.15"`). | `ExtractVersion_Simple_ReturnsExpectedVersion` | ✅ Passou |
| `test_extract_version_with_build_metadata` | Extrai versão preservando metadados de build (ex: `"OpenSSL 3.0.13-1ubuntu3"` -> `"3.0.13-1ubuntu3"`). | `ExtractVersion_WithBuildMetadata_ReturnsFullVersion` | ✅ Passou |
| `test_extract_version_none_when_absent` | Retorna `null` quando não há versão numérica no texto ou texto nulo. | `ExtractVersion_AbsentOrNull_ReturnsNull` | ✅ Passou |
| `test_collect_system_info_returns_kernel_and_arch` | Coleta versão do kernel, arquitetura do sistema e lista de componentes. | `CollectSystemInfo_ReturnsKernelAndArchitecture` | ✅ Passou |
| `test_component_dataclass_defaults` | Garante valor default para `RawOutput` no record/model `Component`. | `Component_Defaults_RawOutputIsEmptyString` | ✅ Passou |

---

### 1.2 `test_matcher.py` ➔ `Tests/MatcherTests.cs` & `Tests/RiskScoreTests.cs`
Cobre a regra de negócio do algoritmo de matching de versão, cálculo da fórmula de risco e orquestração do CQRS (`CorrelateRiskQuery`).

| Teste Python | Descrição / Comportamento Testado | Teste C# Equivalente | Status C# |
| :--- | :--- | :--- | :--- |
| `test_version_mentioned_true_when_no_version` | Retorna `true` se a versão instalada for nula (sem filtro). | `VersionMentioned_NullVersion_ReturnsTrue` | ✅ Passou |
| `test_version_mentioned_matches_prefix` | Valida se o prefixo de versão (ex: `1.9.15p5`) bate com a descrição. | `VersionMentioned_MatchesPrefix_ReturnsTrue` | ✅ Passou |
| `test_version_mentioned_false_when_absent` | Retorna `false` se a versão não estiver na descrição da CVE. | `VersionMentioned_VersionAbsent_ReturnsFalse` | ✅ Passou |
| `test_version_mentioned_strips_debian_epoch_and_revision` | Remove epoch Debian (`1:`) e revisão (`-7+deb13u4`) para casar com a versão upstream (`10.0p1`). | `VersionMentioned_StripsDebianEpochAndRevision_ReturnsTrue` | ✅ Passou |
| `test_version_mentioned_strips_ubuntu_build_suffix` | Remove sufixos de empacotamento Ubuntu (ex: `2.39-0ubuntu8.7` -> `2.39`). | `VersionMentioned_StripsUbuntuBuildSuffix_ReturnsTrue` | ✅ Passou |
| `test_risk_score_bonus_for_public_exploit` | Pontuação de risco concede bônus de 10 pontos quando há indício de exploit público. | `RiskScore_BonusForPublicExploit_ReturnsHigherScore` | ✅ Passou |
| `test_risk_score_bounded_at_100` | Garante teto de 100.0 pontos para o `RiskScore`. | `RiskScore_BoundedAt100_ReturnsMax100` | ✅ Passou |
| `test_find_vulnerabilities_filters_and_sorts` | Filtra CVEs irrelacionadas e ordena os achados por risco decrescente. | `CorrelateRiskHandler_FiltersUnmatchedVersionsAndSorts` | ✅ Passou |
| `test_nvd_search_called_with_name_only_not_raw_version` | Garante que a busca no NVD envia apenas o nome do componente, sem a versão crua. | `CorrelateRiskHandler_QueriesNvdByNameOnly` | ✅ Passou |
| `test_on_component_result_reports_raw_vs_filtered_counts` | Notifica estatísticas de total de CVEs brutas vs filtradas por componente. | `CorrelateRiskHandler_ReportsRawAndFilteredCounts` | ✅ Passou |


---

### 1.3 `test_nvd_client.py` ➔ `Tests/NvdClientTests.cs`
Cobre a comunicação com a API do NVD, paginação com `startIndex` e tratamento de respostas HTTP.

| Teste Python | Descrição / Comportamento Testado | Teste C# Equivalente | Status C# |
| :--- | :--- | :--- | :--- |
| `test_search_does_single_request_when_results_fit_in_one_page` | Faz apenas 1 chamada HTTP quando `totalResults <= resultsPerPage`. | `SearchByKeyword_SinglePage_MakesOneHttpRequest` | ✅ Passou |
| `test_search_refetches_most_recent_page_when_total_exceeds_page_size` | Quando resultados excedem 1 página, recalcula `startIndex` para buscar a última página (CVEs mais recentes). | `SearchByKeyword_MultiplePages_FetchesMostRecentPage` | ✅ Passou |
| `test_search_by_keyword_ignores_raw_version_in_query` | Assegura que o parâmetro `keywordSearch` recebe apenas o nome do componente. | `SearchByKeyword_SendsOnlyKeywordNameInParams` | ✅ Passou |

---

### 1.4 `test_ui.py` ➔ `Tests/UiTests.cs`
Cobre a saída no terminal, verificação do banner e comportamento thread-safe do spinner em ambientes com e sem TTY.

| Teste Python | Descrição / Comportamento Testado | Teste C# Equivalente | Status C# |
| :--- | :--- | :--- | :--- |
| `test_print_banner_writes_title_and_art` | Escreve o título "HEIMDALL" e a arte ASCII do olho no stream de saída. | `PrintBanner_WritesTitleAndAsciiArt` | ✅ Passou |
| `test_spinner_enter_exit_does_not_raise_without_tty` | O spinner funciona sem exceções quando a saída não é um TTY (ex: `StringWriter`). | `EyeSpinner_NonTtyStream_DoesNotThrow` | ✅ Passou |
| `test_spinner_log_writes_message_even_without_tty` | O método `log()` escreve mensagens no stream mesmo sem suporte a TTY. | `EyeSpinner_Log_WritesMessageToStream` | ✅ Passou |

---

## 2. Estratégia de Desenvolvimento TDD (Test-Driven Development)

Para a migração em C#:
1. Escreveremos **primeiro todos os testes unitários** correspondentes na pasta `Tests/`.
2. Implementaremos os serviços de domínio (`VersionNormalizer`, `VersionMatcher`, `RiskCalculator`), os handlers de Query/Command do CQRS e a camada de infraestrutura.
3. Executaremos `dotnet test` até que 100% dos testes passem com sucesso.
