# 01 — Visão e Game Design

## Pilares (tudo que entrar no jogo precisa servir a pelo menos um)

1. **Cooperação obrigatória, não opcional.** Nenhum puzzle pode ser resolvido por um jogador sozinho. O design sempre pergunta: "o que o *outro* precisa fazer?"
2. **Fluidez acima de tudo.** 60fps, input instantâneo, morte sem custo de tempo, zero telas de loading dentro do capítulo.
3. **Sessões curtas, laço longo.** Um nível em 5–8 minutos; a relação da dupla dura meses (streak, cosméticos, desafios).
4. **O parceiro está sempre presente.** Mesmo cada um na sua tela, você *sente* o outro: o fio aponta para ele, a luz responde à distância, emotes e pings são de um toque.

## Conceito

Dois espíritos de luz acordam separados num mundo apagado e se ligam por um fio de luz. O fio tem comprimento máximo — vocês nunca se separam de verdade. Juntos, reacendem o mundo, bioma por bioma.

- **Distância = escuridão.** Perto um do outro, o cenário ao redor se ilumina (raio de luz maior). No limite do fio, a luz fica tensa e trêmula. Isso comunica a mecânica de tether sem UI nenhuma.
- Tema emocional honesto e universal: manter-se conectado a alguém mesmo quando o caminho força vocês a se afastarem.

## Mecânicas core (MVP)

| Mecânica | Descrição | Dependência de rede |
|---|---|---|
| **Mover / pular / escalar** | Plataforma clássica com assists generosos (ver Controles) | Só o próprio personagem — local |
| **Tether (fio entre os dois)** | Distância máxima; esticar no limite puxa levemente o mais leve/no ar | Restrição suave, tolerante a desvio |
| **Arremessar o fio** | Mirar e prender o fio em pontos de gancho do cenário → balançar, rapelar | Âncora é estática → 100% previsível |
| **Fixar-se (pin)** | O personagem se "planta" no chão/gancho e vira âncora viva: o parceiro balança a partir dele, desce de rapel, usa o fio esticado como ponte/trampolim | O fixado fica **estático** → previsível na rede (decisão deliberada, ver netcode) |
| **Puxar / contrapeso** | Puxar objetos com o fio; puzzles de peso em dupla (um segura, outro atravessa) | Autoridade do objeto transferida para quem toca |
| **Carregar o parceiro** | Um "recolhe" o outro e anda carregando (vira uma luzinha no ombro) | Também é o fallback de desconexão/AFK — o jogo nunca trava esperando o outro |

**Design para latência é design de jogo:** toda interação em que um jogador depende *fisicamente* do outro exige que um dos dois esteja fixado (pin) ou estático. Isso não é só truque de rede — cria ritmo de revezamento ("eu seguro, você vai; agora você segura, eu vou") que é exatamente o prazer do co-op.

### Mecânicas pós-MVP (gavetas, não promessas)
- Trampolim de fio entre dois pins; cortar/reatar o fio em pontos especiais; seções de "fuga" sincronizada; fio condutor de energia (acender mecanismos enquanto esticado sobre eles).

## Controles touch (onde mobile ganha ou perde o jogo)

- **Polegar esquerdo:** joystick virtual **flutuante** (nasce onde o dedo tocar, raio curto). Nunca fixo no canto.
- **Polegar direito:** dois botões grandes — **Pulo** e **Ação** (contextual: arremessar fio / agarrar / fixar / soltar).
- **Mirar o arremesso:** segurar Ação entra em **câmera lenta (~0.3x)** com mira em arco; soltar arremessa. Perdoa dedos imprecisos e fica cinematográfico — a câmera lenta é só local e não afeta o parceiro (o arremesso em si é instantâneo para a rede).
- **Assists obrigatórios** (mobile precisa de mais do que console):
  - Coyote time ~120ms, jump buffer ~150ms, pulo de altura variável.
  - Magnetismo de agarre em bordas e ganchos (raio generoso, snapping suave).
  - Auto-escalada de degraus baixos.
