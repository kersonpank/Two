# Two — co-op de plataforma e física para mobile

Jogo mobile de plataforma 2.5D cooperativo online para **duas pessoas, cada uma no seu celular**, inspirado na sensação de *Unravel Two*: dois personagens ligados por um fio, que só avançam se cooperarem de verdade.

**Conceito:** dois espíritos de luz ligados por um **fio de luz**. O fio é ao mesmo tempo a mecânica central (balançar, puxar, ancorar, servir de ponte) e o laço emocional do jogo — quando a dupla se afasta, o mundo escurece; quando se aproxima, ilumina.

> Por que "fio de luz" e não lã? Primeiro, para não colidir com o trade dress de Unravel (os Yarnys e a estética de lã são identidade da Coldwood/EA). Segundo, porque luz dá uma mecânica extra de graça: iluminar o caminho vira um incentivo *sistêmico* para ficar perto do parceiro.

## Decisões principais (resumo)

| Área | Decisão | Por quê |
|---|---|---|
| Engine | **Unity 6.3 LTS (URP)** | Melhor ecossistema de netcode com predição; export mobile maduro; física 2D pronta |
| Netcode | **Photon Fusion 2 — Shared Mode** | Autoridade local do próprio personagem = input instantâneo; relay/matchmaking na nuvem = zero DevOps |
| Física da corda | **Visual local (verlet) separada da restrição de gameplay** | A corda nunca "briga" com a rede; ver `docs/03-netcode.md` |
| Backend | **Unity Gaming Services (Auth + Cloud Save + Analytics) ou Firebase** | Login anônimo, progresso na nuvem, deep links de convite |
| Entrada na sala | **Código de 6 letras + deep link (WhatsApp)** | O loop de crescimento no Brasil é o link no zap |
| Modelo | **Friend Pass**: um compra/baixa, convida o amigo de graça | Jogo co-op obrigatório precisa eliminar a fricção de "convencer o amigo" |

## Estrutura do repositório

```
UnityProject/Assets/_Project/Scripts/
  Core/    # Two.Core — TODO o gameplay em C# puro (sem UnityEngine): motor de
           # plataforma, pêndulo do balanço, corda verlet, tether, aim assist
  Unity/   # Two.Unity — a casca: adapters MonoBehaviour, input touch, câmera,
           # corda visual, porta de rede (IPartnerProvider) + notas do Fusion 2
tests/     # 25 testes headless do core (dotnet run — sem editor, roda em CI)
prototype/ # protótipo HTML jogável (touch) com os MESMOS números de feel
docs/      # visão, stack, netcode, roadmap
```

**Arquitetura em uma frase:** o personagem não sabe que a Unity nem a rede existem —
`Two.Core` roda headless (testes, CI, bot, replay, futuro servidor), a Unity só
apresenta, e a rede entra por uma única interface (`IPartnerProvider`).

Para abrir na Unity: `UnityProject/SETUP.md`. Para rodar os testes:
`dotnet run -c Release --project tests/Two.Core.Tests`.

## Documentos

1. [`docs/01-visao-e-game-design.md`](docs/01-visao-e-game-design.md) — conceito, mecânicas, controles touch, câmera, retenção ("viciante" por design, não por acaso)
2. [`docs/02-stack-e-arquitetura.md`](docs/02-stack-e-arquitetura.md) — comparação de engines, stack completa, custos, estrutura do projeto
3. [`docs/03-netcode.md`](docs/03-netcode.md) — o documento mais importante: como sincronizar dois personagens ligados por uma corda em redes móveis
4. [`docs/04-roadmap-mvp.md`](docs/04-roadmap-mvp.md) — fases com critérios de saída, riscos, métricas, e o que fica explicitamente FORA do MVP

## Regra de ouro do projeto

**O jogo vive ou morre no "feel".** Nada de produzir conteúdo antes de o movimento + balanço na corda estarem gostosos num único celular, offline. A Fase 0 do roadmap existe só para isso.
