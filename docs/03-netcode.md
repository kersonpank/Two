# 03 — Netcode: dois personagens, uma corda, redes móveis

Este é o documento mais importante do projeto. O jogo inteiro depende de resolver bem **um** problema: dois corpos com física, ligados por uma corda, controlados por dois celulares com 80–250ms de latência entre si (4G/5G no Brasil, com jitter e perda de pacote reais).

## Por que as soluções "clássicas" não servem aqui

- **Lockstep determinístico (estilo rollback de jogo de luta):** exige física determinística — Box2D/Unity Physics 2D **não** é determinístico entre dispositivos (float, arquitetura). Reescrever física em fixed-point para um plataformer de corda é trabalho de meses e mata a fluidez de iteração.
- **Servidor autoritativo com predição completa (estilo FPS):** predizer *dois* corpos acoplados por uma restrição significa que o erro de predição do parceiro contamina a sua própria predição → correções visíveis constantes ("borracha"). Além de custo de servidor dedicado para um jogo PvE de 2 pessoas, sem motivo anti-cheat.

## O modelo escolhido: autoridade distribuída + corda dividida em duas

**Photon Fusion 2 em Shared Mode.** Cada cliente tem **autoridade de estado sobre o próprio personagem**. O Photon Cloud faz relay (resolve NAT, nada de P2P frágil) e hospeda a sala.

### As três camadas da sincronização

**1. Seu personagem — 100% local.**
Input → física → render no mesmo frame. Latência percebida: zero, sempre, em qualquer rede. Estado replicado para o parceiro a cada tick (posição, velocidade, estado da state machine, flags de fio).

**2. O parceiro — interpolado no passado.**
Renderizado ~100–150ms atrás do presente dele, interpolando entre snapshots recebidos (buffer de interpolação absorve o jitter). Extrapolação curta (≤ 100ms) em perda de pacote, depois congela com efeito visual de "luz tremulando" — o lag vira linguagem do jogo em vez de glitch.

**3. A corda — dividida em gameplay e visual. Esta é a decisão central do projeto.**

- **Corda de gameplay (`RopeConstraint`):** não é física de corda — é uma **restrição de distância suave** entre a sua posição (real) e a posição *interpolada* do parceiro. Se `dist > comprimentoMax`, aplica força de mola puxando você de volta (nunca teleporte, nunca restrição rígida). Cada cliente aplica isso **só no próprio personagem**.
  - Por que funciona: a restrição é *suave*, então uma discordância de 20–30cm entre o que cada cliente vê não explode — vira uma diferença de força minúscula que se autocorrige. Os dois clientes discordam levemente e **nunca importa**, porque nenhum puzzle depende de centímetros no tether.
- **Corda visual (`RopeVisual`):** simulação verlet local (~20 pontos, 2–3 iterações de constraint por frame) ancorada nas duas posições *renderizadas*. Custa microssegundos, nunca trafega na rede, e está **sempre** perfeitamente colada nos dois personagens que o jogador vê. A corda que o jogador olha o tempo todo é, por construção, impossível de dessincronizar.

### Por que o balanço (a mecânica de ouro) fica perfeito

Quando você arremessa o fio num **gancho do cenário**, a âncora é um ponto **estático do mundo** — conhecido por ambos os clientes sem nenhuma comunicação. O pêndulo inteiro roda localmente com física própria (não joint do Box2D: pêndulo analítico — posição projetada no círculo da corda, velocidade tangencial preservada; mais estável e mais "tunável" que `DistanceJoint2D`). O parceiro só recebe sua posição replicada, como sempre. **Balançar — o momento de maior exigência de fluidez — tem dependência de rede zero.**

### Interações que dependem do parceiro: resolvidas por design, não por código

Regra de design (doc 01): para o parceiro ser âncora/ponte/contrapeso, ele precisa **se fixar (pin)** — estado estático, replicado como um booleano + posição.

