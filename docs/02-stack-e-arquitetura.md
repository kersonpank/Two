# 02 — Stack e Arquitetura

## A decisão de engine

O requisito que decide tudo: **física de corda entre dois jogadores, online, em rede móvel, com sensação fluida**. Isso exige netcode com predição/autoridade local madura + física 2D sólida + export mobile sem dor. Avaliação honesta:

| Critério | **Unity 6** | Godot 4.x | Unreal 5 | Web (Phaser/Pixi + WebRTC) | Defold |
|---|---|---|---|---|---|
| Netcode com predição pronto p/ produção | ✅ Photon Fusion 2, Netick, FishNet | ⚠️ API high-level sem predição; faça você mesmo | ✅ mas pensado p/ server dedicado | ❌ tudo na mão + TURN próprio | ⚠️ pouca coisa |
| Relay/matchmaking gerenciado (zero DevOps) | ✅ Photon Cloud, Unity Relay+Lobby | ❌ precisa Nakama/custom | ⚠️ EOS (complexo) | ❌ | ⚠️ |
| Física 2D + joints maduros | ✅ Box2D integrado | ✅ bom | ⚠️ overkill 3D | ⚠️ determinismo/perf JS | ✅ |
| Export mobile (tamanho, perf, toolchain) | ✅ maduro | ✅ ok | ❌ builds pesados p/ 2.5D | ✅ instantâneo, ⚠️ perf/fullscreen/haptics | ✅ ótimo |
| Ferramental de "feel" (câmera, tween, partículas) | ✅ Cinemachine, ecossistema enorme | ✅ bom | ✅ | ⚠️ | ⚠️ |
| Custo | Grátis < US$200k/ano de receita (Unity Personal, sem splash obrigatório; runtime fee foi cancelada em 2024) | Grátis (MIT) | 5% royalty > US$1M | Grátis | Grátis |

**Decisão: Unity 6 LTS + URP.** O gargalo do projeto é netcode-com-física, e é exatamente aí que o ecossistema Unity está anos à frente para um time pequeno.

- **Godot** é a alternativa legítima se a prioridade for stack 100% livre — mas você escreveria predição, interpolação, transferência de autoridade e relay na mão. São 2–3 meses extras no problema mais arriscado do projeto.
- **Web/PWA** é tentador pela distribuição instantânea, mas "fluido e viciante" em browser mobile briga com: Safari iOS (sem fullscreen real, sem haptics decentes), jitter de GC do JS, e WebRTC exigindo seus próprios servidores TURN. Não vale a troca.

## Stack completa

### Cliente (o jogo)
- **Unity 6 LTS** (6000.x), **URP**, pipeline 2.5D: gameplay num plano 2D (`Rigidbody2D`), cenário com camadas de parallax e luz 2D (`Light2D` — casa perfeitamente com o tema "fio de luz").
- **Física:** Unity Physics 2D (Box2D). Corda de gameplay = restrição de distância própria (ver netcode); corda visual = verlet caseiro (~20 segmentos, `LineRenderer` com glow). **Não** usar cadeia de `HingeJoint2D` para a corda — instável, caro e péssimo para rede.
- **Input System** (novo) com controles touch próprios (joystick flutuante não vem pronto de qualidade — fazer o nosso, é pequeno).
- **Cinemachine 3** para a câmera descrita no doc 01.
- **DOTween/PrimeTween** para juice; **Feel** (asset) opcional para haptics/screenshake.
- Áudio: Unity Audio no MVP (FMOD só se o projeto crescer).

### Netcode
- **Photon Fusion 2, Shared Mode** + **Photon Cloud** (relay, salas, código de sala). Detalhes e justificativa no doc 03.
- Sem servidor dedicado próprio no MVP: jogo co-op PvE de 2 jogadores não tem incentivo a cheating que justifique o custo. Anti-cheat aqui é irrelevante; consistência é o que importa.

### Backend / serviços
- **Unity Gaming Services**: Authentication (anônimo + Google Play Games / Sign in with Apple), **Cloud Save** (progresso individual + progresso da dupla, chaveado pelo par de contas), Remote Config (tuning de física/retention sem update na loja), Analytics.
  - Alternativa equivalente: Firebase (Auth + Firestore + Remote Config + Crashlytics). Escolher **um** e não misturar; UGS integra melhor com o editor, Firebase tem Crashlytics melhor. Decisão fina na Fase 1 — ambos têm free tier folgado para o MVP.
- **Crash reporting:** Crashlytics (ou Unity Cloud Diagnostics).
- **Deep links:** Android App Links + iOS Universal Links apontando para uma página estática (`two.app/j/ABC123`) que redireciona para a loja se o app não estiver instalado. (Firebase Dynamic Links foi descontinuado — não usar.)

### Custos de rede (ordem de grandeza)
- Photon free tier: **20 CCU = 10 duplas simultâneas**. Com sessões de ~20 min, isso atende confortavelmente algumas centenas de DAU — sobra para todo o desenvolvimento e o soft launch.
- Próximo degrau (~100 CCU ≈ 50 duplas) fica na casa de ~US$100/mês — confirmar tabela vigente da Photon quando chegar lá. Tráfego de um jogo de 2 players com snapshot pequeno é mínimo; o custo é CCU, não banda.

## Metas de performance (restrição de design, não detalhe)

- **60fps em Android médio** (classe Snapdragon 695 / Helio G99, ~2021+). Tudo é orçado contra esse aparelho, não contra o flagship do dev.
- Build Android **< 300MB** (realidade de armazenamento no Brasil); textura ASTC, áudio Vorbis, strip de engine code.
- Opção de 30fps/eco para bateria; atenção a thermal throttling em sessão longa (teste de 30 min contínuos).
- Zero loading dentro do capítulo: níveis em additive scenes com pré-carga assíncrona.

## Estrutura do projeto Unity

```
Assets/
  _Project/
    Scripts/
      Core/          # bootstrap, service locator, ciclo de vida do app (pause/resume!)
      Player/        # controller, state machine (Run/Jump/Swing/Pinned/Carried), assists
      Rope/          # RopeConstraint (gameplay) + RopeVisual (verlet) — separados de propósito
      Net/           # Fusion: spawn, autoridade, sala por código, reconexão
      Interactables/ # ganchos, objetos puxáveis, mecanismos, checkpoints
      CameraRig/
      UI/            # HUD touch, emotes, pings, lobby
      Meta/          # progresso, streak, cosméticos, cloud save
    Prefabs/  Scenes/  Art/  Audio/  Settings/
  Plugins/
```

- **Assembly definitions** por pasta de Scripts (compilação rápida; impede `Net/` de vazar para dentro de `Player/` — o controller não pode saber que rede existe, ver doc 03).
- **ScriptableObjects para todo tuning** (física do pulo, corda, câmera, assists): designer itera sem tocar código, e Remote Config pode sobrescrever em produção.
- Testes: EditMode para a matemática da corda/constraints; **Multiplayer Play Mode** (Unity 6) para rodar duas instâncias do editor lado a lado — é assim que se desenvolve co-op sem dois celulares na mão o dia todo.

## Princípio arquitetural nº 1

**O personagem não sabe que a rede existe.** `PlayerController` consome um `IInputSource` (local, remoto-interpolado ou replay) e aplica física. A camada `Net/` fornece inputs e corrige estado por fora. Isso permite: Fase 0 100% offline, bots de teste, replays para debug de física, e trocar de solução de netcode sem reescrever o jogo.