- **Haptics** em: agarrar gancho, ápice do balanço (o momento de soltar!), aterrissar, parceiro puxando o fio. O ápice do balanço com haptic é o que ensina o timing sem tutorial.
- Layout espelhável para canhotos; botões reposicionáveis.

## Câmera (cada um na sua tela, mas vendo o outro)

- Cinemachine seguindo o **próprio** personagem como alvo primário.
- **Peso gravitacional do parceiro:** o enquadramento desloca-se parcialmente em direção ao ponto médio da dupla e dá zoom out conforme a distância cresce — dentro do alcance do fio, os dois estão quase sempre na mesma tela.
- Parceiro fora da tela (raro, pelo limite do tether): seta indicadora na borda + **o próprio fio já aponta para ele** — wayfinding de graça.
- Lookahead na direção do movimento; shake sutil em impactos; zoom dramático em momentos de sincronização.

## Morte, checkpoint e fluxo

- Morte de um = ele **se desfaz em luz e é puxado de volta pelo fio até o parceiro** em ~1,5s. Sem tela de morte, sem loading, sem recomeçar o nível.
- Morte dos dois = respawn no último checkpoint (frequente: a cada 30–60s de progresso).
- Custo da morte é quase zero **de tempo**, mas a coordenação continua sendo exigida — frustração baixa, desafio intacto. Essencial para "fluido e viciante".

## Comunicação sem voz (ninguém quer falar no busão)

- **Roda de emotes** (toque no avatar do parceiro): "vem!", "espera", "segura", "boa!", coração, riso.
- **Ping contextual:** toque duplo no cenário marca um ponto para o parceiro (brilha na tela dele).
- **Botão de sincronia "3-2-1":** qualquer um propõe; os dois seguram; contagem regressiva compartilhada; soltar juntos. Resolve todo puzzle do tipo "temos que pular/soltar ao mesmo tempo" sem voz — e vira o momento assinatura do jogo.
- Chat por voz/texto: **fora do MVP** (dupla geralmente já tem WhatsApp aberto; e voz traz moderação, permissões e custo).

## "Viciante" por design (retenção honesta, sem dark patterns)

1. **Estrutura:** capítulos de 5 níveis, níveis de 5–8 min. Sempre dá para "só mais um nível".
2. **Streak da dupla:** "vocês jogaram juntos 4 dias seguidos" — o streak é **do par**, não do indivíduo. Compromisso social é o retentor mais forte que existe; aqui ele é o tema do jogo.
3. **Desafio diário em dupla:** um nível remixado por dia, com tempo da dupla em ranking entre amigos. Motivo diário para chamar o parceiro.
4. **Cosméticos:** cores/trilhas do fio, skins dos espíritos, auras — ganhos por jogar, vendidos por IAP. O fio da dupla é a vitrine: os dois veem o cosmético o tempo todo.
5. **Colecionáveis escondidos** que exigem cooperação para alcançar (razão para rejogar níveis).
6. **Nunca punir quem não voltou:** streak quebra com carência de 1 dia, progresso nunca regride. Jogo co-op que gera culpa mata a dupla.

## Crescimento (Brasil-first)

- **Entrar na sala = código de 6 letras OU link.** O link abre o app direto na sala (deep link / App Link). O fluxo alvo: *"baixa aí → clica no link que te mandei no zap → já estamos jogando"* em menos de 2 minutos.
- **Friend Pass:** o jogo é gratuito para quem for convidado por alguém que tenha a versão completa (convidado joga todos os capítulos *em dupla com o dono*). Remove a maior fricção de um jogo 100% co-op e transforma cada comprador em distribuidor.
- Momentos "printáveis": foto da dupla no fim do capítulo (pose + cosméticos + tempo), um toque para compartilhar.

## O que este jogo NÃO é

- Não tem single-player (o "carregar o parceiro" cobre quedas de conexão, não substitui o co-op).
- Não tem matchmaking com estranhos no MVP (co-op íntimo com quem você conhece; estranhos = moderação, toxicidade, outro jogo).
- Não tem PvP, energia/stamina, anúncios intersticiais, nem pay-to-win.
