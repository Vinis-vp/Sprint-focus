<p align="center">
  <img src="Packaging/icons/sprintfocus-256.png" alt="SprintFocus" width="128" />
</p>

<h1 align="center">SprintFocus</h1>

<p align="center">
  App desktop de gestão de tempo entre <b>trabalho</b>, <b>estudo</b> e <b>lazer</b>, com timer Pomodoro.
</p>

---

## Sobre

O SprintFocus nasceu de uma pergunta simples: *para onde está indo o meu tempo?* Ele combina um timer
Pomodoro com o registro de quanto tempo foi dedicado a cada categoria do dia. O objetivo de longo
prazo é também detectar quanto tempo é gasto assistindo vídeos fora das sessões de trabalho ou estudo.

A interface é minimalista: um menu lateral com três páginas (Timer, Resumo e Configurações) e um
mini-timer flutuante que aparece quando a janela principal é minimizada.

## Funcionalidades

- **Timer Pomodoro** com foco, pausa curta e pausa longa, durações configuráveis e anel de progresso.
- **Categorias** Trabalho, Estudo e Lazer. O tempo de foco é contado na categoria selecionada.
- **Resumo do dia** com o total por categoria.
- **Histórico em SQLite.** As sessões de foco ficam salvas e os totais continuam ao reabrir o app.
- **Mini-timer flutuante** ao minimizar:
  - fica sempre por cima e pode ser arrastado e redimensionado;
  - fica num canto configurável;
  - tem botões de iniciar/pausar, +30s, abrir o app e fechar;
  - não rouba o foco do app em uso.
- **Avisos ao fim de cada fase:** som, notificação do sistema e mini-timer piscando. Cada um pode ser ligado ou desligado.
- **Início automático da próxima fase** (opcional).
- **Mascote** (provisório, com emojis) que muda conforme o estado: focado, descansando ou parado.
- **Tema** claro, escuro ou do sistema.

## Tecnologias

| | |
|---|---|
| Linguagem / runtime | C# · .NET 10 |
| Interface | [Avalonia UI 12](https://avaloniaui.net/) (Fluent) · padrão MVVM |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) |
| Ícones | [FluentIcons.Avalonia](https://github.com/davidxuang/FluentIcons) |
| Banco de dados | SQLite via [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) |