- Fixado = quase-estático = a predição sobre ele é trivialmente correta.
- O balanço a partir do parceiro fixado é idêntico ao balanço em gancho (âncora parada).
- Transição de estados com confirmação otimista: você *pede* para rapelar do parceiro; localmente já começa (otimismo); se o pin tivesse sido solto 100ms antes, o estado replicado chega e a corda "escorrega" — falha graciosamente, sem rubber-banding.

### Objetos compartilhados (caixas, alavancas, plataformas)

- **Autoridade por interação:** quem toca/agarra primeiro pede autoridade do objeto (Fusion suporta transferência de state authority). O outro cliente o vê interpolado.
- Dois agarrando a mesma caixa: a autoridade fica com quem pegou primeiro; o segundo aplica força *via rede* (envia input de força, não posição). Em puzzle de contrapeso, um dos dois está sempre fixado — de novo o design salvando o netcode.
- Mecanismos de estado discreto (alavanca, porta, checkpoint): estado replicado simples com o dono da sala como tiebreaker. Trivial.

## Mobile é hostil: o ciclo de vida é parte do netcode

Celular recebe ligação, notificação, troca de app, troca de Wi-Fi para 4G no meio do pulo. Isso é o caso **comum**, não a exceção:

- **App em background / queda:** o parceiro vê o personagem virar "luzinha adormecida" (estado `Dormant`), invulnerável e carregável — o jogador presente pode continuar se movendo e até carregar o ausente até o checkpoint. **Janela de reconexão: 60s** com retorno por snapshot; depois disso, sala pausa com opa de "esperar / salvar e sair".
- **Troca de rede (Wi-Fi→4G):** reconexão transparente do Fusion; o estado `Dormant` cobre o buraco visualmente.
- **Pausa de verdade não existe em online** — mas em co-op de 2, existe: botão de pausa *pede* pausa, o parceiro aceita, simulação congela para ambos. Detalhe pequeno, enorme para a vida real ("abre a porta pra mãe").
- **Relógio/ticks:** Fusion cuida da sincronização de tick; nunca usar `Time.time` local para lógica compartilhada — tempo de jogo é o tick da simulação.

## Entrada na sala

1. Jogador A cria sala → Fusion gera sala privada → código de 6 letras (sem 0/O/1/I).
2. Jogador B digita o código **ou** abre o deep link `two.app/j/ABC123` do WhatsApp.
3. Região Photon: `sa` (São Paulo) como default para o soft launch BR; seleção automática por ping depois.

## Como validar (antes de ter conteúdo)

- **Simulação de rede desde o dia 1:** Fusion tem network conditions embutido. Toda feature de gameplay é aprovada jogando com **150ms RTT + 30ms jitter + 2% loss** — o perfil "4G razoável no busão". Se estiver gostoso aí, está gostoso em qualquer lugar.
- **Multiplayer Play Mode** (duas instâncias de editor) no dia a dia; dois devices físicos (1 Android médio + 1 iPhone) nas revisões semanais.
- Overlay de debug no build: RTT, tamanho do buffer de interpolação, divergência de posição do parceiro (dist entre onde ele está e onde eu o renderizo). Divergência média > 50cm em condição-alvo = regressão, investigar.

## Riscos conhecidos e saídas de emergência

| Risco | Sinal | Plano B |
|---|---|---|
| Shared Mode: autoridade de objeto "ping-pong" em puzzles | caixa tremendo quando os dois interagem | Host (criador da sala) mantém autoridade de TODOS os objetos de mundo; só personagens ficam distribuídos |
| Restrição suave do tether diverge demais em loss alto | puxões fantasmas no limite da corda | Zona morta maior + reconciliação lenta (lerp de comprimento), nunca força instantânea |
| Fusion/Photon não atender | custo, bugs, limite técnico | A arquitetura (doc 02, princípio nº 1) isola a rede: trocar para Netick/FishNet+Relay muda `Net/`, não o jogo |
